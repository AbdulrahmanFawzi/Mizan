import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/storage/token_storage.dart';
import '../data/auth_repository.dart';

enum AuthStatus { authenticated, unauthenticated }

// The single source of truth for "is the user logged in?". The router listens to it.
class AuthController extends AsyncNotifier<AuthStatus> {
  @override
  Future<AuthStatus> build() async {
    final tokens = await ref.read(tokenStorageProvider).read();
    return tokens == null ? AuthStatus.unauthenticated : AuthStatus.authenticated;
  }

  // Errors are thrown to the calling screen, which shows them next to the form.
  Future<void> login({required String email, required String password}) async {
    await ref.read(authRepositoryProvider).login(email: email, password: password);
    state = const AsyncData(AuthStatus.authenticated);
  }

  Future<void> register(RegisterRequest request) async {
    await ref.read(authRepositoryProvider).register(request);
    state = const AsyncData(AuthStatus.authenticated);
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    state = const AsyncData(AuthStatus.unauthenticated);
  }

  // Called by the HTTP layer when the refresh token is no longer valid.
  void sessionExpired() => state = const AsyncData(AuthStatus.unauthenticated);
}

final authControllerProvider = AsyncNotifierProvider<AuthController, AuthStatus>(AuthController.new);
