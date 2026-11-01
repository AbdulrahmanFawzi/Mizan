using Mizan.Domain.Categories;
using Mizan.Domain.Categorization;
using Mizan.Domain.Transactions;

namespace Mizan.Domain.Importing;

public sealed record CategoryChoice(Guid Id, string Name, CategoryKind Kind);

public sealed record CategoryDecision(Guid CategoryId, CategorySource Source);

// Picks a category for a bank transaction (BR-K1): the user's merchant rule, then the provider's
// guess matched to one of the user's own categories, then "Other".
public static class CategoryResolver
{
    // Provider category codes (Lean, V4) and words that identify the same idea in a user's category name.
    private static readonly Dictionary<string, string[]> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GROCERIES"] = ["بقالة", "سوبر", "مقاضي", "تموين", "grocer", "supermarket"],
        ["RESTAURANTS_DINING"] = ["مطعم", "مطاعم", "أكل", "اكل", "طعام", "قهوة", "كافي", "food", "restaurant", "dining", "coffee"],
        ["TRANSPORT"] = ["وقود", "بنزين", "سيارة", "مواصلات", "نقل", "fuel", "gas", "car", "transport"],
        ["RETAIL"] = ["تسوق", "ملابس", "مشتريات", "shopping", "clothes", "retail"],
        ["RENT_AND_SERVICES"] = ["سكن", "إيجار", "ايجار", "فاتورة", "فواتير", "كهرباء", "ماء", "جوال", "rent", "bill", "utilit", "housing"],
        ["HEALTH_AND_WELLBEING"] = ["صحة", "صيدلية", "طبيب", "مستشفى", "health", "pharmacy", "doctor"],
        ["EDUCATION"] = ["تعليم", "دراسة", "مدرسة", "كتب", "education", "school", "course"],
        ["TRAVEL"] = ["سفر", "رحلة", "طيران", "travel", "trip", "flight"],
        ["GOVERNMENT"] = ["حكوم", "رسوم حكومية", "government"],
        ["BANK_FEES_AND_CHARGES"] = ["رسوم", "عمولة", "fees", "charges"],
    };

    public static CategoryDecision Resolve(
        MerchantKey? merchant,
        ProviderCategory? providerCategory,
        bool isCashWithdrawal,
        IReadOnlyDictionary<MerchantKey, Guid> merchantRules,
        IReadOnlyList<CategoryChoice> categories)
    {
        ArgumentNullException.ThrowIfNull(merchantRules);
        ArgumentNullException.ThrowIfNull(categories);

        var other = categories.Single(c => c.Kind == CategoryKind.Other);

        // 1. The user's own rule wins (BR-K2).
        if (merchant is not null && merchantRules.TryGetValue(merchant, out var ruleCategory)
            && categories.Any(c => c.Id == ruleCategory))
        {
            return new CategoryDecision(ruleCategory, CategorySource.MerchantRule);
        }

        // BR-X12: cash withdrawals go to "Other" unless a rule says otherwise.
        if (isCashWithdrawal)
        {
            return new CategoryDecision(other.Id, CategorySource.Fallback);
        }

        // 2. The provider's guess, only if it's confident (BR-K4) and matches one of the user's categories.
        //    Savings is never picked automatically: it belongs to goals (BR-K5).
        if (providerCategory is { IsReliable: true } && Keywords.TryGetValue(providerCategory.Code, out var words))
        {
            var match = categories.FirstOrDefault(c =>
                c.Kind == CategoryKind.Regular && words.Any(w => c.Name.Contains(w, StringComparison.OrdinalIgnoreCase)));

            if (match is not null)
            {
                return new CategoryDecision(match.Id, CategorySource.Provider);
            }
        }

        // 3. Nothing fits.
        return new CategoryDecision(other.Id, CategorySource.Fallback);
    }
}
