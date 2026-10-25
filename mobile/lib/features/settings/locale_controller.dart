import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

// null = follow the phone's language. Not saved yet: resets when the app restarts.
class LocaleController extends Notifier<Locale?> {
  @override
  Locale? build() => null;

  void toggle(Locale current) =>
      state = current.languageCode == 'ar' ? const Locale('en') : const Locale('ar');
}

final localeProvider = NotifierProvider<LocaleController, Locale?>(LocaleController.new);
