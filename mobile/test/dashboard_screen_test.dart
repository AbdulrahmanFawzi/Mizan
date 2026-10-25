import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/categories/data/category.dart';
import 'package:mizan/features/dashboard/data/dashboard.dart';
import 'package:mizan/features/dashboard/data/dashboard_repository.dart';
import 'package:mizan/features/dashboard/presentation/dashboard_screen.dart';
import 'package:mizan/features/goals/data/goal.dart';
import 'package:mizan/features/profile/data/profile_repository.dart';
import 'package:mizan/l10n/app_localizations.dart';

// Widget tests replace the API with fixed data, so they run in milliseconds without a server.
Dashboard _dashboard({
  double available = 9540,
  double? usage = 92,
  List<TopSpending> topSpending = const [TopSpending(categoryId: 'c1', name: 'Fuel', spent: 460)],
  List<Goal> goals = const [],
  List<Insight> insights = const [],
}) =>
    Dashboard(
      period: BudgetPeriod(start: DateTime(2026, 10, 1), end: DateTime(2026, 10, 31), lengthInDays: 31, daysElapsed: 22),
      monthlyIncome: 10000,
      totalSpent: 460,
      available: available,
      budgetUsagePercent: usage,
      topSpending: topSpending,
      goals: goals,
      insights: insights,
      unreadNotifications: 2,
    );

Future<void> _pump(WidgetTester tester, Dashboard dashboard, {Locale locale = const Locale('en')}) async {
  await tester.pumpWidget(ProviderScope(
    overrides: [
      dashboardProvider.overrideWith((ref) async => dashboard),
      categoriesProvider.overrideWith((ref) async => const <Category>[]),
      profileProvider.overrideWith((ref) async => const Profile(
            name: 'Abdulrahman',
            email: 'a@example.com',
            currency: 'SAR',
            monthlyIncome: 10000,
            periodStartDay: 1,
          )),
    ],
    child: MaterialApp(
      locale: locale,
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: const DashboardScreen(),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('shows the summary, top spending, and unread badge', (tester) async {
    await _pump(tester, _dashboard());

    expect(find.text('Welcome, Abdulrahman'), findsOneWidget);
    expect(find.text('9,540 SAR'), findsOneWidget);
    expect(find.text('92% of your budget used'), findsOneWidget);
    expect(find.textContaining('9 days left'), findsOneWidget);
    expect(find.text('Fuel'), findsOneWidget);
    expect(find.text('2'), findsOneWidget); // notifications badge
    expect(find.text("You're on track. No warnings this period."), findsOneWidget);
  });

  testWidgets('new user sees empty states instead of blank sections', (tester) async {
    await _pump(tester, _dashboard(usage: null, topSpending: const []));

    expect(find.text('Set limits for your categories to track your budget'), findsOneWidget);
    expect(find.text('No spending yet this period'), findsOneWidget);
    expect(find.text('No goals yet'), findsOneWidget);
  });

  testWidgets('shows insights and goal progress', (tester) async {
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
    expect(find.text('Travel'), findsOneWidget);
    expect(find.text('35%'), findsOneWidget);
    expect(find.text('Save 542 SAR per month'), findsOneWidget);
  });

  testWidgets('Arabic layout is right-to-left with Arabic labels', (tester) async {
    await _pump(tester, _dashboard(), locale: const Locale('ar'));

    expect(find.text('أهلًا، Abdulrahman'), findsOneWidget);
    expect(find.text('أعلى المصروفات'), findsOneWidget);
    expect(Directionality.of(tester.element(find.byType(DashboardScreen))), TextDirection.rtl);
  });
}
