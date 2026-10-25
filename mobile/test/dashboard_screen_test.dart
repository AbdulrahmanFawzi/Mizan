import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/budget/data/budget.dart';
import 'package:mizan/features/budget/data/budget_repository.dart';
import 'package:mizan/features/categories/data/category.dart';
import 'package:mizan/features/dashboard/data/dashboard.dart';
import 'package:mizan/features/dashboard/data/dashboard_repository.dart';
import 'package:mizan/features/dashboard/presentation/dashboard_screen.dart';
import 'package:mizan/features/goals/data/goal.dart';
import 'package:mizan/features/profile/data/profile_repository.dart';

import 'helpers/test_app.dart';

final _period = BudgetPeriod(start: DateTime(2026, 10, 1), end: DateTime(2026, 10, 31), lengthInDays: 31, daysElapsed: 25);

// Widget tests replace the API with fixed data, so they run in milliseconds without a server.
Dashboard _dashboard({double? safe = 1008, List<Goal> goals = const [], List<Insight> insights = const []}) => Dashboard(
      period: _period,
      monthlyIncome: 10000,
      totalSpent: 2940,
      available: 7060,
      safeToSpendToday: safe,
      daysLeft: 7,
      budgetUsagePercent: 77,
      topSpending: const [],
      goals: goals,
      insights: insights,
      unreadNotifications: 2,
    );

CategoryBudget _cat(String id, String name, CategoryKind kind, double limit, double spent) => CategoryBudget(
      categoryId: id,
      name: name,
      kind: kind,
      limit: limit,
      spent: spent,
      remaining: limit - spent,
      percentUsed: limit == 0 ? null : spent / limit * 100,
      isOverBudget: spent > limit,
    );

Budget _budget({bool empty = false}) => Budget(
      period: _period,
      plannedIncome: 10000,
      totalAllocated: 3800,
      unallocated: 6200,
      isOverAllocated: false,
      totalSpent: empty ? 0 : 2940,
      categories: [
        _cat('food', 'Food', CategoryKind.regular, 1000, empty ? 0 : 1270),
        _cat('bills', 'Bills', CategoryKind.regular, 1500, empty ? 0 : 950),
        _cat('savings', 'الادخار', CategoryKind.savings, 0, empty ? 0 : 720),
      ],
    );

Future<void> _pump(WidgetTester tester, Dashboard dashboard, {Budget? budget, Locale locale = const Locale('en')}) async {
  usePhoneScreen(tester);

  await tester.pumpWidget(testApp(
    const DashboardScreen(),
    locale: locale,
    overrides: [
      dashboardProvider.overrideWith((ref) async => dashboard),
      budgetProvider.overrideWith((ref) async => budget ?? _budget()),
      profileProvider.overrideWith((ref) async => const Profile(
            name: 'Abdulrahman',
            email: 'a@example.com',
            currency: 'SAR',
            monthlyIncome: 10000,
            periodStartDay: 1,
          )),
    ],
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('the hero shows what can be spent today, not just the balance', (tester) async {
    await _pump(tester, _dashboard());

    expect(find.text('Abdulrahman'), findsOneWidget);
    expect(find.text('You can spend today'), findsOneWidget);
    expect(find.text('1,008 SAR'), findsOneWidget);
    expect(find.text('7 days left'), findsOneWidget);
    expect(find.text('7,060 SAR'), findsOneWidget);
    expect(find.text('77%'), findsOneWidget);
    expect(find.text('2'), findsOneWidget); // unread notifications badge
  });

  testWidgets('an overspent period says so instead of showing zero', (tester) async {
    await _pump(tester, _dashboard(safe: 0));

    expect(find.text('Nothing left to spend this period'), findsOneWidget);
  });

  testWidgets('the donut lists spending by category, without savings', (tester) async {
    await _pump(tester, _dashboard());

    expect(find.text('Where your money went'), findsOneWidget);
    expect(find.text('Food'), findsOneWidget);
    expect(find.text('Bills'), findsOneWidget);
    expect(find.text('43%'), findsOneWidget); // 1,270 of 2,220
    expect(find.text('Savings'), findsNothing);
  });

  testWidgets('a new user sees empty states instead of blank sections', (tester) async {
    await _pump(tester, _dashboard(), budget: _budget(empty: true));

    expect(find.text('No spending yet this period'), findsOneWidget);
    expect(find.text('No goals yet'), findsOneWidget);
  });

  testWidgets('shows insights and goals', (tester) async {
    await _pump(
      tester,
      _dashboard(
        insights: const [Insight(type: InsightType.overBudget, categoryName: 'Food', message: 'Over budget on Food')],
        goals: [
          Goal(
            id: 'g1',
            name: 'Travel',
            targetAmount: 10000,
            initialAmount: 3500,
            targetDate: DateTime(2027, 12, 31),
            currentAmount: 3500,
            remaining: 6500,
            requiredPerPeriod: 542,
            remainingPeriods: 12,
            percentComplete: 35,
            status: GoalStatus.active,
          ),
        ],
      ),
    );

    expect(find.text('Over budget on Food'), findsOneWidget);
    // Goals are further down; ListView only builds what is on screen, so scroll like a user.
    await tester.scrollUntilVisible(find.text('Travel'), 300, scrollable: find.byType(Scrollable).first);
    expect(find.text('Travel'), findsOneWidget);
    expect(find.text('35%'), findsOneWidget);
    expect(find.text('Save 542 SAR per month'), findsOneWidget);
  });

  testWidgets('Arabic layout is right-to-left with Arabic labels and currency', (tester) async {
    await _pump(tester, _dashboard(), locale: const Locale('ar'));

    expect(find.text('تقدر تصرف اليوم'), findsOneWidget);
    expect(find.text('1,008 ر.س'), findsOneWidget);
    expect(Directionality.of(tester.element(find.byType(DashboardScreen))), TextDirection.rtl);
  });
}
