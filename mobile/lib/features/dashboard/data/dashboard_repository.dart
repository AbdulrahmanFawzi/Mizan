import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'dashboard.dart';

class DashboardRepository {
  DashboardRepository(this._dio);

  final Dio _dio;

  Future<Dashboard> get() async {
    final response = await apiCall(() => _dio.get<Map<String, dynamic>>('/api/dashboard'));
    return Dashboard.fromJson(response.data!);
  }
}

final dashboardRepositoryProvider = Provider<DashboardRepository>(
  (ref) => DashboardRepository(ref.watch(dioProvider)),
);

// autoDispose: data is fetched fresh each time the dashboard is opened, and dropped after logout.
final dashboardProvider = FutureProvider.autoDispose<Dashboard>(
  (ref) => ref.watch(dashboardRepositoryProvider).get(),
);
