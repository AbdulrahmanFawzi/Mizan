using Microsoft.EntityFrameworkCore;
using Mizan.Application.Auth;
using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Common;
using Mizan.Domain.Goals;
using Mizan.Domain.Notifications;
using Mizan.Domain.Transactions;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence;

public sealed class MizanDbContext(DbContextOptions<MizanDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MizanDbContext).Assembly);

        // Domain events live in memory only.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(AggregateRoot).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(AggregateRoot.DomainEvents));
        }
    }
}
