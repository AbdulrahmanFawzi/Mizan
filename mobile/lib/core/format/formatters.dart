import 'package:flutter/widgets.dart';
import 'package:intl/intl.dart';

// Formatting depends on the current language: "1,240 SAR" vs "1,240 ر.س".
extension Formatters on BuildContext {
  String get _locale => Localizations.localeOf(this).toString();

  String money(double amount, {String currency = 'SAR'}) {
    final number = NumberFormat.decimalPatternDigits(
      locale: _locale,
      decimalDigits: amount == amount.roundToDouble() ? 0 : 2,
    ).format(amount);
    final symbol = currency == 'SAR' && _locale.startsWith('ar') ? 'ر.س' : currency;
    return '$number $symbol';
  }

  String shortDate(DateTime date) => DateFormat.MMMd(_locale).format(date);

  // With the year: for dates that may be far away, like a goal's target date.
  String mediumDate(DateTime date) => DateFormat.yMMMd(_locale).format(date);

  String longDate(DateTime date) => DateFormat.yMMMEd(_locale).format(date);
}
