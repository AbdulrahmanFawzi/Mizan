import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

// Loaded once in main() before the app starts, then handed to Riverpod with an override.
// That way every provider can read preferences synchronously, with no loading state.
// For non-secret settings only (like the language); tokens stay in secure storage.
final sharedPreferencesProvider = Provider<SharedPreferences>(
  (ref) => throw UnimplementedError('Override sharedPreferencesProvider in main()'),
);
