import 'package:dio/dio.dart';

import '../storage/token_storage.dart';

// Adds the access token to every request. When the API answers 401 (token expired),
// it uses the refresh token once to get new tokens, then repeats the original request.
// QueuedInterceptor handles one failure at a time, so parallel 401s don't refresh twice.
class AuthInterceptor extends QueuedInterceptor {
  // Private named parameters: callers still write `storage:`, `plainDio:`, `onSessionExpired:`.
  AuthInterceptor({
    required this._storage,
    required this._plainDio,
    required this._onSessionExpired,
  });

  static const _retriedKey = 'auth_retried';

  final TokenStorage _storage;
  final Dio _plainDio; // No interceptors: used for refresh and retry.
  final void Function() _onSessionExpired;

  @override
  Future<void> onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final tokens = await _storage.read();
    if (tokens != null) {
      options.headers['Authorization'] = 'Bearer ${tokens.accessToken}';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final request = err.requestOptions;
    final isAuthCall = request.path.startsWith('/api/auth/');

    if (err.response?.statusCode != 401 || isAuthCall || request.extra[_retriedKey] == true) {
      return handler.next(err);
    }

    final tokens = await _storage.read();
    if (tokens == null) return handler.next(err);

    try {
      // Another request may already have refreshed while this one waited in the queue.
      final sentWith = request.headers['Authorization'];
      var accessToken = tokens.accessToken;

      if (sentWith == 'Bearer $accessToken') {
        final response = await _plainDio.post<Map<String, dynamic>>(
          '/api/auth/refresh',
          data: {'refreshToken': tokens.refreshToken},
        );
        final refreshed = AuthTokens.fromJson(response.data!);
        await _storage.save(refreshed);
        accessToken = refreshed.accessToken;
      }

      request.headers['Authorization'] = 'Bearer $accessToken';
      request.extra[_retriedKey] = true;
      handler.resolve(await _plainDio.fetch(request));
    } on DioException catch (refreshError) {
      if (refreshError.response?.statusCode == 401) {
        await _storage.clear();
        _onSessionExpired();
      }
      handler.next(err);
    }
  }
}
