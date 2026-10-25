import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';
import '../../../core/storage/token_storage.dart';

class RegisterRequest {
  const RegisterRequest({
    required this.name,
    required this.email,
    required this.password,
    required this.monthlyIncome,
    required this.periodStartDay,
  });

  final String name;
  final String email;
  final String password;
  final double monthlyIncome;
  final int periodStartDay;

  Map<String, dynamic> toJson() => {
        'name': name,
        'email': email,
        'password': password,
        'monthlyIncome': monthlyIncome,
        'periodStartDay': periodStartDay,
      };
}

class AuthRepository {
  AuthRepository(this._dio, this._storage);

  final Dio _dio;
  final TokenStorage _storage;

  Future<void> login({required String email, required String password}) => _authenticate(
        '/api/auth/login',
        {'email': email, 'password': password},
      );

  Future<void> register(RegisterRequest request) => _authenticate('/api/auth/register', request.toJson());

  Future<void> logout() async {
    final tokens = await _storage.read();
    await _storage.clear();
    if (tokens == null) return;

    // Best effort: the user is logged out on this device even if the server can't be reached.
    try {
      await _dio.post<void>('/api/auth/logout', data: {'refreshToken': tokens.refreshToken});
    } on DioException {
      // Ignored on purpose.
    }
  }

  Future<void> _authenticate(String path, Map<String, dynamic> body) async {
    final response = await apiCall(() => _dio.post<Map<String, dynamic>>(path, data: body));
    await _storage.save(AuthTokens.fromJson(response.data!));
  }
}

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => AuthRepository(ref.watch(dioProvider), ref.watch(tokenStorageProvider)),
);
