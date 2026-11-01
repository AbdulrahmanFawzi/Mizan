using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Banking;
using Mizan.Domain.Categories;
using Mizan.Domain.Categorization;
using Mizan.Domain.Goals;
using Mizan.Domain.Transactions;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public const string BudgetAmount = "BudgetAmount";

    // The same rule as Transaction.BudgetEffect, computed by Postgres so totals stay a fast SUM.
    // ApiTests check that both give the same number.
    private const string BudgetAmountSql = """
        CASE
            WHEN is_excluded OR review = 'PossibleTransfer' OR possible_duplicate_of IS NOT NULL THEN 0
            WHEN treatment = 'Spending' THEN amount + COALESCE(fee, 0)
            WHEN treatment = 'Refund' THEN -amount
            ELSE 0
        END
        """;

    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", t =>
            t.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0"));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.MapMoney(t => t.Amount, "amount");
        builder.Property(t => t.Merchant).HasMaxLength(Transaction.MerchantMaxLength);
        builder.Property(t => t.MerchantKey)
            .HasConversion(m => m == null ? null : m.Value, value => value == null ? null : MerchantKey.Restore(value))
            .HasMaxLength(MerchantKey.MaxLength);
        builder.Property(t => t.Note).HasMaxLength(Transaction.NoteMaxLength);

        builder.Property(t => t.Source).HasConversion<string>().HasMaxLength(10).HasDefaultValue(TransactionSource.Manual);
        builder.Property(t => t.Direction).HasConversion<string>().HasMaxLength(10).HasDefaultValue(TransactionDirection.Debit);
        builder.Property(t => t.Treatment).HasConversion<string>().HasMaxLength(20).HasDefaultValue(Treatment.Spending);
        builder.Property(t => t.CategorySource).HasConversion<string>().HasMaxLength(20).HasDefaultValue(CategorySource.User);
        builder.Property(t => t.BankStatus).HasConversion<string>().HasMaxLength(10);
        builder.Property(t => t.Review).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.BankSubType).HasConversion<string>().HasMaxLength(20);

        // Bank reference and fee: plain columns behind the domain's computed properties.
        builder.Ignore(t => t.Bank);
        builder.Ignore(t => t.Fee);
        builder.Ignore(t => t.BudgetEffect);
        builder.Ignore(t => t.IsFromBank);
        builder.Property<Guid?>("_bankConnectionId").HasColumnName("bank_connection_id");
        builder.Property<Guid?>("_bankAccountId").HasColumnName("bank_account_id");
        builder.Property<string?>("_providerTransactionId").HasColumnName("provider_transaction_id").HasMaxLength(100);
        builder.Property<decimal?>("_feeAmount").HasColumnName("fee").HasPrecision(18, 2);

        builder.Property<decimal>(BudgetAmount)
            .HasColumnName("budget_amount")
            .HasPrecision(18, 2)
            .HasComputedColumnSql(BudgetAmountSql, stored: true);

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Category>().WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Goal>().WithMany().HasForeignKey(t => t.GoalId).OnDelete(DeleteBehavior.NoAction);

        // BR-C9: when the user keeps a removed bank's transactions, they simply lose the link.
        builder.HasOne<BankConnection>().WithMany().HasForeignKey("_bankConnectionId").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<BankAccount>().WithMany().HasForeignKey("_bankAccountId").OnDelete(DeleteBehavior.SetNull);

        // The most common question: "how much was spent in category X between two dates?"
        builder.HasIndex(t => new { t.UserId, t.Date });
        builder.HasIndex(t => new { t.CategoryId, t.Date });

        // BR-S5: one row per bank transaction.
        builder.HasIndex("_bankConnectionId", "_providerTransactionId").IsUnique();
        builder.HasIndex(t => new { t.UserId, t.MerchantKey });
    }
}
