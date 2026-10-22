using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Budgets;
using Mizan.Domain.Categories;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets", t =>
            t.HasCheckConstraint("ck_budgets_period", "period_end >= period_start"));

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.ComplexProperty(b => b.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("period_start");
            period.Property(p => p.End).HasColumnName("period_end");
        });

        builder.MapMoney(b => b.PlannedIncome, "planned_income");

        builder.HasOne<User>().WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Allocations)
            .WithOne()
            .HasForeignKey("BudgetId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Allocations)
            .HasField("_allocations")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}

internal sealed class BudgetAllocationConfiguration : IEntityTypeConfiguration<BudgetAllocation>
{
    public void Configure(EntityTypeBuilder<BudgetAllocation> builder)
    {
        builder.ToTable("budget_allocations", t =>
            t.HasCheckConstraint("ck_budget_allocations_limit", "limit_amount >= 0"));

        builder.Property<Guid>("BudgetId");
        builder.HasKey("BudgetId", nameof(BudgetAllocation.CategoryId));

        builder.MapMoney(a => a.Limit, "limit_amount");

        builder.Property(a => a.HighestThresholdSent)
            .HasConversion(t => t!.Percent, percent => AlertThreshold.FromPercent(percent)); // EF never passes null to a converter

        // NO ACTION (checked at the end of the statement) so deleting a user can cascade everywhere.
        builder.HasOne<Category>().WithMany().HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.NoAction);
    }
}
