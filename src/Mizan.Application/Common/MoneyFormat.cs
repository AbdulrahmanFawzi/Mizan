using System.Globalization;
using Mizan.Domain.Common;

namespace Mizan.Application.Common;

internal static class MoneyFormat
{
    public static string Riyals(Money money) =>
        $"{money.Amount.ToString("#,0.##", CultureInfo.InvariantCulture)} ريال";
}
