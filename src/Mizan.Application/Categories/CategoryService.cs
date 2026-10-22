using Mizan.Application.Abstractions;
using Mizan.Application.Budgets;
using Mizan.Application.Common;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;

namespace Mizan.Application.Categories;

public sealed record CategoryDto(Guid Id, string Name, CategoryKind Kind, bool IsSystem);

public sealed record CreateCategoryCommand(string Name, decimal MonthlyLimit);

public sealed record RenameCategoryCommand(string Name);

public sealed class CategoryService(
    IUserRepository users,
    ICategoryRepository categories,
    BudgetProvisioner provisioner,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var list = await categories.ListAsync(userId, cancellationToken);
        return list.OrderBy(c => c.Kind).ThenBy(c => c.Name.Value).Select(ToDto).ToList();
    }

    // UC-21, BR-B10: a new category also gets a limit in the current budget.
    public async Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        var name = CategoryName.From(command.Name);
        await EnsureNameIsFreeAsync(userId, name, cancellationToken);

        var category = Category.CreateRegular(userId, name);
        categories.Add(category);

        var budget = await provisioner.GetOrCreateCurrentAsync(user, cancellationToken);
        budget.SetLimit(category.Id, Money.Of(command.MonthlyLimit, user.Currency));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    public async Task<CategoryDto> RenameAsync(Guid userId, Guid categoryId, RenameCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await GetAsync(userId, categoryId, cancellationToken);
        var name = CategoryName.From(command.Name);

        if (name.Normalized != category.NormalizedName)
        {
            await EnsureNameIsFreeAsync(userId, name, cancellationToken);
        }

        category.Rename(name);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    // UC-23: the CategoryDeleted handler moves its transactions to "Other" and removes the category (BR-B4).
    public async Task DeleteAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await GetAsync(userId, categoryId, cancellationToken);
        category.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken) =>
        await categories.GetAsync(userId, categoryId, cancellationToken) ?? throw new NotFoundException("Category");

    // BR-B1
    private async Task EnsureNameIsFreeAsync(Guid userId, CategoryName name, CancellationToken cancellationToken)
    {
        if (await categories.NameExistsAsync(userId, name, cancellationToken))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }
    }

    internal static CategoryDto ToDto(Category category) =>
        new(category.Id, category.Name.Value, category.Kind, category.IsSystem);
}
