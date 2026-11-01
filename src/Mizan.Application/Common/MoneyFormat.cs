using System.Globalization;
using Mizan.Domain.Common;

namespace Mizan.Application.Common;

internal static class MoneyFormat
{
    public static string Riyals(Money money)
    {
        var amount = money.Amount.ToString("#,0.##", CultureInfo.InvariantCulture);
        return Lang.Pick($"{amount} ريال", $"{amount} SAR");
    }
}
