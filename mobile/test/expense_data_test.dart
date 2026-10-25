import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/core/format/json.dart';
import 'package:mizan/core/widgets/validators.dart';
import 'package:mizan/features/transactions/data/transaction.dart';
import 'package:mizan/l10n/app_localizations_en.dart';

void main() {
  test('apiDate always sends yyyy-MM-dd with padding', () {
    expect(apiDate(DateTime(2026, 3, 5)), '2026-03-05');
    expect(apiDate(DateTime(2026, 12, 25)), '2026-12-25');
  });

  test('ExpenseInput leaves out an empty merchant', () {
    final json = ExpenseInput(amount: 50, categoryId: 'c1', date: DateTime(2026, 10, 25), merchant: '   ').toJson();

    expect(json, {'amount': 50.0, 'categoryId': 'c1', 'date': '2026-10-25'});
  });

  test('ExpenseInput trims the merchant', () {
    final json = ExpenseInput(amount: 50, categoryId: 'c1', date: DateTime(2026, 10, 25), merchant: ' Al Baik ').toJson();

    expect(json['merchant'], 'Al Baik');
  });

  test('Transaction.fromJson reads a goal contribution', () {
    final transaction = Transaction.fromJson({
      'id': 't1',
      'amount': {'amount': 6500, 'currency': 'SAR'},
      'categoryId': 'savings',
      'date': '2026-10-22',
      'merchant': null,
      'goalId': 'g1',
    });

    expect(transaction.amount, 6500);
    expect(transaction.date, DateTime(2026, 10, 22));
    expect(transaction.isGoalContribution, isTrue);
  });

  group('Validators.positiveAmount', () {
    final l10n = AppLocalizationsEn();

    for (final valid in ['50', '0.5', '10.25', ' 100 ']) {
      test('accepts "$valid"', () => expect(Validators.positiveAmount(valid, l10n), isNull));
    }

    for (final invalid in ['', '0', '-5', '10.005', 'abc', '1,000']) {
      test('rejects "$invalid"', () => expect(Validators.positiveAmount(invalid, l10n), isNotNull));
    }
  });
}
