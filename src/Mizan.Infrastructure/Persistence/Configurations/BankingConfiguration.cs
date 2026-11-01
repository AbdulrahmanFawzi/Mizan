using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Banking;
using Mizan.Domain.Categories;
using Mizan.Domain.Categorization;
using Mizan.Domain.Common;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;
using Mizan.Domain.Users;
using Mizan.Infrastructure.Messaging;
using Mizan.Infrastructure.Security;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class BankingCustomerConfiguration : IEntityTypeConfiguration<BankingCustomer>
{
    public void Configure(EntityTypeBuilder<BankingCustomer> builder)
    {
        builder.ToTable("bank_customers");
        builder.HasKey(c => c.UserId);
        builder.Property(c => c.Provider).HasMaxLength(20);
        builder.Property(c => c.ProviderCustomerId).HasMaxLength(100);
        builder.HasIndex(c => c.ProviderCustomerId).IsUnique();
        builder.HasOne<User>().WithOne().HasForeignKey<BankingCustomer>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BankConnectionConfiguration : IEntityTypeConfiguration<BankConnection>
{
    public void Configure(EntityTypeBuilder<BankConnection> builder)
    {
        builder.ToTable("bank_connections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ProviderEntityId).HasMaxLength(100);
        builder.Property(c => c.BankIdentifier).HasMaxLength(BankConnection.BankNameMaxLength);
        builder.Property(c => c.BankName).HasMaxLength(BankConnection.BankNameMaxLength);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(c => c.Consent, consent =>
        {
            consent.Property(x => x.GrantedAt).HasColumnName("consent_granted_at");
            consent.Property(x => x.ExpiresAt).HasColumnName("consent_expires_at");
        });

        builder.Ignore(c => c.RemindersSent);
        builder.Property<List<int>>("_remindersSent")
            .HasColumnName("reminders_sent")
            .HasColumnType("integer[]")
            .Metadata.SetValueComparer(new ValueComparer<List<int>>(
                (a, b) => a!.SequenceEqual(b!),
                list => list.Aggregate(0, HashCode.Combine),
                list => list.ToList()));

        builder.HasMany(c => c.Accounts).WithOne().HasForeignKey(a => a.ConnectionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Accounts).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.ProviderEntityId).IsUnique();

        // BR-C2: one active connection per bank per user.
        builder.HasIndex(c => new { c.UserId, c.BankIdentifier })
            .IsUnique()
            .HasFilter("status = 'Active'");
    }
}

// Not an IEntityTypeConfiguration: it needs the IBAN protector, so MizanDbContext calls it directly.
internal sealed class BankAccountConfiguration(IbanProtector ibans)
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("bank_accounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.ProviderAccountId).HasMaxLength(100);
        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Currency)
            .HasConversion(c => c.Code, code => Currency.From(code))
            .HasMaxLength(Mapping.CurrencyLength)
            .IsFixedLength();
        builder.Property(a => a.Nickname).HasMaxLength(BankAccount.NicknameMaxLength);

        // Encrypted at rest.
        builder.Property(a => a.Iban)
            .HasColumnName("iban_encrypted")
            .HasConversion(
                iban => iban == null ? null : ibans.Protect(iban.Value),
                stored => stored == null ? null : Iban.From(ibans.Unprotect(stored)));

        builder.Ignore(a => a.Balance);
        builder.Property<decimal?>("_balanceAmount").HasColumnName("balance").HasPrecision(18, 2);

        builder.HasIndex(a => new { a.ConnectionId, a.ProviderAccountId }).IsUnique();
    }
}

internal sealed class MerchantRuleConfiguration : IEntityTypeConfiguration<MerchantRule>
{
    public void Configure(EntityTypeBuilder<MerchantRule> builder)
    {
        builder.ToTable("merchant_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Merchant)
            .HasColumnName("merchant_key")
            .HasConversion(m => m.Value, value => MerchantKey.Restore(value))
            .HasMaxLength(MerchantKey.MaxLength);

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

        // Deleting a category removes the rules that pointed to it.
        builder.HasOne<Category>().WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.UserId, r.Merchant }).IsUnique();
    }
}

internal sealed class RejectedMatchConfiguration : IEntityTypeConfiguration<RejectedMatch>
{
    public void Configure(EntityTypeBuilder<RejectedMatch> builder)
    {
        builder.ToTable("rejected_matches");
        builder.HasKey(r => new { r.BankTransactionId, r.ManualTransactionId });
        builder.HasOne<Transaction>().WithMany().HasForeignKey(r => r.BankTransactionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Transaction>().WithMany().HasForeignKey(r => r.ManualTransactionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("webhook_inbox");
        builder.HasKey(m => m.EventId);
        builder.Property(m => m.EventId).HasMaxLength(100);
        builder.Property(m => m.Provider).HasMaxLength(20);
        builder.Property(m => m.Type).HasMaxLength(100);
        builder.Property(m => m.Payload).HasColumnType("jsonb");
        builder.HasIndex(m => new { m.ProcessedAt, m.NextAttemptAt });
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).HasMaxLength(100);
        builder.Property(m => m.Payload).HasColumnType("jsonb");
        builder.HasIndex(m => new { m.ProcessedAt, m.NextAttemptAt });
    }
}
