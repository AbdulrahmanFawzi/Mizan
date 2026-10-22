using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Domain.Categories;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(MizanDbContext db) : ICategoryRepository
{
    public Task<Category?> GetAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken) =>
        db.Categories.SingleOrDefaultAsync(c => c.UserId == userId && c.Id == categoryId, cancellationToken);

    public Task<Category> GetSystemAsync(Guid userId, CategoryKind kind, CancellationToken cancellationToken) =>
        db.Categories.SingleAsync(c => c.UserId == userId && c.Kind == kind, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Categories.Where(c => c.UserId == userId).ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(Guid userId, CategoryName name, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(c => c.UserId == userId && c.NormalizedName == name.Normalized, cancellationToken);

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
