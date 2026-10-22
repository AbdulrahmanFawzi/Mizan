using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Common;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal static class Mapping
{
    public const int CurrencyLength = 3;

    // Money is stored as two columns: <name> numeric(18,2) and <name>_currency char(3).
    public static void MapMoney<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, Money>> property,
        string columnName)
        where TEntity : class
    {
        // EF's signature allows a nullable expression; Money here is required (non-nullable property).
#pragma warning disable CS8620
        builder.ComplexProperty(property, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName(columnName)
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName($"{columnName}_currency")
                .HasConversion(c => c.Code, code => Currency.From(code))
                .HasMaxLength(CurrencyLength)
                .IsFixedLength();
        });
#pragma warning restore CS8620
    }
}
