using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Goals;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals", t =>
        {
            t.HasCheckConstraint("ck_goals_target_positive", "target_amount > 0");
            t.HasCheckConstraint("ck_goals_initial_not_negative", "initial_amount >= 0");
        });

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.Name)
            .HasConversion(n => n.Value, v => GoalName.From(v))
            .HasMaxLength(GoalName.MaxLength);

        builder.MapMoney(g => g.TargetAmount, "target_amount");
        builder.MapMoney(g => g.InitialAmount, "initial_amount");

        builder.HasOne<User>().WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(g => g.UserId);
    }
}
