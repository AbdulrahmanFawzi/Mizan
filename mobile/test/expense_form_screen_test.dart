import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/categories/data/category.dart';
import 'package:mizan/features/transactions/application/transactions_controller.dart';
import 'package:mizan/features/transactions/data/transaction.dart';
import 'package:mizan/features/transactions/presentation/expense_form_screen.dart';

import 'helpers/test_app.dart';

// Records what the form would send instead of calling the API.
class FakeExpenseActions implements ExpenseActions {
  final added = <ExpenseInput>[];
  final updated = <(String, ExpenseInput)>[];
  final deleted = <String>[];

  @override
  Future<void> add(ExpenseInput input) async => added.add(input);

  @override
  Future<void> update(String id, ExpenseInput input) async => updated.add((id, input));

  @override
  Future<void> delete(String id) async => deleted.add(id);
}

const _categories = [
  Category(id: 'other', name: 'أخرى', kind: CategoryKind.other),
  Category(id: 'savings', name: 'الادخار', kind: CategoryKind.savings),
  Category(id: 'fuel', name: 'Fuel', kind: CategoryKind.regular),
];

void main() {
  late FakeExpenseActions actions;

  setUp(() => actions = FakeExpenseActions());

  Future<void> pump(WidgetTester tester, {Transaction? existing}) async {
    await tester.pumpWidget(testApp(
      ExpenseFormScreen(existing: existing),
      overrides: [
        categoriesProvider.overrideWith((ref) async => _categories),
        expenseActionsProvider.overrideWithValue(actions),
      ],
    ));
    await tester.pumpAndSettle();
  }

  testWidgets('adds an expense in the first category the user created', (tester) async {
    await pump(tester);

    await tester.enterText(find.byKey(const Key('amountField')), '50');
    await tester.enterText(find.byKey(const Key('merchantField')), 'XYZ Fuel');
    await tester.tap(find.byKey(const Key('saveExpense')));
    await tester.pumpAndSettle();

    final input = actions.added.single;
    expect(input.amount, 50);
    expect(input.categoryId, 'fuel');
    expect(input.merchant, 'XYZ Fuel');
    expect(input.date, DateUtils.dateOnly(DateTime.now()));
  });

  testWidgets('system categories are shown translated', (tester) async {
    await pump(tester);

    await tester.tap(find.text('Fuel'));
    await tester.pumpAndSettle();

    expect(find.text('Other'), findsWidgets);
    expect(find.text('Savings'), findsWidgets);
    expect(find.text('أخرى'), findsNothing);
  });

  testWidgets('rejects an invalid amount without calling the API', (tester) async {
    await pump(tester);

    await tester.enterText(find.byKey(const Key('amountField')), '10.005');
    await tester.tap(find.byKey(const Key('saveExpense')));
    await tester.pumpAndSettle();

    expect(find.text('Enter an amount greater than zero (up to 2 decimals)'), findsOneWidget);
    expect(actions.added, isEmpty);
  });

  testWidgets('edits an existing expense and keeps its values', (tester) async {
    final existing = Transaction(
      id: 't1',
      amount: 12.5,
      categoryId: 'fuel',
      date: DateTime(2026, 10, 20),
      merchant: 'Station',
      goalId: null,
    );
    await pump(tester, existing: existing);

    expect(find.text('Edit expense'), findsOneWidget);
    expect(find.text('12.5'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('amountField')), '15');
    await tester.tap(find.byKey(const Key('saveExpense')));
    await tester.pumpAndSettle();

    final (id, input) = actions.updated.single;
    expect(id, 't1');
    expect(input.amount, 15);
    expect(input.date, DateTime(2026, 10, 20));
  });

  testWidgets('deletes only after confirming', (tester) async {
    final existing = Transaction(
      id: 't1', amount: 10, categoryId: 'fuel', date: DateTime(2026, 10, 20), merchant: null, goalId: null);
    await pump(tester, existing: existing);

    await tester.tap(find.byKey(const Key('deleteExpense')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();
    expect(actions.deleted, isEmpty);

    await tester.tap(find.byKey(const Key('deleteExpense')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Delete'));
    await tester.pumpAndSettle();
    expect(actions.deleted, ['t1']);
  });

  testWidgets('a goal contribution cannot change category', (tester) async {
    final contribution = Transaction(
      id: 't2', amount: 500, categoryId: 'savings', date: DateTime(2026, 10, 20), merchant: null, goalId: 'g1');
    await pump(tester, existing: contribution);

    expect(find.text('Money saved toward a goal stays in Savings'), findsOneWidget);
    final dropdown = tester.widget<DropdownButton<String>>(find.byType(DropdownButton<String>));
    expect(dropdown.onChanged, isNull);
  });
}
