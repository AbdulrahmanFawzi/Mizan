using System.Globalization;
using Mizan.Domain.Common;

namespace Mizan.Application.Common;

internal static class MoneyFormat
{
    // whole: for estimates, where halalas would look falsely precise.
    public static string Riyals(Money money, bool whole = false)
    {
        var amount = money.Amount.ToString(whole ? "#,0" : "#,0.##", CultureInfo.InvariantCulture);
        return Lang.Pick($"{amount} ريال", $"{amount} SAR");
    }
}
