using System.Globalization;

namespace Mizan.Application.Common;

// The language of the current request. The API sets it from Accept-Language; Arabic unless the app asks for English.
public static class Lang
{
    public static bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    public static string Pick(string arabic, string english) => IsEnglish ? english : arabic;
}
