namespace Mizan.Application.Abstractions;

// Saves all changes in one database transaction and dispatches the domain events they raised.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
