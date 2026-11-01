import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/core/storage/preferences.dart';
import 'package:mizan/core/storage/token_storage.dart';
import 'package:mizan/features/auth/application/auth_controller.dart';
import 'package:mizan/features/profile/data/profile_repository.dart';
import 'package:mizan/features/settings/edit_profile_screen.dart';
import 'package:mizan/features/settings/locale_controller.dart';
import 'package:mizan/features/settings/theme_mode_controller.dart';
import 'package:mizan/features/settings/profile_actions.dart';
import 'package:mizan/features/settings/settings_screen.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'helpers/test_app.dart';

const _profile = Profile(
  name: 'Abdulrahman',
  email: 'a@example.com',
  currency: 'SAR',
  monthlyIncome: 10000,
  periodStartDay: 27,
);

// In-memory stand-ins for the Keychain and the API.
class FakeTokenStorage extends TokenStorage {
  FakeTokenStorage() : super(const FlutterSecureStorage());

  AuthTokens? tokens = const AuthTokens(accessToken: 'a', refreshToken: 'r');

  @override
  Future<AuthTokens?> read() async => tokens;

  @override
  Future<void> clear() async => tokens = null;
}

class FakeProfileRepository implements ProfileRepository {
  bool deleted = false;

  @override
  Future<Profile> getMe() async => _profile;

  @override
  Future<Profile> update({required String name, required double monthlyIncome, required int periodStartDay}) async =>
      _profile;

  @override
  Future<void> deleteAccount() async => deleted = true;

  @override
  Future<(Uint8List, String)> exportData() async => (Uint8List(0), 'x.json');

  @override
  Future<void> sendVerificationCode() async {}

  @override
  Future<void> verifyEmail(String code) async {}
}

class FakeProfileActions implements ProfileActions {
  final updates = <(String, double, int)>[];

  @override
  Future<void> update({required String name, required double monthlyIncome, required int periodStartDay}) async =>
      updates.add((name, monthlyIncome, periodStartDay));

  int exports = 0;

  @override
  Future<void> exportData() async => exports++;
}

void main() {
  late SharedPreferences preferences;
  late FakeTokenStorage tokens;
  late FakeProfileRepository repository;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    preferences = await SharedPreferences.getInstance();
    tokens = FakeTokenStorage();
    repository = FakeProfileRepository();
  });

  Future<ProviderContainer> pumpSettings(WidgetTester tester, {FakeProfileActions? actions}) async {
    usePhoneScreen(tester);

    await tester.pumpWidget(testApp(const SettingsScreen(), overrides: [
      sharedPreferencesProvider.overrideWithValue(preferences),
      tokenStorageProvider.overrideWithValue(tokens),
      profileRepositoryProvider.overrideWithValue(repository),
      if (actions != null) profileActionsProvider.overrideWithValue(actions),
    ]));
    await tester.pumpAndSettle();
    return ProviderScope.containerOf(tester.element(find.byType(SettingsScreen)));
  }

  testWidgets('shows the profile', (tester) async {
    await pumpSettings(tester);

    expect(find.text('Abdulrahman'), findsOneWidget);
    expect(find.text('10,000 SAR'), findsOneWidget);
    expect(find.text('27'), findsOneWidget);
  });

  testWidgets('the language choice is saved for the next launch', (tester) async {
    final container = await pumpSettings(tester);

    await tester.tap(find.text('العربية'));
    await tester.pumpAndSettle();

    expect(container.read(localeProvider), const Locale('ar'));
    expect(preferences.getString('locale'), 'ar');

    await tester.tap(find.text('Phone'));
    await tester.pumpAndSettle();
    expect(preferences.getString('locale'), isNull);
  });

  testWidgets('a saved language is restored on start', (tester) async {
    SharedPreferences.setMockInitialValues({'locale': 'en'});
    preferences = await SharedPreferences.getInstance();

    final container = await pumpSettings(tester);

    expect(container.read(localeProvider), const Locale('en'));
  });

  testWidgets('the theme choice is saved for the next launch', (tester) async {
    final container = await pumpSettings(tester);
    expect(container.read(themeModeProvider), ThemeMode.system);

    await tester.tap(find.text('Dark'));
    await tester.pumpAndSettle();
    expect(container.read(themeModeProvider), ThemeMode.dark);
    expect(preferences.getString('themeMode'), 'dark');

    await tester.tap(find.text('Auto'));
    await tester.pumpAndSettle();
    expect(preferences.getString('themeMode'), isNull);
  });

  testWidgets('a saved theme is restored on start', (tester) async {
    SharedPreferences.setMockInitialValues({'themeMode': 'light'});
    preferences = await SharedPreferences.getInstance();

    final container = await pumpSettings(tester);

    expect(container.read(themeModeProvider), ThemeMode.light);
  });

  testWidgets('export my data starts the export', (tester) async {
    final actions = FakeProfileActions();
    await pumpSettings(tester, actions: actions);

    await tester.ensureVisible(find.byKey(const Key('exportData')));
    await tester.tap(find.byKey(const Key('exportData')));
    await tester.pump();

    expect(actions.exports, 1);
  });

  testWidgets('a verified email has no "confirm your email" row', (tester) async {
    await pumpSettings(tester);

    expect(find.byKey(const Key('openVerifyEmail')), findsNothing);
  });

  testWidgets('deleting the account needs confirmation, then ends the session', (tester) async {
    final container = await pumpSettings(tester);
    await container.read(authControllerProvider.future);

    await tester.tap(find.byKey(const Key('deleteAccount')));
    await tester.pumpAndSettle();
    expect(find.textContaining("can't be undone"), findsOneWidget);

    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();
    expect(repository.deleted, isFalse);

    await tester.tap(find.byKey(const Key('deleteAccount')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('confirmDeleteAccount')));
    await tester.pumpAndSettle();

    expect(repository.deleted, isTrue);
    expect(tokens.tokens, isNull);
    expect(container.read(authControllerProvider).value, AuthStatus.unauthenticated);
  });

  testWidgets('editing the profile sends the new values', (tester) async {
    final actions = FakeProfileActions();
    usePhoneScreen(tester);
    await tester.pumpWidget(testApp(const EditProfileScreen(), overrides: [
      profileRepositoryProvider.overrideWithValue(repository),
      profileActionsProvider.overrideWithValue(actions),
    ]));
    await tester.pumpAndSettle();

    expect(find.text('A new salary day starts with your next budget period.'), findsOneWidget);
    await tester.enterText(find.byKey(const Key('profileIncomeField')), '12000');
    await tester.tap(find.byKey(const Key('saveProfile')));
    await tester.pumpAndSettle();

    expect(actions.updates, [('Abdulrahman', 12000.0, 27)]);
  });
}
