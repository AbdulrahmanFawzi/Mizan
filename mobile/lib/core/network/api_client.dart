import 'dart:ui';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/application/auth_controller.dart';
import '../../features/settings/locale_controller.dart';
import '../config/app_config.dart';
import '../storage/token_storage.dart';
import 'api_exception.dart';
import 'auth_interceptor.dart';

BaseOptions _options() => BaseOptions(
      baseUrl: AppConfig.apiBaseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 15),
      contentType: Headers.jsonContentType,
    );

// The language the app shows, so the API answers in it (insights, notifications, errors).
// Same choice as MaterialApp: the saved language, else the phone's, with Arabic as the fallback.
String apiLanguage(Locale? chosen, Locale phone) => (chosen ?? phone).languageCode == 'en' ? 'en' : 'ar';

final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(_options());
  dio.interceptors.add(InterceptorsWrapper(onRequest: (options, handler) {
    options.headers['Accept-Language'] = apiLanguage(ref.read(localeProvider), PlatformDispatcher.instance.locale);
    handler.next(options);
  }));
  dio.interceptors.add(AuthInterceptor(
    storage: ref.watch(tokenStorageProvider),
    plainDio: Dio(_options()),
    // Read lazily (only when it happens) to avoid a provider dependency cycle.
    onSessionExpired: () => ref.read(authControllerProvider.notifier).sessionExpired(),
  ));
  return dio;
});

// Runs an API call and turns Dio's errors into our own ApiException.
Future<T> apiCall<T>(Future<T> Function() call) async {
  try {
    return await call();
  } on DioException catch (error) {
    throw ApiException.fromDio(error);
  }
}
