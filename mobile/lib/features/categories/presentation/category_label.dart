import '../../../l10n/app_localizations.dart';
import '../data/category.dart';

// The API names the two system categories in Arabic ("أخرى", "الادخار").
// They are translated here by their kind; categories the user created keep their own name.
String categoryLabel(Category category, AppLocalizations l10n) => switch (category.kind) {
      CategoryKind.other => l10n.categoryOther,
      CategoryKind.savings => l10n.categorySavings,
      CategoryKind.regular => category.name,
    };
