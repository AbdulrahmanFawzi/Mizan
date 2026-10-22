using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Budgets;
using Mizan.Domain.Common;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t =>
        {
            t.HasCheckConstraint("ck_users_monthly_income_positive", "monthly_income > 0");
            t.HasCheckConstraint("ck_users_period_start_day", "period_start_day BETWEEN 1 AND 31");
        });

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Name)
            .HasConversion(n => n.Value, v => PersonName.From(v))
            .HasMaxLength(PersonName.MaxLength);

        builder.Property(u => u.Email)
            .HasConversion(e => e.Value, v => Email.From(v))
            .HasMaxLength(Email.MaxLength);
        builder.HasIndex(u => u.Email).IsUnique(); // BR-U1

        builder.Property(u => u.PasswordHash);

        builder.Property(u => u.Currency)
            .HasConversion(c => c.Code, code => Currency.From(code))
            .HasMaxLength(Mapping.CurrencyLength)
            .IsFixedLength();

        builder.MapMoney(u => u.MonthlyIncome, "monthly_income");

        builder.Property(u => u.PeriodStartDay)
            .HasConversion(d => d.Value, v => PeriodStartDay.From(v));
    }
}
