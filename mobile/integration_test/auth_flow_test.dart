import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mizan/features/notifications/presentation/notifications_screen.dart';
import 'package:mizan/features/transactions/presentation/widgets/transaction_tile.dart';
import 'package:mizan/main.dart' as app;

// Runs the real app on a simulator against the real local API.
// Start the API first, then: flutter test integration_test -d <simulator-id>
// Finds widgets by Key, so it works whatever language the simulator is in.
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  String textOf(WidgetTester tester, Key key) => tester
      .widgetList<Text>(find.descendant(of: find.byKey(key), matching: find.byType(Text)))
      .map((t) => t.data ?? t.textSpan?.toPlainText())
      .join(' ');

  // pumpAndSettle only waits for animations, not for real async work like reading secure storage
  // on a cold start. This keeps pumping until the widget is actually on screen.
  Future<void> pumpUntilFound(WidgetTester tester, Finder finder) async {
    final deadline = DateTime.now().add(const Duration(seconds: 15));
    while (finder.evaluate().isEmpty) {
      if (DateTime.now().isAfter(deadline)) throw TestFailure('Timed out waiting for $finder');
      await tester.pump(const Duration(milliseconds: 100));
    }
  }

  testWidgets('the whole MVP 0 journey, from sign-up to account deletion', (tester) async {
    await const FlutterSecureStorage().deleteAll();
    await (await SharedPreferences.getInstance()).clear(); // language, theme and setup state from earlier runs
    final email = 'it-${DateTime.now().millisecondsSinceEpoch}@example.com';

    app.main();
    await pumpUntilFound(tester, find.byKey(const Key('goToRegister')));

    // Register
    await tester.tap(find.byKey(const Key('goToRegister')));
    await tester.pumpAndSettle();
    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Integration Test');
    await tester.enterText(fields.at(1), email);
    await tester.enterText(fields.at(2), 'P@ssw0rd123');
    await tester.enterText(fields.at(3), '12500');
    await tester.tap(find.byType(FilledButton));
    await tester.pumpAndSettle(const Duration(seconds: 1));

    // A new account starts with the guided setup; this journey skips it.
    await pumpUntilFound(tester, find.byKey(const Key('skipSetup')));
    await tester.tap(find.byKey(const Key('skipSetup')));
    await tester.pumpAndSettle(const Duration(seconds: 1));

    // Dashboard from GET /api/dashboard, name from GET /api/me
    expect(find.byKey(const Key('availableAmount')), findsOneWidget);
    expect(find.textContaining('Integration Test'), findsOneWidget);
    expect(textOf(tester, const Key('totalSpent')), contains('0'));

    // Plan a category in the Budget tab: Fuel, 500 SAR a month
    await tester.tap(find.byKey(const Key('budgetTab')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.tap(find.byKey(const Key('addCategory')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('categoryNameField')), 'Fuel');
    await tester.enterText(find.byKey(const Key('categoryLimitField')), '500');
    await tester.tap(find.byKey(const Key('saveCategory')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.text('Fuel'), findsOneWidget);

    // Add an expense (the form picks the first category the user created: Fuel)
    await tester.tap(find.byKey(const Key('addExpense')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('amountField')), '450');
    await tester.enterText(find.byKey(const Key('merchantField')), 'XYZ Fuel');
    await tester.tap(find.byKey(const Key('saveExpense')));
    await tester.pumpAndSettle(const Duration(seconds: 1));

    // The Budget tab reloaded: Fuel is at 90% of its limit
    expect(find.text('90%'), findsOneWidget);

    // So did the dashboard
    await tester.tap(find.byKey(const Key('homeTab')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(textOf(tester, const Key('totalSpent')), contains('450'));

    // Crossing 80% and 90% at once created ONE alert, for the highest threshold (BR-A3)
    await tester.tap(find.byKey(const Key('notificationsBell')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byType(NotificationTile), findsOneWidget);
    expect(find.textContaining('90%'), findsOneWidget);

    // Tapping it marks it read and opens the budget, where the user can act on it
    await tester.tap(find.byType(NotificationTile));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byKey(const Key('addCategory')), findsOneWidget);

    // The expenses tab lists it
    await tester.tap(find.byKey(const Key('expensesTab')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byType(TransactionTile), findsOneWidget);
    expect(find.text('XYZ Fuel'), findsOneWidget);

    // Open it and delete it
    await tester.tap(find.byType(TransactionTile));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('deleteExpense')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('confirmDelete')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byType(TransactionTile), findsNothing);

    // Create a goal and save toward it until it is reached
    await tester.tap(find.byKey(const Key('goalsTab')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.tap(find.byKey(const Key('addGoal')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('goalNameField')), 'Trip');
    await tester.enterText(find.byKey(const Key('goalTargetField')), '1000');
    await tester.tap(find.byKey(const Key('saveGoal')));
    await tester.pumpAndSettle(const Duration(seconds: 1));

    await tester.tap(find.text('Trip'));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byKey(const Key('requiredPerMonth')), findsOneWidget);

    await tester.tap(find.byKey(const Key('addSaving')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('contributionAmountField')), '1000');
    await tester.tap(find.byKey(const Key('saveContribution')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byKey(const Key('goalCompletedChip')), findsOneWidget);
    expect(find.byKey(const Key('addSaving')), findsNothing);
    await tester.pageBack();
    await tester.pumpAndSettle();

    // Settings: change the income, then delete the account (UC-06)
    await tester.tap(find.byKey(const Key('homeTab')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('openSettings')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.tap(find.byKey(const Key('editProfile')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    await tester.enterText(find.byKey(const Key('profileIncomeField')), '15000');
    await tester.tap(find.byKey(const Key('saveProfile')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.textContaining('15,000'), findsOneWidget);

    // The Bank accounts section (MVP 1) pushes this further down the screen.
    // The "profile saved" message floats over the bottom of the screen; a person would wait for it.
    tester.state<ScaffoldMessengerState>(find.byType(ScaffoldMessenger).first).removeCurrentSnackBar();
    await tester.ensureVisible(find.byKey(const Key('deleteAccount')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('deleteAccount')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('confirmDeleteAccount')));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byKey(const Key('goToRegister')), findsOneWidget);

    // The account is really gone: logging in with it fails
    final loginFields = find.byType(TextFormField);
    await tester.enterText(loginFields.at(0), email);
    await tester.enterText(loginFields.at(1), 'P@ssw0rd123');
    await tester.tap(find.byType(FilledButton));
    await tester.pumpAndSettle(const Duration(seconds: 1));
    expect(find.byKey(const Key('goToRegister')), findsOneWidget);
    expect(find.byKey(const Key('availableAmount')), findsNothing);
  });
}
