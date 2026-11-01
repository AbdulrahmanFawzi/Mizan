using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Mizan.Application.Banking;
using Mizan.Application.Budgets;
using Mizan.Application.Categories;
using Mizan.Application.Common;
using Mizan.Application.Notifications;
using Mizan.Application.Transactions;
using Mizan.Domain.Banking;
using Mizan.Domain.Categories;
using Mizan.Domain.Notifications;
using Mizan.Domain.Transactions;
using Mizan.Infrastructure.Background;
using static Mizan.Api.Tests.FakeBankingProvider;

namespace Mizan.Api.Tests;

// MVP 1 end to end: HTTP → services → PostgreSQL, with a fake bank in place of Lean.
[Collection(ApiCollection.Name)]
public sealed class BankingJourneyTests(MizanApiFactory factory)
{
    private const string MyCurrent = "SA0310000000000000000001";
    private const string MyCard = "SA0310000000000000000002";
    private const string MySavings = "SA0310000000000000000003";

    private static readonly DateOnly Today = ApiClient.Today;

    private FakeBankingProvider Bank => factory.Bank;

    private static async Task<(ApiClient Api, string CustomerId)> StartAsync(MizanApiFactory factory)
    {
        var api = await ApiClient.RegisterAsync(factory);
        var link = await api.PostAsync<LinkSessionDto>("/api/bank-connections/link", new { });
        return (api, link.CustomerId);
    }

    private static Task<List<BankConnectionDto>> DiscoverAsync(ApiClient api) =>
        api.PostAsync<List<BankConnectionDto>>("/api/bank-connections/discover", new { });

    private static async Task<CategoryStatusDto> CategoryAsync(ApiClient api, string name) =>
        (await api.GetAsync<BudgetDto>("/api/budgets/current")).Categories.Single(c => c.Name == name);

    private static async Task<List<TransactionDto>> TransactionsAsync(ApiClient api) =>
        (await api.GetAsync<PagedResult<TransactionDto>>("/api/transactions?pageSize=100")).Items.ToList();

    [Fact]
    public async Task Link_ImportsAndClassifiesEverything()
    {
        var (api, customer) = await StartAsync(factory);
        var restaurants = await api.PostAsync<CategoryDto>("/api/categories", new { name = "المطاعم", monthlyLimit = 1000 });

        Bank.Link(customer,
        [
            (Account("cur", AccountKind.Current, MyCurrent),
            [
                Debit("t1", 120, Today, "Al Baik 0012", "RESTAURANTS_DINING"),          // spending, matched by name
                Debit("t2", 2000, Today, "Card payment", counterpartyIban: MyCard),      // own transfer: ask (OB-3)
                Credit("t3", 50, Today, "Al Baik 0012", "RESTAURANTS_DINING", BankSubType.Refund),
                Debit("t5", 400, Today, null, atm: true, fee: 1.15m),                     // ATM: spending under Other (OB-12)
                Credit("t6", 10_000, Today, "Employer"),                                  // incoming, ignored
                Debit("t7", 80, Today, "PANDA 991", status: BankStatus.Pending),          // pending counts at once (OB-2)
            ]),
            (Account("card", AccountKind.CreditCard, MyCard), [Credit("c1", 2000, Today, counterpartyIban: MyCurrent)]),
            (Account("sav", AccountKind.Savings, MySavings), [Debit("s1", 300, Today, "Something")]),
            (Account("usd", AccountKind.Current, currency: "USD"), [Debit("u1", 100, Today, "Amazon", currency: "USD")]),
        ]);

        var connection = Assert.Single(await DiscoverAsync(api));
        Assert.True(connection.HistoryImported);
        Assert.Equal([true, true, false, false], connection.Accounts.Select(a => a.IncludeInBudget));
        Assert.Equal("SA•• •••• 0001", connection.Accounts[0].IbanMasked);

        // Refund lowers the restaurants total: 120 − 50.
        Assert.Equal(70m, (await CategoryAsync(api, "المطاعم")).Spent.Amount);

        // ATM 400 + fee 1.15 + pending 80. The card payment, savings, USD and salary don't count.
        Assert.Equal(481.15m, (await CategoryAsync(api, "أخرى")).Spent.Amount);

        var review = await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review");
        var item = Assert.Single(review);
        Assert.Equal("Card payment", item.Transaction.Merchant);
        Assert.Equal("PossibleTransfer", item.Reason);

        var notifications = await api.GetAsync<List<NotificationDto>>("/api/notifications");
        Assert.Contains(notifications, n => n.Type == NotificationType.ReviewNeeded && n.Data["count"] == "1");

        // The database's budget_amount and the domain's BudgetEffect agree.
        var transactions = await TransactionsAsync(api);
        Assert.Equal(70m, transactions.Where(t => t.CategoryId == restaurants.Id).Sum(t => t.BudgetEffect.Amount));
        Assert.Equal(9, transactions.Count);
    }

