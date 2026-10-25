import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/storage/preferences.dart';

// null = follow the phone's language. The choice is saved, so it survives a restart.
class LocaleController extends Notifier<Locale?> {
  static const _key = 'locale';

  @override
  Locale? build() {
    final saved = ref.read(sharedPreferencesProvider).getString(_key);
    return saved == null ? null : Locale(saved);
  }

  Future<void> set(Locale? locale) async {
    state = locale;
    final preferences = ref.read(sharedPreferencesProvider);
    locale == null ? await preferences.remove(_key) : await preferences.setString(_key, locale.languageCode);
  }

  Future<void> toggle(Locale current) => set(current.languageCode == 'ar' ? const Locale('en') : const Locale('ar'));
}

final localeProvider = NotifierProvider<LocaleController, Locale?>(LocaleController.new);
