using System.Text.RegularExpressions;
using Mizan.Application.Abstractions;
using Mizan.Application.Common;

namespace Mizan.Api.Infrastructure;

// The message the user sees for a known error, in the request's language.
// The code throws in English; Arabic comes from this table. A message missing here stays in English.
internal static partial class ErrorText
{
    public static string For(Exception exception) => exception switch
    {
        // Never show the provider's technical details.
        ConsentExpiredException => Lang.Pick("انتهى ربط البنك. أعد الربط من الإعدادات.", "Your bank link has ended. Link it again from Settings."),
        BankingProviderException => Lang.Pick("خدمة البنك ما ترد الحين. حاول بعد شوي.", "The bank service isn't answering right now. Try again shortly."),
        NotFoundException notFound when !Lang.IsEnglish =>
            $"{Resources.GetValueOrDefault(notFound.Resource, notFound.Resource)} غير موجود.",
        _ when !Lang.IsEnglish => ToArabic(exception.Message),
        _ => exception.Message,
    };

    internal static string ToArabic(string message)
    {
        foreach (var (pattern, arabic) in Table)
        {
            var match = pattern.Match(message);
            if (match.Success)
            {
                return match.Result(arabic).Replace("{field}", Fields.GetValueOrDefault(match.Groups["field"].Value, match.Groups["field"].Value));
            }
        }

        return message;
    }

    private static readonly Dictionary<string, string> Resources = new()
    {
        ["User"] = "المستخدم", ["Transaction"] = "العملية", ["Category"] = "الفئة", ["Goal"] = "الهدف",
        ["Budget"] = "الميزانية", ["Notification"] = "الإشعار", ["Bank connection"] = "ربط البنك", ["Bank account"] = "الحساب البنكي",
    };

    private static readonly Dictionary<string, string> Fields = new()
    {
        ["Name"] = "الاسم", ["Category name"] = "اسم الفئة", ["Goal name"] = "اسم الهدف", ["Merchant"] = "اسم التاجر",
        ["Note"] = "الملاحظة", ["Bank identifier"] = "رمز البنك",
    };

    private static Regex R(string pattern) => new($"^{pattern}$", RegexOptions.CultureInvariant);

