using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(MizanDbContext db) : IUserRepository
{
    public Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}
