import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AuthTokens {
  const AuthTokens({required this.accessToken, required this.refreshToken});

  factory AuthTokens.fromJson(Map<String, dynamic> json) => AuthTokens(
        accessToken: json['accessToken'] as String,
        refreshToken: json['refreshToken'] as String,
      );

  final String accessToken;
  final String refreshToken;
}

// Tokens live in the iOS Keychain / Android Keystore, never in plain preferences.
class TokenStorage {
  TokenStorage(this._storage);

  static const _accessKey = 'access_token';
  static const _refreshKey = 'refresh_token';

  final FlutterSecureStorage _storage;
  AuthTokens? _cached;

  Future<AuthTokens?> read() async {
    if (_cached != null) return _cached;

    final access = await _storage.read(key: _accessKey);
    final refresh = await _storage.read(key: _refreshKey);
    if (access == null || refresh == null) return null;

    return _cached = AuthTokens(accessToken: access, refreshToken: refresh);
  }

  Future<void> save(AuthTokens tokens) async {
    _cached = tokens;
    await _storage.write(key: _accessKey, value: tokens.accessToken);
    await _storage.write(key: _refreshKey, value: tokens.refreshToken);
  }

  Future<void> clear() async {
    _cached = null;
    await _storage.delete(key: _accessKey);
    await _storage.delete(key: _refreshKey);
  }
}

final tokenStorageProvider = Provider<TokenStorage>(
  (ref) => TokenStorage(const FlutterSecureStorage()),
);