    private static readonly (Regex Pattern, string Arabic)[] Table =
    [
        // Sign in and account
        (R(@"Invalid credentials\."), "الإيميل أو كلمة المرور غير صحيحة."),
        (R(@"(Invalid refresh token|Refresh token has expired)\."), "انتهت الجلسة. سجّل دخولك من جديد."),
        (R(@"An account with this email already exists\."), "فيه حساب مسجّل بهذا الإيميل."),
        (R(@"Password must be between (\d+) and (\d+) characters\."), "كلمة المرور لازم تكون بين $1 و$2 حرف."),
        (R(@"The code is wrong or has expired\."), "الرمز غلط أو انتهت صلاحيته."),
        (R(@"Verify your email first\."), "أكّد إيميلك أول."),
        (R(@"'(.*)' is not a valid email address\."), "'$1' مو إيميل صحيح."),
        (R(@"Monthly income must be greater than zero\."), "الدخل الشهري لازم يكون أكبر من صفر."),
        (R(@"Monthly income must be in (\w+)\."), "الدخل الشهري لازم يكون بـ $1."),
        (R(@"Period start day must be between 1 and 31, but was .*\."), "يوم بداية الفترة لازم يكون بين 1 و31."),
        (R(@"Currency (\w+) is not supported\."), "العملة $1 غير مدعومة."),
        (R(@"'(.*)' is not a valid currency code\."), "'$1' مو رمز عملة صحيح."),

        // Text fields
        (R(@"(?<field>.+) is required\."), "{field} مطلوب."),
        (R(@"(?<field>.+) must be at most (\d+) characters\."), "{field} لازم ما يزيد عن $2 حرف."),

        // Money, budgets and categories
        (R(@"Amount must be greater than zero\."), "المبلغ لازم يكون أكبر من صفر."),
        (R(@"Amount .* has more than (\d+) decimal places\."), "المبلغ ما يقبل أكثر من $1 خانات عشرية."),
        (R(@"Cannot combine (\w+) with (\w+)\."), "ما ينفع نجمع $1 مع $2."),
        (R(@"Budget amounts must be in (\w+)\."), "مبالغ الميزانية لازم تكون بـ $1."),
        (R(@"A category limit cannot be negative\."), "حد الفئة ما يكون بالسالب."),
        (R(@"A category named '(.*)' already exists\."), "فيه فئة اسمها '$1' من قبل."),
        (R(@"This item already exists\."), "هذا موجود من قبل."),
        (R(@"The '(.*)' category cannot be renamed\."), "فئة '$1' ما يتغير اسمها."),
        (R(@"The '(.*)' category cannot be deleted\."), "فئة '$1' ما تنحذف."),
        (R(@"The next budget must start after the current one ends\."), "الميزانية الجاية لازم تبدأ بعد نهاية الحالية."),

        // Expenses
        (R(@"Category is required\."), "اختر فئة."),
        (R(@"Date cannot be in the future\."), "التاريخ ما يكون في المستقبل."),
        (R(@"'from' must not be after 'to'\."), "تاريخ البداية لازم يكون قبل تاريخ النهاية."),
        (R(@"Page must be at least 1 and page size between 1 and \d+\."), "رقم الصفحة أو حجمها غير صحيح."),

        // Goals
        (R(@"Target amount must be greater than zero\."), "المبلغ المستهدف لازم يكون أكبر من صفر."),
        (R(@"Initial amount cannot be negative\."), "المبلغ المبدئي ما يكون بالسالب."),
        (R(@"Target and initial amounts must use the same currency\."), "المبلغ المستهدف والمبدئي لازم يكونون بنفس العملة."),
        (R(@"Target date must be in the future\."), "تاريخ الهدف لازم يكون في المستقبل."),
        (R(@"This goal is archived\."), "هذا الهدف مؤرشف."),
        (R(@"A goal contribution must stay in the Savings category\."), "مبلغ الهدف لازم يبقى في فئة الادخار."),

        // Banks
        (R(@"A bank transaction can't be (edited|deleted)\. Change its category or exclude it instead\."), "عمليات البنك ما تنعدل ولا تنحذف. غيّر فئتها أو استبعدها من الميزانية."),
        (R(@"This only applies to bank transactions\."), "هذا الخيار لعمليات البنك فقط."),
        (R(@"Accounts in (\w+) can't be part of a (\w+) budget\."), "حسابات $1 ما تدخل في ميزانية بالـ $2."),
        (R(@"Account not found in this connection\."), "الحساب مو موجود في هذا الربط."),
        (R(@"This bank connection is not active\."), "ربط البنك هذا مو فعّال."),
        (R(@"This bank was refreshed recently\."), "تحدّث البنك قبل شوي. حاول بعدين."),
        (R(@"A removed bank can't be reconnected\. Link it again instead\."), "البنك المحذوف ما يرجع. اربطه من جديد."),
        (R(@"'(.*)' is not a valid IBAN\."), "'$1' مو آيبان صحيح."),
        (R(@"(This|These) transactions? (is|are) not waiting to be compared\."), "هذي العملية ما تنتظر مقارنة."),
        (R(@"This transaction is not waiting for a transfer decision\."), "هذي العملية ما تنتظر قرار تحويل."),
        (R(@"One of these transactions is already waiting for review\."), "وحدة من هالعمليات تنتظر مراجعتك من قبل."),
        (R(@"This transaction has no merchant name to remember\."), "العملية ما فيها اسم تاجر نقدر نتذكره."),
        (R(@"This transfer has no account to remember\."), "التحويل ما فيه حساب نقدر نتذكره."),
    ];
}
