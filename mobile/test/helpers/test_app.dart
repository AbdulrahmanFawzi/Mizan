import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/core/theme/app_theme.dart';
import 'package:mizan/l10n/app_localizations.dart';

// Wraps a screen with what it needs to run in a widget test: the real theme, Riverpod overrides,
// and translations.
Widget testApp(Widget home, {List<Override> overrides = const [], Locale locale = const Locale('en')}) =>
    ProviderScope(
      overrides: overrides,
      child: MaterialApp(
        theme: AppTheme.light(),
        locale: locale,
        supportedLocales: AppLocalizations.supportedLocales,
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        home: home,
      ),
    );

// Widget tests default to an 800x600 window. Screens are designed for phones, so tests that
// look at a whole screen use an iPhone-sized one (430x932 points).
void usePhoneScreen(WidgetTester tester) {
  tester.view.physicalSize = const Size(1290, 2796);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
}
