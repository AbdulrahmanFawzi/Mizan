import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../budget/data/budget_repository.dart';
import '../../dashboard/data/dashboard_repository.dart';
import '../../transactions/application/transactions_controller.dart';
import '../data/goal.dart';
import '../data/goal_repository.dart';

class GoalActions {
  GoalActions(this._ref);

  final Ref _ref;

  GoalRepository get _repository => _ref.read(goalRepositoryProvider);

  Future<void> create({
    required String name,
    required double targetAmount,
    required double initialAmount,
    required DateTime targetDate,
  }) async {
    await _repository.create(name: name, targetAmount: targetAmount, initialAmount: initialAmount, targetDate: targetDate);
    _refreshGoals();
  }

  Future<void> update(String id, {required String name, required double targetAmount, required DateTime targetDate}) async {
    await _repository.update(id, name: name, targetAmount: targetAmount, targetDate: targetDate);
    _refreshGoals();
  }

  Future<void> archive(String id) async {
    await _repository.archive(id);
    _refreshGoals();
  }

  // Returns the goal after the saving, so the screen can celebrate if it was just completed.
  // A contribution is also an expense in "Savings", so the budget and the expense list reload too.
  Future<Goal> contribute(String id, {required double amount, required DateTime date}) async {
    await _repository.contribute(id, amount: amount, date: date);
    _refreshGoals();
    _ref.invalidate(budgetProvider);
    _ref.invalidate(transactionsControllerProvider);
    return _repository.get(id);
  }

  void _refreshGoals() {
    _ref.invalidate(goalsProvider);
    _ref.invalidate(goalProvider); // every goal in the family
    _ref.invalidate(dashboardProvider); // shows goals, and a completion adds a notification
  }
}

final goalActionsProvider = Provider<GoalActions>(GoalActions.new);
