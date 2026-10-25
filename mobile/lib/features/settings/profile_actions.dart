import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../budget/data/budget_repository.dart';
import '../dashboard/data/dashboard_repository.dart';
import '../goals/data/goal_repository.dart';
import '../profile/data/profile_repository.dart';

class ProfileActions {
  ProfileActions(this._ref);

  final Ref _ref;

  // Income changes the current budget (BR-I2); the salary day changes how goal months are counted.
  Future<void> update({required String name, required double monthlyIncome, required int periodStartDay}) async {
    await _ref.read(profileRepositoryProvider).update(
          name: name,
          monthlyIncome: monthlyIncome,
          periodStartDay: periodStartDay,
        );
    _ref
      ..invalidate(profileProvider)
      ..invalidate(dashboardProvider)
      ..invalidate(budgetProvider)
      ..invalidate(goalsProvider)
      ..invalidate(goalProvider);
  }
}

final profileActionsProvider = Provider<ProfileActions>(ProfileActions.new);
