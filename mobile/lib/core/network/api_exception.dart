import 'package:dio/dio.dart';

import '../../l10n/app_localizations.dart';

sealed class ApiException implements Exception {
  const ApiException();

  factory ApiException.fromDio(DioException error) {
    final response = error.response;
    if (response == null) return const NetworkException();

    // The API answers errors with RFC 7807 Problem Details: { title, detail, status }.
    final data = response.data;
    final detail = data is Map<String, dynamic> ? data['detail'] as String? : null;
    return ServerException(response.statusCode ?? 0, detail);
  }
}

class NetworkException extends ApiException {
  const NetworkException();
}

class ServerException extends ApiException {
  const ServerException(this.statusCode, this.detail);

  final int statusCode;
  final String? detail;
}

// One place that turns any error into a message the user can read.
String errorMessage(Object error, AppLocalizations l10n) => switch (error) {
      NetworkException() => l10n.networkError,
      ServerException(:final detail?) => detail,
      _ => l10n.unexpectedError,
    };
