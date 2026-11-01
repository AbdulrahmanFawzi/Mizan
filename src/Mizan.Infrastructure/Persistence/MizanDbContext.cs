using Microsoft.EntityFrameworkCore;
using Mizan.Application.Auth;
using Mizan.Domain.Banking;
using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Categorization;
using Mizan.Domain.Common;
using Mizan.Domain.Goals;
using Mizan.Domain.Importing;
using Mizan.Domain.Notifications;
using Mizan.Domain.Transactions;
using Mizan.Domain.Users;
using Mizan.Infrastructure.Messaging;
using Mizan.Infrastructure.Persistence.Configurations;
using Mizan.Infrastructure.Security;

namespace Mizan.Infrastructure.Persistence;

public sealed class MizanDbContext(DbContextOptions<MizanDbContext> options, IbanProtector ibans) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BankingCustomer> BankingCustomers => Set<BankingCustomer>();
    public DbSet<BankConnection> BankConnections => Set<BankConnection>();
    public DbSet<MerchantRule> MerchantRules => Set<MerchantRule>();
    public DbSet<RejectedMatch> RejectedMatches => Set<RejectedMatch>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MizanDbContext).Assembly);
        new BankAccountConfiguration(ibans).Configure(modelBuilder.Entity<BankAccount>());

        // Domain events live in memory only.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(AggregateRoot).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(AggregateRoot.DomainEvents));
        }
    }
}
