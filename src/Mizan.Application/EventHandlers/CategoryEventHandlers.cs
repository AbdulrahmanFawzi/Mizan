using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Categories;

namespace Mizan.Application.EventHandlers;

// BR-B4: the category's transactions move to "Other", its limits disappear, then the category is removed.
// Everything happens in the same database transaction as the delete request.
public sealed class MoveTransactionsOnCategoryDeleted(
    ICategoryRepository categories,
    ITransactionRepository transactions,
    IBudgetRepository budgets,
    Clock clock) : IDomainEventHandler<CategoryDeleted>
{
    public async Task HandleAsync(CategoryDeleted e, CancellationToken cancellationToken)
    {
        var category = await categories.GetAsync(e.UserId, e.CategoryId, cancellationToken);
        if (category is null)
        {
            return;
        }

        var other = await categories.GetSystemAsync(e.UserId, CategoryKind.Other, cancellationToken);

        foreach (var transaction in await transactions.ListByCategoryAsync(e.UserId, e.CategoryId, cancellationToken))
        {
            transaction.MoveToCategory(other.Id, clock.Now);
        }

        foreach (var budget in await budgets.ListAsync(e.UserId, cancellationToken))
        {
            budget.RemoveAllocation(e.CategoryId);
        }

        categories.Remove(category);
    }
}
