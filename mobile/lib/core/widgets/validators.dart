import '../../l10n/app_localizations.dart';

// Same rules as the API, checked early so the user gets instant feedback.
abstract final class Validators {
  static const minPasswordLength = 8;

  static String? required(String? value, AppLocalizations l10n) =>
      (value == null || value.trim().isEmpty) ? l10n.fieldRequired : null;

  static String? email(String? value, AppLocalizations l10n) {
    if (required(value, l10n) case final error?) return error;
    return RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(value!.trim()) ? null : l10n.invalidEmail;
  }

  static String? password(String? value, AppLocalizations l10n) {
    if (required(value, l10n) case final error?) return error;
    return value!.length < minPasswordLength ? l10n.passwordTooShort(minPasswordLength) : null;
  }

  // Money: greater than zero with at most two decimals (halalas), like the API (BR-M2, BR-T1).
  static String? positiveAmount(String? value, AppLocalizations l10n) {
    final text = value?.trim() ?? '';
    final valid = RegExp(r'^\d+(\.\d{1,2})?$').hasMatch(text) && double.parse(text) > 0;
    return valid ? null : l10n.invalidAmount;
  }

  // A category limit may be zero ("I don't plan to spend on this"), but not negative (BR-B2).
  static String? nonNegativeAmount(String? value, AppLocalizations l10n) {
    final text = value?.trim() ?? '';
    return RegExp(r'^\d+(\.\d{1,2})?$').hasMatch(text) ? null : l10n.invalidLimit;
  }
}
