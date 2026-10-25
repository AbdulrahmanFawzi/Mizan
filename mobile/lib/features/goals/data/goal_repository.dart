import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/format/json.dart';
import '../../../core/network/api_client.dart';
import 'goal.dart';

class GoalRepository {
  GoalRepository(this._dio);

  final Dio _dio;

  Future<List<Goal>> list() async {
    final response = await apiCall(() => _dio.get<List<dynamic>>('/api/goals'));
    return response.data!.cast<Map<String, dynamic>>().map(Goal.fromJson).toList();
  }

  Future<Goal> get(String id) async {
    final response = await apiCall(() => _dio.get<Map<String, dynamic>>('/api/goals/$id'));
    return Goal.fromJson(response.data!);
  }

  Future<Goal> create({
    required String name,
    required double targetAmount,
    required double initialAmount,
    required DateTime targetDate,
  }) async {
    final response = await apiCall(() => _dio.post<Map<String, dynamic>>('/api/goals', data: {
          'name': name,
          'targetAmount': targetAmount,
          'initialAmount': initialAmount,
          'targetDate': apiDate(targetDate),
        }));
    return Goal.fromJson(response.data!);
  }

  Future<Goal> update(String id, {required String name, required double targetAmount, required DateTime targetDate}) async {
    final response = await apiCall(() => _dio.put<Map<String, dynamic>>('/api/goals/$id', data: {
          'name': name,
          'targetAmount': targetAmount,
          'targetDate': apiDate(targetDate),
        }));
    return Goal.fromJson(response.data!);
  }

  Future<void> archive(String id) => apiCall(() => _dio.post<void>('/api/goals/$id/archive'));

  // UC-42 / D4: the API records this as a Savings expense linked to the goal.
  Future<void> contribute(String id, {required double amount, required DateTime date}) => apiCall(
        () => _dio.post<void>('/api/goals/$id/contributions', data: {'amount': amount, 'date': apiDate(date)}),
      );
}

final goalRepositoryProvider = Provider<GoalRepository>((ref) => GoalRepository(ref.watch(dioProvider)));

final goalsProvider = FutureProvider.autoDispose<List<Goal>>((ref) => ref.watch(goalRepositoryProvider).list());

// One provider per goal id ("family"), used by the details screen.
final goalProvider = FutureProvider.autoDispose.family<Goal, String>(
  (ref, id) => ref.watch(goalRepositoryProvider).get(id),
);
