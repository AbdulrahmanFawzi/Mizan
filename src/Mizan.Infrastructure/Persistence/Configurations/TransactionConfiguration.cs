using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Categories;
using Mizan.Domain.Goals;
using Mizan.Domain.Transactions;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", t =>
            t.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0"));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.MapMoney(t => t.Amount, "amount");
        builder.Property(t => t.Merchant).HasMaxLength(Transaction.MerchantMaxLength);

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Category>().WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Goal>().WithMany().HasForeignKey(t => t.GoalId).OnDelete(DeleteBehavior.NoAction);

        // The most common question: "how much was spent in category X between two dates?"
        builder.HasIndex(t => new { t.UserId, t.Date });
        builder.HasIndex(t => new { t.CategoryId, t.Date });
    }
}
