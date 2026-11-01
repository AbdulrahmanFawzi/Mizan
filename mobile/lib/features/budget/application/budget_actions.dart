import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../categories/data/category.dart';
import '../../dashboard/data/dashboard_repository.dart';
import '../../transactions/application/transactions_controller.dart';
import '../data/budget_repository.dart';

// Category and limit changes. A limit changes the dashboard's budget bar and insights,
// and deleting a category moves its expenses, so everything that shows them reloads.
class BudgetActions {
  BudgetActions(this._ref);

  final Ref _ref;

  Future<void> createCategory({required String name, required double limit}) async {
    await _ref.read(categoryRepositoryProvider).create(name: name, monthlyLimit: limit);
    _refresh();
  }

  // Only sends what actually changed. System categories can't be renamed (BR-B3).
  Future<void> updateCategory(Category category, {required String name, required double limit, required double oldLimit}) async {
    if (!category.isSystem && name != category.name) {
      await _ref.read(categoryRepositoryProvider).rename(category.id, name);
    }
    if (limit != oldLimit) {
      await _ref.read(budgetRepositoryProvider).setLimit(category.id, limit);
    }
    _refresh();
  }

  // The guided setup's budget. Names that already exist are skipped, so trying again after an error is safe.
  Future<void> createStarterBudget({required Map<String, double> categories, required double savingsLimit}) async {
    final existing = await _ref.read(categoriesProvider.future);
    final names = {for (final c in existing) c.name.toLowerCase()};
    for (final MapEntry(key: name, value: limit) in categories.entries) {
      if (!names.contains(name.toLowerCase())) {
        await _ref.read(categoryRepositoryProvider).create(name: name, monthlyLimit: limit);
      }
    }
    final savings = existing.firstWhere((c) => c.kind == CategoryKind.savings);
    await _ref.read(budgetRepositoryProvider).setLimit(savings.id, savingsLimit);
    _refresh();
  }

  Future<void> deleteCategory(String categoryId) async {
    await _ref.read(categoryRepositoryProvider).delete(categoryId);
    _refresh();
  }

  void _refresh() {
    _ref.invalidate(budgetProvider);
    _ref.invalidate(categoriesProvider);
    _ref.invalidate(dashboardProvider);
    _ref.invalidate(transactionsControllerProvider);
  }
}

final budgetActionsProvider = Provider<BudgetActions>(BudgetActions.new);
