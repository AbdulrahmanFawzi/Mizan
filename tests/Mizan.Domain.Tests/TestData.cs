using System.Globalization;
using Mizan.Domain.Common;

namespace Mizan.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 30, 9, 0, 0, TimeSpan.FromHours(3));
    public static readonly DateOnly Today = new(2026, 10, 30);

    public static DateOnly D(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static Money Sar(decimal amount) => Money.Of(amount, Currency.SAR);
}
