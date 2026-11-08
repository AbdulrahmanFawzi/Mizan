import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mizan/core/storage/preferences.dart';
import 'package:mizan/features/budget/application/budget_actions.dart';
import 'package:mizan/features/onboarding/category_suggestions.dart';
import 'package:mizan/features/onboarding/onboarding_controller.dart';
import 'package:mizan/features/onboarding/welcome_screen.dart';
import 'package:mizan/features/profile/data/profile_repository.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'budget_test.dart' show FakeBudgetActions;
import 'helpers/test_app.dart';

const _profile = Profile(
  name: 'A',
  email: 'a@example.com',
  currency: 'SAR',
  monthlyIncome: 10000,
  periodStartDay: 1,
  emailVerified: false,
);

void main() {
  test('suggested limits are a share of the income, rounded to 50', () {
    expect(const CategorySuggestion('', '', 0.12).limitFor(10000), 1200);
    expect(const CategorySuggestion('', '', 0.08).limitFor(7300), 600); // 584 → 600
  });

  testWidgets('the setup: skip the email, adjust the budget, finish', (tester) async {
    usePhoneScreen(tester);
    SharedPreferences.setMockInitialValues({'onboardingPending': true});
    final preferences = await SharedPreferences.getInstance();
    final actions = FakeBudgetActions();

    // A router, because the setup pushes the bank link screen.
    final router = GoRouter(routes: [
      GoRoute(path: '/', builder: (_, _) => const WelcomeScreen()),
      GoRoute(path: '/settings/banks/link', builder: (_, _) => const Text('link screen')),
    ]);
    await tester.pumpWidget(testApp(
      Router.withConfig(config: router),
      overrides: [
        sharedPreferencesProvider.overrideWithValue(preferences),
        profileProvider.overrideWith((ref) async => _profile),
        budgetActionsProvider.overrideWithValue(actions),
      ],
    ));
    await tester.pumpAndSettle();

    expect(find.text('Step 1 of 3'), findsOneWidget);
    await tester.tap(find.byKey(const Key('stepLater')));
    await tester.pumpAndSettle();

    // Step 2: income 10,000 → rent 2,500, groceries 1,200 ... and savings 1,000.
    expect(find.text('Step 2 of 3'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, '2500'), findsOneWidget);
    await tester.tap(find.byKey(const Key('suggestion-6'))); // no Health
    await tester.enterText(find.byKey(const Key('savingsLimit')), '1500');
    await tester.ensureVisible(find.byKey(const Key('createBudget')));
    await tester.tap(find.byKey(const Key('createBudget')));
    await tester.pumpAndSettle();

    expect(actions.starter!.length, 6);
    expect(actions.starter!['Rent & bills'], 2500);
    expect(actions.starter!.containsKey('Health'), isFalse);
    expect(actions.starterSavings, 1500);

    // Step 3: link the bank ends the setup and opens the link screen.
    expect(find.text('Step 3 of 3'), findsOneWidget);
    await tester.tap(find.byKey(const Key('setupLinkBank')));
    await tester.pumpAndSettle();

    expect(find.text('link screen'), findsOneWidget);
    expect(preferences.getBool('onboardingPending'), isNull);
    final container = ProviderScope.containerOf(tester.element(find.text('link screen')));
    expect(container.read(onboardingProvider), isFalse);
  });
}
