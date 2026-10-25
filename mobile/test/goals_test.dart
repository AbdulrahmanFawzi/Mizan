import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/goals/application/goal_actions.dart';
import 'package:mizan/features/goals/data/goal.dart';
import 'package:mizan/features/goals/data/goal_repository.dart';
import 'package:mizan/features/goals/presentation/goal_detail_screen.dart';
import 'package:mizan/features/goals/presentation/goal_form_screen.dart';
import 'package:mizan/features/goals/presentation/goals_screen.dart';

import 'helpers/test_app.dart';

class FakeGoalActions implements GoalActions {
  final created = <Map<String, Object>>[];
  final updated = <Map<String, Object>>[];
  final contributions = <(String, double)>[];
  Goal? afterContribution;

  @override
  Future<void> create({
    required String name,
    required double targetAmount,
    required double initialAmount,
    required DateTime targetDate,
  }) async =>
      created.add({'name': name, 'target': targetAmount, 'initial': initialAmount, 'date': targetDate});

  @override
  Future<void> update(String id, {required String name, required double targetAmount, required DateTime targetDate}) async =>
      updated.add({'id': id, 'name': name, 'target': targetAmount});

  @override
  Future<void> archive(String id) async {}

  @override
  Future<Goal> contribute(String id, {required double amount, required DateTime date}) async {
    contributions.add((id, amount));
    return afterContribution!;
  }
}

Goal _goal({GoalStatus status = GoalStatus.active, double current = 3500, double remaining = 6500}) => Goal(
      id: 'g1',
      name: 'Travel',
      targetAmount: 10000,
      initialAmount: 3500,
      targetDate: DateTime(2027, 12, 31),
      currentAmount: current,
      remaining: remaining,
      requiredPerPeriod: 542,
      remainingPeriods: 12,
      percentComplete: current / 100,
      status: status,
    );

void main() {
  late FakeGoalActions actions;

  setUp(() => actions = FakeGoalActions());

  test('Goal.fromJson reads the API response', () {
    final goal = Goal.fromJson({
      'id': 'g1',
      'name': 'Travel',
      'targetAmount': {'amount': 10000, 'currency': 'SAR'},
      'initialAmount': {'amount': 3500, 'currency': 'SAR'},
      'targetDate': '2027-12-31',
      'currentAmount': {'amount': 10000, 'currency': 'SAR'},
      'remaining': {'amount': 0, 'currency': 'SAR'},
      'requiredPerPeriod': {'amount': 0, 'currency': 'SAR'},
      'remainingPeriods': 15,
      'percentComplete': 100,
      'status': 'Completed',
    });

    expect(goal.status, GoalStatus.completed);
    expect(goal.canReceiveSavings, isFalse);
    expect(goal.targetDate, DateTime(2027, 12, 31));
  });

  group('Goals tab', () {
    testWidgets('lists goals with the monthly saving needed', (tester) async {
      await tester.pumpWidget(testApp(const GoalsScreen(), overrides: [goalsProvider.overrideWith((ref) async => [_goal()])]));
      await tester.pumpAndSettle();

      expect(find.text('Travel'), findsOneWidget);
      expect(find.text('Save 542 SAR per month'), findsOneWidget);
    });

    testWidgets('invites the user to create the first goal', (tester) async {
      await tester.pumpWidget(testApp(const GoalsScreen(), overrides: [goalsProvider.overrideWith((ref) async => <Goal>[])]));
      await tester.pumpAndSettle();

      expect(find.text('No goals yet'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'New goal'), findsOneWidget);
    });
  });

  group('Goal details', () {
    Future<void> pump(WidgetTester tester, Goal goal) async {
      await tester.pumpWidget(testApp(
        const GoalDetailScreen(goalId: 'g1'),
        overrides: [
          goalProvider('g1').overrideWith((ref) async => goal),
          goalActionsProvider.overrideWithValue(actions),
        ],
      ));
      await tester.pumpAndSettle();
    }

    testWidgets('an active goal shows what to save each month', (tester) async {
      await pump(tester, _goal());

      expect(find.text('35%'), findsOneWidget);
      expect(find.byKey(const Key('requiredPerMonth')), findsOneWidget);
      expect(find.text('12 months left'), findsOneWidget);
      expect(find.byKey(const Key('addSaving')), findsOneWidget);
    });

    testWidgets('a completed goal shows the badge and no "add saving"', (tester) async {
      await pump(tester, _goal(status: GoalStatus.completed, current: 10000, remaining: 0));

      expect(find.byKey(const Key('goalCompletedChip')), findsOneWidget);
      expect(find.byKey(const Key('addSaving')), findsNothing);
    });

    testWidgets('adding a saving that reaches the target celebrates', (tester) async {
      actions.afterContribution = _goal(status: GoalStatus.completed, current: 10000, remaining: 0);
      await pump(tester, _goal());

      await tester.tap(find.byKey(const Key('addSaving')));
      await tester.pumpAndSettle();
      expect(find.text('This amount counts as spending in your Savings category.'), findsOneWidget);

      await tester.enterText(find.byKey(const Key('contributionAmountField')), '6500');
      await tester.tap(find.byKey(const Key('saveContribution')));
      await tester.pumpAndSettle();

      expect(actions.contributions, [('g1', 6500.0)]);
      expect(find.text('🎉 You reached your goal!'), findsOneWidget);
    });
  });

  group('Goal form', () {
    testWidgets('creating a goal sends name, target, and what is already saved', (tester) async {
      await tester.pumpWidget(testApp(const GoalFormScreen(), overrides: [goalActionsProvider.overrideWithValue(actions)]));
      await tester.pumpAndSettle();

      await tester.enterText(find.byKey(const Key('goalNameField')), 'Car');
      await tester.enterText(find.byKey(const Key('goalTargetField')), '20000');
      await tester.enterText(find.byKey(const Key('goalInitialField')), '2500');
      await tester.tap(find.byKey(const Key('saveGoal')));
      await tester.pumpAndSettle();

      final created = actions.created.single;
      expect(created['name'], 'Car');
      expect(created['target'], 20000.0);
      expect(created['initial'], 2500.0);
      expect((created['date']! as DateTime).isAfter(DateTime.now()), isTrue);
    });

    testWidgets('editing does not ask for the initial amount again', (tester) async {
      await tester.pumpWidget(testApp(GoalFormScreen(existing: _goal()), overrides: [goalActionsProvider.overrideWithValue(actions)]));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('goalInitialField')), findsNothing);
      await tester.enterText(find.byKey(const Key('goalTargetField')), '12000');
      await tester.tap(find.byKey(const Key('saveGoal')));
      await tester.pumpAndSettle();

      expect(actions.updated.single, {'id': 'g1', 'name': 'Travel', 'target': 12000.0});
    });
  });
}
