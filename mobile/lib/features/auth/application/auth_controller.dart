import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/storage/token_storage.dart';
import '../../profile/data/profile_repository.dart';
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

  Future<void> resetPassword({required String email, required String code, required String newPassword}) async {
    await ref.read(authRepositoryProvider).resetPassword(email: email, code: code, newPassword: newPassword);
    state = const AsyncData(AuthStatus.authenticated);
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    state = const AsyncData(AuthStatus.unauthenticated);
  }

  // Lives here (not in a profile class) because it ends the session, like logout.
  // Tokens are cleared only after the server confirmed the delete.
  Future<void> deleteAccount() async {
    await ref.read(profileRepositoryProvider).deleteAccount();
    await ref.read(tokenStorageProvider).clear();
    state = const AsyncData(AuthStatus.unauthenticated);
  }

  // Called by the HTTP layer when the refresh token is no longer valid.
  void sessionExpired() => state = const AsyncData(AuthStatus.unauthenticated);
}

final authControllerProvider = AsyncNotifierProvider<AuthController, AuthStatus>(AuthController.new);