    [Fact]
    public async Task TransferReview_SpendingAnswer_StartsCounting()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur", iban: MyCurrent), [Debit("t1", 700, Today, "Transfer", counterpartyIban: MyCard)]),
                             (Account("card", AccountKind.CreditCard, MyCard), [])]);
        await DiscoverAsync(api);
        Assert.Equal(0m, (await CategoryAsync(api, "أخرى")).Spent.Amount);

        var item = Assert.Single(await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review"));
        await api.PostAsync<TransactionDto>($"/api/transactions/{item.Transaction.Id}/review", new { decision = "Spending" });

        Assert.Equal(700m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
        Assert.Empty(await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review"));
    }

    [Fact]
    public async Task RememberedTransferAnswer_AnswersTheWaitingOnes_AndFutureImports()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur", iban: MyCurrent), [Debit("t1", 700, Today, "Transfer", counterpartyIban: MyCard),
                                                                Debit("t2", 400, Today, "Transfer", counterpartyIban: MyCard)]),
                             (Account("card", AccountKind.CreditCard, MyCard), [])]);
        var connection = Assert.Single(await DiscoverAsync(api));

        var review = await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review");
        Assert.Equal(2, review.Count);
        Assert.All(review, r => Assert.Equal("CreditCard account", r.TransferTo));

        // BR-R6: one answer with "remember" clears both questions.
        await api.PostAsync<TransactionDto>($"/api/transactions/{review[0].Transaction.Id}/review", new { decision = "OwnTransfer", remember = true });
        Assert.Empty(await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review"));

        Bank.For(customer).Transactions["cur"].Add(Debit("t3", 250, Today, "Transfer", counterpartyIban: MyCard));
        await api.PostAsync<SyncResultDto>($"/api/bank-connections/{connection.Id}/sync", new { });

        Assert.Empty(await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review"));
        Assert.All(await TransactionsAsync(api), t => Assert.Equal("OwnTransfer", t.Treatment));
        Assert.Equal(0m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
    }

    [Fact]
    public async Task MovingMoneyIntoSavings_CountsAsSaving_AndTakingItOutLowersIt()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer,
        [
            (Account("cur", AccountKind.Current, MyCurrent),
            [
                Debit("t1", 1000, Today, "Transfer", counterpartyIban: MySavings),   // into savings
                Credit("t2", 300, Today, "Transfer", counterpartyIban: MySavings),   // back out of savings
            ]),
            (Account("sav", AccountKind.Savings, MySavings), [Credit("s1", 1000, Today), Debit("s2", 300, Today)]),
        ]);

        await DiscoverAsync(api);

        // OB-13: no question asked, and it lands in Savings: 1000 saved − 300 taken back.
        Assert.Empty(await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review"));
        Assert.Equal(700m, (await CategoryAsync(api, "الادخار")).Spent.Amount);
        Assert.Equal(0m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
    }

    [Fact]
    public async Task IncludingAnAccount_MakesItsTransactionsCount_AndLeavingItOutUndoesIt()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("sav", AccountKind.Savings, MySavings), [Debit("s1", 300, Today, "Cash")])]);
        var connection = Assert.Single(await DiscoverAsync(api));
        var account = Assert.Single(connection.Accounts);
        Assert.Equal(0m, (await CategoryAsync(api, "أخرى")).Spent.Amount);

        var response = await api.Http.PatchAsync($"/api/bank-accounts/{account.Id}",
            System.Net.Http.Json.JsonContent.Create(new { includeInBudget = true }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(300m, (await CategoryAsync(api, "أخرى")).Spent.Amount);

        await api.Http.PatchAsync($"/api/bank-accounts/{account.Id}", System.Net.Http.Json.JsonContent.Create(new { includeInBudget = false }));
        Assert.Equal(0m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
    }

    [Fact]
    public async Task ManualThenBank_IsSuggestedAsDuplicate_AndMergedOnConfirm()
    {
        var (api, customer) = await StartAsync(factory);
        var fuel = await api.PostAsync<CategoryDto>("/api/categories", new { name = "الوقود", monthlyLimit = 500 });
        var manual = await api.PostAsync<TransactionDto>("/api/transactions", new { amount = 75, categoryId = fuel.Id, date = Today, merchant = "Aldrees" });

        Bank.Link(customer, [(Account("cur"), [Debit("t1", 75, Today, "ALDREES 22")])]);
        await DiscoverAsync(api);

        // While waiting: counted once, from the bank side (OB-10).
        var item = Assert.Single(await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review"));
        Assert.Equal("PossibleDuplicate", item.Reason);
        Assert.Equal(manual.Id, item.Candidate!.Id);
        Assert.Equal(0m, (await CategoryAsync(api, "الوقود")).Spent.Amount);
        Assert.Equal(75m, (await CategoryAsync(api, "أخرى")).Spent.Amount);

        var merged = await api.PostAsync<TransactionDto>($"/api/transactions/{item.Transaction.Id}/review", new { decision = "SameAsManual" });

        // The bank transaction keeps the user's category; the manual one is gone.
        Assert.Equal(fuel.Id, merged.CategoryId);
        Assert.Equal(75m, (await CategoryAsync(api, "الوقود")).Spent.Amount);
        Assert.Equal(0m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Http.GetAsync($"/api/transactions/{manual.Id}")).StatusCode);
    }

    [Fact]
    public async Task BankThenManual_ManualIsFlagged_AndRejectingCountsBoth()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur"), [Debit("t1", 40, Today, "Coffee")])]);
        await DiscoverAsync(api);

        var other = await CategoryAsync(api, "أخرى");
        var manual = await api.PostAsync<TransactionDto>("/api/transactions", new { amount = 40, categoryId = other.CategoryId, date = Today });
        Assert.NotNull(manual.PossibleDuplicateOf);
        Assert.Equal(40m, (await CategoryAsync(api, "أخرى")).Spent.Amount);

        var item = Assert.Single(await api.GetAsync<List<ReviewItemDto>>("/api/transactions/review"));
        await api.PostAsync<TransactionDto>($"/api/transactions/{item.Transaction.Id}/review", new { decision = "NotDuplicate" });

        Assert.Equal(80m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
    }

    [Fact]
    public async Task Resync_UpdatesInPlace_AndDropsCancelledPending()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur"), [Debit("p1", 100, Today, "Hotel", status: BankStatus.Pending), Debit("b1", 30, Today, "Bakery")])]);
        var connection = Assert.Single(await DiscoverAsync(api));
        Assert.Equal(130m, (await CategoryAsync(api, "أخرى")).Spent.Amount);

        // The hold was released (pending vanished) and the bakery amount was corrected.
        Bank.For(customer).Transactions["cur"] = [Debit("b1", 35, Today, "Bakery")];
        await api.PostAsync<SyncResultDto>($"/api/bank-connections/{connection.Id}/sync", new { });

        Assert.Equal(35m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
        Assert.Single(await TransactionsAsync(api));
    }

    [Fact]
    public async Task Refresh_IsRequestedAtMostEveryFourHours()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur"), [Debit("t1", 10, Today)])]);
        var connection = Assert.Single(await DiscoverAsync(api));

        await api.PostAsync<SyncResultDto>($"/api/bank-connections/{connection.Id}/sync", new { });
        var second = await api.PostAsync<List<SyncResultDto>>("/api/bank-connections/sync", new { });

        Assert.Equal(1, Bank.For(customer).RefreshRequests);
        Assert.Equal("Refreshing", Assert.Single(second).Status); // waits for "data ready" instead of hitting a busy provider
        Assert.Single(await TransactionsAsync(api));
    }

    [Fact]
    public async Task BankTransaction_CantBeEditedOrDeleted_ButCanBeExcluded()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur"), [Debit("t1", 60, Today, "Gift shop")])]);
        await DiscoverAsync(api);
        var transaction = Assert.Single(await TransactionsAsync(api));

        var edit = await api.Http.PutAsync($"/api/transactions/{transaction.Id}",
            System.Net.Http.Json.JsonContent.Create(new { amount = 1, categoryId = transaction.CategoryId, date = Today }));
        Assert.Equal(HttpStatusCode.BadRequest, edit.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Http.DeleteAsync($"/api/transactions/{transaction.Id}")).StatusCode);

        await api.PutAsync<TransactionDto>($"/api/transactions/{transaction.Id}/excluded", new { excluded = true });
        Assert.Equal(0m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
    }

    [Fact]
    public async Task RememberMerchant_AppliesToFutureImports()
    {
        var (api, customer) = await StartAsync(factory);
        var groceries = await api.PostAsync<CategoryDto>("/api/categories", new { name = "Groceries", monthlyLimit = 1500 });
        Bank.Link(customer, [(Account("cur"), [Debit("t1", 90, Today, "Tamimi 004")])]);
        var connection = Assert.Single(await DiscoverAsync(api));
        var first = Assert.Single(await TransactionsAsync(api));

        await api.PutAsync<TransactionDto>($"/api/transactions/{first.Id}/category", new { categoryId = groceries.Id, rememberMerchant = true });

        Bank.For(customer).Transactions["cur"].Add(Debit("t2", 45, Today, "TAMIMI-117"));
        await api.PostAsync<SyncResultDto>($"/api/bank-connections/{connection.Id}/sync", new { });

        var second = (await TransactionsAsync(api)).Single(t => t.Amount.Amount == 45);
        Assert.Equal(groceries.Id, second.CategoryId);
        Assert.Equal("MerchantRule", second.CategorySource);
        Assert.Equal(135m, (await CategoryAsync(api, "Groceries")).Spent.Amount);
    }

    [Fact]
    public async Task Unlink_WithDelete_RemovesImportedTransactions()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur"), [Debit("t1", 200, Today, "Store")])]);
        var connection = Assert.Single(await DiscoverAsync(api));

        var response = await api.Http.DeleteAsync($"/api/bank-connections/{connection.Id}?keepTransactions=false");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(Bank.For(customer).RevokedEntities);
        Assert.Empty(await TransactionsAsync(api));
        Assert.Empty(await api.GetAsync<List<BankConnectionDto>>("/api/bank-connections"));
    }

    [Fact]
    public async Task Unlink_KeepingTransactions_LeavesTheBudgetAsIs()
    {
        var (api, customer) = await StartAsync(factory);
        Bank.Link(customer, [(Account("cur"), [Debit("t1", 200, Today, "Store")])]);
        var connection = Assert.Single(await DiscoverAsync(api));

        await api.Http.DeleteAsync($"/api/bank-connections/{connection.Id}?keepTransactions=true");

        Assert.Equal(200m, (await CategoryAsync(api, "أخرى")).Spent.Amount);
    }

    [Fact]
    public async Task ExpiredConsent_StopsSyncing_AndNotifiesOnce()
    {
        var (api, customer) = await StartAsync(factory);
        var entity = Bank.Link(customer, [(Account("cur"), [Debit("t1", 10, Today)])]);
        var connection = Assert.Single(await DiscoverAsync(api));

        Bank.For(customer).ExpiredEntities.Add(entity);
        await api.PostAsync<SyncResultDto>($"/api/bank-connections/{connection.Id}/sync", new { });
        await api.PostAsync<List<SyncResultDto>>("/api/bank-connections/sync", new { });

        Assert.Equal("Expired", Assert.Single(await api.GetAsync<List<BankConnectionDto>>("/api/bank-connections")).Status);
        var notifications = await api.GetAsync<List<NotificationDto>>("/api/notifications");
        Assert.Single(notifications, n => n.Type == NotificationType.ConsentExpired);
    }

    [Fact]
    public async Task Webhook_WithoutValidSignature_IsRejected()
    {
        var client = factory.CreateClient();
        var body = """{"type":"entity.created","event_id":"e1","payload":{"id":"x","customer_id":"y"}}""";

        var response = await client.PostAsync("/api/webhooks/lean", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_EntityCreated_ActivatesTheConnection_OnlyOnce()
    {
        var (api, customer) = await StartAsync(factory);
        var entity = Bank.Link(customer, [(Account("cur"), [Debit("t1", 10, Today)])]);
        var body = $$$"""{"type":"entity.created","event_id":"evt-{{{entity}}}","payload":{"id":"{{{entity}}}","customer_id":"{{{customer}}}"}}""";

        await PostWebhookAsync(body);
        await PostWebhookAsync(body); // Lean retries: same event id, processed once.
        await factory.Services.GetRequiredService<BankingJobRunner>().ProcessInboxAsync(CancellationToken.None);

        var connection = Assert.Single(await api.GetAsync<List<BankConnectionDto>>("/api/bank-connections"));
        Assert.Equal("Active", connection.Status);
        Assert.False(connection.HistoryImported); // waits for the "data ready" webhook

        var ready = $$$"""{"type":"entity.data.refresh.updated","event_id":"ready-{{{entity}}}","payload":{"entity_id":"{{{entity}}}","status":"FINISHED"}}""";
        await PostWebhookAsync(ready);
        await factory.Services.GetRequiredService<BankingJobRunner>().ProcessInboxAsync(CancellationToken.None);

        Assert.Single(await TransactionsAsync(api));
    }

    [Fact]
    public async Task DeletingTheAccount_TellsTheProviderInTheBackground()
    {
        var (api, customer) = await StartAsync(factory);

        Assert.Equal(HttpStatusCode.NoContent, (await api.Http.DeleteAsync("/api/me")).StatusCode);
        await factory.Services.GetRequiredService<BankingJobRunner>().ProcessOutboxAsync(CancellationToken.None);

        Assert.Contains(customer, Bank.DeletedCustomers);
    }

    private async Task PostWebhookAsync(string body)
    {
        var signature = Convert.ToHexString(HMACSHA512.HashData(
            Encoding.UTF8.GetBytes(MizanApiFactory.WebhookSecret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/lean")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("lean-signature", $"sha512={signature}");

        var response = await factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
