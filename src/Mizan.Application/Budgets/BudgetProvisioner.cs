using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Budgets;
using Mizan.Domain.Users;

namespace Mizan.Application.Budgets;

// UC-25 ("Time starts a new budget period") done lazily: the budget for the current period
// is created the first time it is needed, by copying the latest one (BR-P6).
public sealed class BudgetProvisioner(IBudgetRepository budgets, Clock clock)
{
    public async Task<Budget> GetOrCreateCurrentAsync(User user, CancellationToken cancellationToken)
    {
        var today = clock.Today;

        var current = await budgets.GetContainingAsync(user.Id, today, cancellationToken);
        if (current is not null)
        {
            return current;
        }

        var latest = await budgets.GetLatestAsync(user.Id, cancellationToken);
        Budget budget;

        if (latest is null)
        {
            budget = Budget.Create(user.Id, BudgetPeriod.Containing(today, user.PeriodStartDay), user.MonthlyIncome);
        }
        else
        {
            // If the user was away for a few periods, skip ahead to the one containing today.
            var period = latest.Period.Next(user.PeriodStartDay);
            while (period.End < today)
            {
                period = period.Next(user.PeriodStartDay);
            }

            budget = latest.CreateNext(period, user.MonthlyIncome);
        }

        budgets.Add(budget);
        return budget;
    }
}
