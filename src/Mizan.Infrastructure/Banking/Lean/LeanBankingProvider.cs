using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Banking;
using Mizan.Domain.Transactions;

namespace Mizan.Infrastructure.Banking.Lean;

// The only class that knows Lean's URLs and JSON. Shapes come from Lean's KSA v2 docs and the
// sandbox spike (docs/domain-discovery/mvp1/00-sandbox-findings.md).
internal sealed class LeanBankingProvider(HttpClient http, LeanTokenCache tokens, IOptions<LeanOptions> options, Clock clock)
    : IBankingProvider
{
    private const int PageSize = 100;
    private readonly LeanOptions _options = options.Value;

    public string Name => "lean";

    public async Task<string> CreateCustomerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var appUserId = userId.ToString();
        using var response = await SendAsync(HttpMethod.Post, "/customers/v1/", new { app_user_id = appUserId }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return (await ReadAsync(response, cancellationToken))?["customer_id"]?.GetValue<string>()
                ?? throw new BankingProviderException("Lean created a customer but returned no customer_id.");
        }

        // app_user_id is unique at Lean: if an earlier attempt created it, fetch it instead.
        var existing = await GetJsonAsync($"/customers/v1/app-user-id/{appUserId}", cancellationToken);
        return existing["customer_id"]!.GetValue<string>();
    }

    public async Task<LinkSession> CreateLinkSessionAsync(string providerCustomerId, CancellationToken cancellationToken)
    {
        var accessToken = await tokens.GetCustomerTokenAsync(providerCustomerId, cancellationToken);

        return new LinkSession(
            _options.AppToken,
            providerCustomerId,
            accessToken,
            _options.Sandbox,
            _options.Permissions,
            clock.Now.AddMonths(_options.ConsentMonths),
            _options.SuccessRedirectUrl,
            _options.FailRedirectUrl);
    }

    public async Task<IReadOnlyList<ProviderEntity>> ListEntitiesAsync(string providerCustomerId, CancellationToken cancellationToken) =>
        (await GetEntityNodesAsync(providerCustomerId, cancellationToken)).Select(ToEntity).OfType<ProviderEntity>().ToList();

    public async Task<ProviderEntity?> GetEntityAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken) =>
        (await ListEntitiesAsync(providerCustomerId, cancellationToken)).SingleOrDefault(e => e.EntityId == entityId);

    public async Task<IReadOnlyList<BankAccountData>> GetAccountsAsync(string entityId, CancellationToken cancellationToken)
    {
        var accounts = await GetPagedDataAsync("/data/v2/accounts", "accounts", new() { ["entity_id"] = entityId }, cancellationToken);
        var result = new List<BankAccountData>();

        foreach (var account in accounts)
        {
            var accountId = Str(account, "account_id")!;
            var (balance, asOf) = await TryGetBalanceAsync(entityId, accountId, cancellationToken);

            result.Add(new BankAccountData(
                accountId,
                ToKind(Str(account, "account_sub_type")),
                Str(account, "currency") ?? "SAR",
                (account["account"] as JsonArray)?
                    .FirstOrDefault(a => Str(a, "scheme_name") == "IBAN") is { } iban ? Str(iban, "identification") : null,
                Str(account, "nickname") ?? Str(account, "description") ?? "Account",
                balance,
                asOf));
        }

        return result;
    }

    public async Task<IReadOnlyList<BankTransactionData>> GetTransactionsAsync(
        string entityId, string accountId, SyncWindow window, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["entity_id"] = entityId,
            ["start_date"] = window.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["end_date"] = window.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["insights"] = "true",
        };

        var nodes = await GetPagedDataAsync($"/data/v2/accounts/{accountId}/transactions", "transactions", query, cancellationToken);
        return nodes.Select(ToTransaction).OfType<BankTransactionData>().ToList();
    }

    public async Task RequestRefreshAsync(string entityId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Post, "/data/v2/refreshes", new { entity_id = entityId }, cancellationToken);
        await EnsureOkAsync(response, cancellationToken);
    }

    public async Task RevokeConsentAsync(string providerCustomerId, string entityId, CancellationToken cancellationToken)
    {
        var entity = (await GetEntityNodesAsync(providerCustomerId, cancellationToken)).SingleOrDefault(e => Str(e, "id") == entityId);
        var consent = entity is null ? null : ActiveConsent(entity);
        if (consent is null)
        {
            return; // Nothing left to revoke.
        }

        var path = $"/customers/v1/{providerCustomerId}/entities/{entityId}/consent/{Str(consent, "id")}?reason=USER_REQUESTED";
        using var response = await SendAsync(HttpMethod.Delete, path, null, cancellationToken);
        await EnsureOkAsync(response, cancellationToken, ignoreNotFound: true);
    }

    public async Task DeleteCustomerAsync(string providerCustomerId, CancellationToken cancellationToken)
    {
        // Lean has no "delete customer": removing every entity removes the data and consents it holds.
        foreach (var entity in await GetEntityNodesAsync(providerCustomerId, cancellationToken))
        {
            using var response = await SendAsync(
                HttpMethod.Delete, $"/customers/v1/{providerCustomerId}/entities/{Str(entity, "id")}",
                new { reason = "USER_REQUESTED" }, cancellationToken);
            await EnsureOkAsync(response, cancellationToken, ignoreNotFound: true);
        }
    }

    private async Task<List<JsonNode>> GetEntityNodesAsync(string providerCustomerId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/customers/v1/{providerCustomerId}/entities", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        var body = await EnsureOkAsync(response, cancellationToken);
        return body is JsonArray array ? array.OfType<JsonNode>().ToList() : [];
    }

    private ProviderEntity? ToEntity(JsonNode entity)
    {
        var bank = entity["bank_details"];
        var consent = ActiveConsent(entity) ?? (entity["consents"] as JsonArray)?.OfType<JsonNode>().LastOrDefault();
        if (consent is null)
        {
            return null;
        }

        var granted = Date(consent, "creation_date_time") ?? clock.Now;
        var active = Str(consent, "consent_status") == "ACTIVE";

        // expiration_date_time is when the bank stops sharing (the sandbox revoked it after 24 hours, as stated).
        // transaction_to_date_time is only the end of the data range, not the access period.
        var expires = active
            ? Date(consent, "expiration_date_time") ?? granted.AddMonths(_options.ConsentMonths)
            : Date(consent, "status_update_date_time") ?? clock.Now;

        return new ProviderEntity(
            Str(entity, "id")!,
            Str(bank, "identifier") ?? Str(entity, "bank_identifier") ?? "UNKNOWN",
            Str(bank, "name") ?? "Bank",
            granted,
            expires > granted ? expires : granted.AddSeconds(1));
    }

    private static JsonNode? ActiveConsent(JsonNode entity) =>
        (entity["consents"] as JsonArray)?.OfType<JsonNode>()
            .Where(c => Str(c, "consent_status") == "ACTIVE")
            .OrderByDescending(c => Str(c, "creation_date_time"))
            .FirstOrDefault();

    private BankTransactionData? ToTransaction(JsonNode node)
    {
        var direction = Str(node, "credit_debit_indicator") switch
        {
            "DEBIT" => TransactionDirection.Debit,
            "CREDIT" => TransactionDirection.Credit,
            _ => (TransactionDirection?)null,
        };

        var amount = Num(node["amount"], "amount");
        var date = Date(node, "booking_date_time") ?? Date(node, "value_date_time");
        if (direction is null || amount is not > 0 || date is null || Str(node, "transaction_id") is not { } id)
        {
            return null;
        }

        var charge = node["charge_amount"];
        var fee = charge is not null && charge["charge_included"]?.GetValue<bool>() == false ? Num(charge, "amount") : null;

        // The other side of the transfer: who received money we sent, or who sent money to us.
        var counterparty = direction == TransactionDirection.Debit ? node["creditor_account"] : node["debtor_account"];
        if (counterparty is JsonArray list)
        {
            counterparty = list.FirstOrDefault();
        }

        var type = Str(node, "transaction_type");

        return new BankTransactionData(
            id,
            amount.Value,
            Str(node["amount"], "currency") ?? "SAR",
            direction.Value,
            Str(node, "status") == "PENDING" ? BankStatus.Pending : BankStatus.Booked,
            // The bank sends dates without a meaningful time (V5 in the findings).
            DateOnly.FromDateTime(date.Value.UtcDateTime),
            Str(node["merchant_details"], "merchant_name") ?? Str(node["insights"], "description_cleansed") ?? Str(node, "transaction_information"),
            Str(node["insights"], "category"),
            Num(node["insights"], "category_confidence"),
            Str(node, "sub_transaction_type") switch
            {
                "REFUND" => BankSubType.Refund,
                "REVERSAL" or "WITHDRAWAL_REVERSAL" or "DEPOSIT_REVERSAL" => BankSubType.Reversal,
                _ => BankSubType.None,
            },
            Str(counterparty, "identification"),
            fee,
            IsCashWithdrawal: type == "ATM" && direction == TransactionDirection.Debit);
    }

    private static AccountKind ToKind(string? subType) => subType switch
    {
        "CURRENT" => AccountKind.Current,
        "SAVINGS" => AccountKind.Savings,
        "CREDIT" or "CREDIT_CARD" => AccountKind.CreditCard,
        "CHARGE_CARD" => AccountKind.ChargeCard,
        "PREPAID" or "PREPAID_CARD" => AccountKind.Prepaid,
        "EMONEY" => AccountKind.EMoney,
        _ => AccountKind.Other,
    };

    private async Task<(decimal? Amount, DateTimeOffset? AsOf)> TryGetBalanceAsync(
        string entityId, string accountId, CancellationToken cancellationToken)
    {
        try
        {
            var balances = await GetPagedDataAsync(
                $"/data/v2/accounts/{accountId}/balances", "balances", new() { ["entity_id"] = entityId }, cancellationToken);

            var balance = balances.FirstOrDefault(b => Str(b, "type") == "CLOSING_AVAILABLE")
                ?? balances.FirstOrDefault(b => Str(b, "type") == "INTERIM_AVAILABLE")
                ?? balances.FirstOrDefault();

            if (balance is null || Num(balance["amount"], "amount") is not { } amount)
            {
                return (null, null);
            }

            return (Str(balance, "credit_debit_indicator") == "DEBIT" ? -amount : amount, Date(balance, "date_time"));
        }
        catch (BankingProviderException)
        {
            // The balance is display-only (OB-11); the import goes on without it.
            return (null, null);
        }
    }

    private async Task<List<JsonNode>> GetPagedDataAsync(
        string path, string itemsKey, Dictionary<string, string> query, CancellationToken cancellationToken)
    {
        var items = new List<JsonNode>();
        for (var page = 0; ; page++)
        {
            query["page"] = page.ToString(CultureInfo.InvariantCulture);
            query["size"] = PageSize.ToString(CultureInfo.InvariantCulture);

            var body = await GetJsonAsync($"{path}?{string.Join('&', query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"))}", cancellationToken);
            var status = Str(body, "status");
            if (status is not null && status != "OK")
            {
                // PENDING means Lean is still collecting the data; the next sync gets it.
                throw new BankingProviderException($"Lean returned {status} for {path}.");
            }

            var data = body["data"];
            items.AddRange((data?[itemsKey] as JsonArray)?.OfType<JsonNode>() ?? []);

            var totalPages = data?["page"]?["total_pages"]?.GetValue<int>() ?? 1;
            if (page + 1 >= totalPages)
            {
                return items;
            }
        }
    }

    private async Task<JsonNode> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
        return await EnsureOkAsync(response, cancellationToken) ?? new JsonObject();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new("Bearer", await tokens.GetApiTokenAsync(cancellationToken));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        try
        {
            return await http.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new BankingProviderException($"Lean could not be reached ({method} {path}).", exception);
        }
    }

    private static async Task<JsonNode?> EnsureOkAsync(HttpResponseMessage response, CancellationToken cancellationToken, bool ignoreNotFound = false)
    {
        var body = await ReadAsync(response, cancellationToken);

        // BR-C6: an expired or revoked consent comes back in the status field with a 4xx,
        // as "CONSENT_EXPIRED" or (seen in the sandbox after expiry) "USER_PERMISSIONS_NOT_GRANTED".
        if (Str(body, "status") is { } status
            && (status.Contains("CONSENT", StringComparison.OrdinalIgnoreCase) || status == "USER_PERMISSIONS_NOT_GRANTED"))
        {
            throw new ConsentExpiredException();
        }

        if (response.IsSuccessStatusCode || (ignoreNotFound && response.StatusCode == HttpStatusCode.NotFound))
        {
            return body;
        }

        throw new BankingProviderException(
            $"Lean answered {(int)response.StatusCode} for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath}: {Str(body, "message")}");
    }

    private static async Task<JsonNode?> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Str(JsonNode? node, string key) =>
        node is JsonObject obj && obj[key] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text)
            ? text
            : null;

    private static decimal? Num(JsonNode? node, string key) =>
        node is JsonObject obj && obj[key] is JsonValue value && value.TryGetValue<decimal>(out var number) ? number : null;

    private static DateTimeOffset? Date(JsonNode? node, string key) =>
        Str(node, key) is { } text && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date
            : null;
}
