// Values passed at build time, e.g.:
// flutter run --dart-define=API_BASE_URL=https://api.example.com
abstract final class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    // The iOS simulator shares the Mac's network, so localhost reaches the local API.
    defaultValue: 'http://localhost:5009',
  );
}
