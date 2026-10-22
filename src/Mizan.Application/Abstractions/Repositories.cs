using Mizan.Application.Auth;
using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Goals;
using Mizan.Domain.Notifications;
using Mizan.Domain.Transactions;
using Mizan.Domain.Users;

namespace Mizan.Application.Abstractions;

// Every query is scoped by userId, so a user can never load another user's data (BR-U2).

public interface IUserRepository
{
    Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);
    void Add(User user);
    void Remove(User user);
}

public interface ICategoryRepository
{
    Task<Category?> GetAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken);
    Task<Category> GetSystemAsync(Guid userId, CategoryKind kind, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(Guid userId, CategoryName name, CancellationToken cancellationToken);
    void Add(Category category);
    void Remove(Category category);
}

public interface IBudgetRepository
{
    Task<Budget?> GetContainingAsync(Guid userId, DateOnly date, CancellationToken cancellationToken);
    Task<Budget?> GetLatestAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Budget>> ListAsync(Guid userId, CancellationToken cancellationToken);
    void Add(Budget budget);
}

public sealed record TransactionFilter(DateOnly? From, DateOnly? To, Guid? CategoryId, int Page, int PageSize);

public interface ITransactionRepository
{
    Task<Transaction?> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> ListAsync(Guid userId, TransactionFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<Transaction>> ListByCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> SumByCategoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> SumByGoalAsync(Guid userId, CancellationToken cancellationToken);
    void Add(Transaction transaction);
    void Remove(Transaction transaction);
}

public interface IGoalRepository
{
    Task<Goal?> GetAsync(Guid userId, Guid goalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Goal>> ListAsync(Guid userId, bool includeArchived, CancellationToken cancellationToken);
    void Add(Goal goal);
}

public interface INotificationRepository
{
    Task<Notification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> ListAsync(Guid userId, bool unreadOnly, int take, CancellationToken cancellationToken);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);
    void Add(Notification notification);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyList<RefreshToken>> ListActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
    void Add(RefreshToken refreshToken);
}
