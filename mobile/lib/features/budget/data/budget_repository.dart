import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'budget.dart';

class BudgetRepository {
  BudgetRepository(this._dio);

  final Dio _dio;

  Future<Budget> current() async {
    final response = await apiCall(() => _dio.get<Map<String, dynamic>>('/api/budgets/current'));
    return Budget.fromJson(response.data!);
  }

  Future<void> setLimit(String categoryId, double limit) => apiCall(
        () => _dio.put<void>('/api/budgets/current/categories/$categoryId/limit', data: {'limit': limit}),
      );
}

final budgetRepositoryProvider = Provider<BudgetRepository>(
  (ref) => BudgetRepository(ref.watch(dioProvider)),
);

final budgetProvider = FutureProvider.autoDispose<Budget>(
  (ref) => ref.watch(budgetRepositoryProvider).current(),
);
