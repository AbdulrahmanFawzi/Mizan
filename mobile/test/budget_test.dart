import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/budget/application/budget_actions.dart';
import 'package:mizan/features/budget/data/budget.dart';
import 'package:mizan/features/budget/data/budget_repository.dart';
import 'package:mizan/features/budget/presentation/budget_screen.dart';
import 'package:mizan/features/categories/data/category.dart';
import 'package:mizan/features/dashboard/data/dashboard.dart' show BudgetPeriod;

import 'helpers/test_app.dart';

class FakeBudgetActions implements BudgetActions {
  final created = <(String, double)>[];
  final updated = <(String, String, double)>[];
  final deleted = <String>[];

  @override
  Future<void> createCategory({required String name, required double limit}) async => created.add((name, limit));

  @override
  Future<void> updateCategory(Category category, {required String name, required double limit, required double oldLimit}) async =>
      updated.add((category.id, name, limit));

  @override
  Future<void> deleteCategory(String categoryId) async => deleted.add(categoryId);
}

CategoryBudget _item(String id, String name, CategoryKind kind, double limit, double spent) => CategoryBudget(
      categoryId: id,
      name: name,
      kind: kind,
      limit: limit,
      spent: spent,
      remaining: limit - spent,
      percentUsed: limit == 0 ? null : spent / limit * 100,
      isOverBudget: spent > limit,
    );

Budget _budget({double allocated = 3500, List<CategoryBudget>? categories}) => Budget(
      period: BudgetPeriod(start: DateTime(2026, 10, 1), end: DateTime(2026, 10, 31), lengthInDays: 31, daysElapsed: 25),
      plannedIncome: 10000,
      totalAllocated: allocated,
      unallocated: 10000 - allocated,
      isOverAllocated: allocated > 10000,
      totalSpent: 0,
      categories: categories ??
          [
            _item('food', 'Food', CategoryKind.regular, 1000, 1270),
            _item('fuel', 'Fuel', CategoryKind.regular, 500, 450),
            _item('other', 'أخرى', CategoryKind.other, 0, 0),
            _item('savings', 'الادخار', CategoryKind.savings, 2000, 2500),
          ],
    );

void main() {
  late FakeBudgetActions actions;

  setUp(() => actions = FakeBudgetActions());

  Future<void> pump(WidgetTester tester, Budget budget) async {
    await tester.pumpWidget(testApp(
      const BudgetScreen(),
      overrides: [
        budgetProvider.overrideWith((ref) async => budget),
        budgetActionsProvider.overrideWithValue(actions),
      ],
    ));
    await tester.pumpAndSettle();
  }

  test('Budget.fromJson reads the API response', () {
    final budget = Budget.fromJson({
      'id': 'b1',
      'period': {'start': '2026-10-01', 'end': '2026-10-31', 'lengthInDays': 31, 'daysElapsed': 25},
      'plannedIncome': {'amount': 10000, 'currency': 'SAR'},
      'totalAllocated': {'amount': 11000, 'currency': 'SAR'},
      'unallocated': {'amount': -1000, 'currency': 'SAR'},
      'isOverAllocated': true,
      'totalSpent': {'amount': 460, 'currency': 'SAR'},
      'categories': [
        {
          'categoryId': 'c1',
          'name': 'أخرى',
          'kind': 'Other',
          'limit': {'amount': 0, 'currency': 'SAR'},
          'spent': {'amount': 0, 'currency': 'SAR'},
          'remaining': {'amount': 0, 'currency': 'SAR'},
          'percentUsed': null,
          'isOverBudget': false,
        },
      ],
    });

    expect(budget.isOverAllocated, isTrue);
    expect(budget.unallocated, -1000);
    expect(budget.categories.single.kind, CategoryKind.other);
    expect(budget.categories.single.percentUsed, isNull);
  });

  group('Budget screen', () {
    testWidgets('shows planned vs actual for each category', (tester) async {
      await pump(tester, _budget());

      expect(find.text('Not planned yet: 6,500 SAR'), findsOneWidget);
      expect(find.text('Over by 270 SAR'), findsOneWidget); // Food: 1,270 of 1,000
      expect(find.text('50 SAR left'), findsOneWidget); // Fuel: 450 of 500
      expect(find.text('No limit set'), findsOneWidget); // Other: 0 of 0
      expect(find.text('Other'), findsOneWidget); // translated system name
    });

    testWidgets('saving more than planned is not shown as a problem', (tester) async {
      await pump(tester, _budget());

      expect(find.text('Saved 2,500 SAR of 2,000 SAR'), findsOneWidget);
      expect(find.textContaining('Over by 500'), findsNothing);
    });

    testWidgets('warns when more than the income is planned', (tester) async {
      await pump(tester, _budget(allocated: 11000));

      expect(find.byKey(const Key('overAllocatedWarning')), findsOneWidget);
      expect(find.text('You planned 1,000 SAR more than your income'), findsOneWidget);
    });
  });

  group('Category editor', () {
    testWidgets('creates a category with a limit', (tester) async {
      await pump(tester, _budget());

      await tester.tap(find.byKey(const Key('addCategory')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const Key('categoryNameField')), 'Shopping');
      await tester.enterText(find.byKey(const Key('categoryLimitField')), '800');
      await tester.tap(find.byKey(const Key('saveCategory')));
      await tester.pumpAndSettle();

      expect(actions.created, [('Shopping', 800.0)]);
    });

    testWidgets('a zero limit is allowed, a negative one is not', (tester) async {
      await pump(tester, _budget());
      await tester.tap(find.byKey(const Key('addCategory')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const Key('categoryNameField')), 'Gifts');

      await tester.enterText(find.byKey(const Key('categoryLimitField')), '-5');
      await tester.tap(find.byKey(const Key('saveCategory')));
      await tester.pumpAndSettle();
      expect(actions.created, isEmpty);

      await tester.enterText(find.byKey(const Key('categoryLimitField')), '0');
      await tester.tap(find.byKey(const Key('saveCategory')));
      await tester.pumpAndSettle();
      expect(actions.created, [('Gifts', 0.0)]);
    });

    testWidgets('editing a category changes its limit', (tester) async {
      await pump(tester, _budget());

      await tester.tap(find.byKey(const Key('categoryCard-fuel')));
      await tester.pumpAndSettle();
      await tester.enterText(find.byKey(const Key('categoryLimitField')), '600');
      await tester.tap(find.byKey(const Key('saveCategory')));
      await tester.pumpAndSettle();

      expect(actions.updated, [('fuel', 'Fuel', 600.0)]);
    });

    testWidgets('a built-in category can only change its limit', (tester) async {
      await pump(tester, _budget());

      await tester.tap(find.byKey(const Key('categoryCard-other')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('categoryNameField')), findsNothing);
      expect(find.byKey(const Key('deleteCategory')), findsNothing);
      expect(find.text('A built-in category: you can change its limit only.'), findsOneWidget);
    });

    testWidgets('deleting asks first and explains where the expenses go', (tester) async {
      await pump(tester, _budget());

      await tester.tap(find.byKey(const Key('categoryCard-fuel')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('deleteCategory')));
      await tester.pumpAndSettle();

      expect(find.text('Its expenses will move to "Other".'), findsOneWidget);
      await tester.tap(find.byKey(const Key('confirmDeleteCategory')));
      await tester.pumpAndSettle();
      expect(actions.deleted, ['fuel']);
    });
  });
}
