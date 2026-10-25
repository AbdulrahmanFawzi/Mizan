import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import 'transaction.dart';

class TransactionRepository {
  TransactionRepository(this._dio);

  static const pageSize = 30;

  final Dio _dio;

  Future<TransactionPage> list({required int page}) async {
    final response = await apiCall(() => _dio.get<Map<String, dynamic>>(
          '/api/transactions',
          queryParameters: {'page': page, 'pageSize': pageSize},
        ));
    return TransactionPage.fromJson(response.data!);
  }

  Future<Transaction> get(String id) async {
    final response = await apiCall(() => _dio.get<Map<String, dynamic>>('/api/transactions/$id'));
    return Transaction.fromJson(response.data!);
  }

  Future<Transaction> create(ExpenseInput input) async {
    final response = await apiCall(() => _dio.post<Map<String, dynamic>>('/api/transactions', data: input.toJson()));
    return Transaction.fromJson(response.data!);
  }

  Future<Transaction> update(String id, ExpenseInput input) async {
    final response =
        await apiCall(() => _dio.put<Map<String, dynamic>>('/api/transactions/$id', data: input.toJson()));
    return Transaction.fromJson(response.data!);
  }

  Future<void> delete(String id) => apiCall(() => _dio.delete<void>('/api/transactions/$id'));
}

final transactionRepositoryProvider = Provider<TransactionRepository>(
  (ref) => TransactionRepository(ref.watch(dioProvider)),
);
