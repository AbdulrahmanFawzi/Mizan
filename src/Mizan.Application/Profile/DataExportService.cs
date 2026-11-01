using Mizan.Application.Abstractions;
using Mizan.Application.Banking;
using Mizan.Application.Categories;
using Mizan.Application.Common;
using Mizan.Application.Goals;
using Mizan.Application.Notifications;
using Mizan.Application.Transactions;

namespace Mizan.Application.Profile;

public sealed record BudgetExportDto(DateOnly Start, DateOnly End, MoneyDto PlannedIncome, IReadOnlyList<BudgetLimitExportDto> Limits);

public sealed record BudgetLimitExportDto(Guid CategoryId, MoneyDto Limit);

public sealed record MerchantRuleExportDto(string Merchant, Guid CategoryId);

// Everything we keep about the user, in the same shapes the app uses. IBANs stay masked.
public sealed record DataExportDto(
    DateTimeOffset ExportedAt,
    ProfileDto Profile,
    IReadOnlyList<CategoryDto> Categories,
    IReadOnlyList<BudgetExportDto> Budgets,
    IReadOnlyList<TransactionDto> Transactions,
    IReadOnlyList<GoalDto> Goals,
    IReadOnlyList<NotificationDto> Notifications,
    IReadOnlyList<BankConnectionDto> BankConnections,
    IReadOnlyList<MerchantRuleExportDto> MerchantRules);

// UC-09 (PDPL right of access): a copy of all the user's data in one file.
public sealed class DataExportService(
    ProfileService profile,
    CategoryService categories,
    GoalService goals,
    BankConnectionService bankConnections,
    IBudgetRepository budgets,
    ITransactionRepository transactions,
    INotificationRepository notifications,
    IMerchantRuleRepository merchantRules,
    Clock clock)
{
    public async Task<DataExportDto> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var (allTransactions, _) = await transactions.ListAsync(
            userId, new TransactionFilter(null, null, null, 1, int.MaxValue), cancellationToken);

        return new DataExportDto(
            clock.Now,
            await profile.GetAsync(userId, cancellationToken),
            await categories.ListAsync(userId, cancellationToken),
            (await budgets.ListAsync(userId, cancellationToken))
                .Select(b => new BudgetExportDto(
                    b.Period.Start,
                    b.Period.End,
                    MoneyDto.From(b.PlannedIncome),
                    b.Allocations.Select(a => new BudgetLimitExportDto(a.CategoryId, MoneyDto.From(a.Limit))).ToList()))
                .ToList(),
            allTransactions.Select(TransactionService.ToDto).ToList(),
            await goals.ListAsync(userId, includeArchived: true, cancellationToken),
            (await notifications.ListAsync(userId, unreadOnly: false, int.MaxValue, cancellationToken))
                .Select(NotificationService.ToDto).ToList(),
            await bankConnections.ListAsync(userId, cancellationToken),
            (await merchantRules.ListAsync(userId, cancellationToken))
                .Select(r => new MerchantRuleExportDto(r.Key.Value, r.Value)).ToList());
    }
}
