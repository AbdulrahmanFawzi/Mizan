import 'package:flutter/material.dart';

import 'mizan_colors.dart';

// One place that decides how every Material widget looks, so screens don't style things by hand.
abstract final class AppTheme {
  static const fontFamily = 'IBMPlexSansArabic';
  static const radius = 20.0;

  static ThemeData light() => _build(
        brightness: Brightness.light,
        brand: MizanColors.light,
        scheme: const ColorScheme.light(
          primary: Color(0xFF0B1220), // ink: main buttons
          onPrimary: Colors.white,
          secondary: Color(0xFF2EE6A6), // mint
          onSecondary: Color(0xFF0B1220),
          surface: Colors.white,
          onSurface: Color(0xFF0B1220),
          surfaceContainerHighest: Color(0xFFEEF2F6),
          outline: Color(0xFFE4E9F0),
          outlineVariant: Color(0xFFE4E9F0),
          error: Color(0xFFE5484D),
        ),
        background: const Color(0xFFF5F7FA),
      );

  static ThemeData dark() => _build(
        brightness: Brightness.dark,
        brand: MizanColors.dark,
        scheme: const ColorScheme.dark(
          primary: Color(0xFF2EE6A6), // on dark, the main buttons are mint
          onPrimary: Color(0xFF0B1220),
          secondary: Color(0xFF2EE6A6),
          onSecondary: Color(0xFF0B1220),
          surface: Color(0xFF0F1626),
          onSurface: Color(0xFFE8EDF5),
          surfaceContainerHighest: Color(0xFF162036),
          outline: Color(0xFF1F2A44),
          outlineVariant: Color(0xFF1F2A44),
          error: Color(0xFFFF6369),
        ),
        background: const Color(0xFF070B14),
      );

  static ThemeData _build({
    required Brightness brightness,
    required MizanColors brand,
    required ColorScheme scheme,
    required Color background,
  }) {
    final base = ThemeData(brightness: brightness, useMaterial3: true, fontFamily: fontFamily);
    final text = base.textTheme.apply(bodyColor: scheme.onSurface, displayColor: scheme.onSurface).copyWith(
          headlineMedium: base.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w700, letterSpacing: -0.5),
          headlineSmall: base.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
          titleLarge: base.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
          titleMedium: base.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
          titleSmall: base.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
          labelLarge: base.textTheme.labelLarge?.copyWith(fontWeight: FontWeight.w600),
        );
    final rounded = RoundedRectangleBorder(borderRadius: BorderRadius.circular(radius));
    final field = OutlineInputBorder(borderRadius: BorderRadius.circular(16), borderSide: BorderSide.none);

    return base.copyWith(
      colorScheme: scheme,
      scaffoldBackgroundColor: background,
      textTheme: text,
      extensions: [brand],
      splashFactory: InkSparkle.splashFactory,
      appBarTheme: AppBarTheme(
        backgroundColor: background,
        foregroundColor: scheme.onSurface,
        elevation: 0,
        scrolledUnderElevation: 0,
        surfaceTintColor: Colors.transparent,
        centerTitle: false,
        titleTextStyle: text.titleLarge?.copyWith(fontSize: 22),
      ),
      cardTheme: CardThemeData(
        color: scheme.surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: rounded.copyWith(side: BorderSide(color: brand.border)),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: brand.fieldFill,
        contentPadding: const EdgeInsets.symmetric(horizontal: 18, vertical: 18),
        border: field,
        enabledBorder: field,
        focusedBorder: field.copyWith(borderSide: BorderSide(color: scheme.primary, width: 1.5)),
        errorBorder: field.copyWith(borderSide: BorderSide(color: scheme.error)),
        focusedErrorBorder: field.copyWith(borderSide: BorderSide(color: scheme.error, width: 1.5)),
        labelStyle: TextStyle(color: brand.textMuted),
        helperStyle: TextStyle(color: brand.textMuted),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size.fromHeight(56),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          textStyle: text.titleMedium,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size.fromHeight(56),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          side: BorderSide(color: brand.border),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: brightness == Brightness.light ? scheme.primary : scheme.secondary,
          textStyle: text.labelLarge,
        ),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(
        color: brand.mintOnLight,
        linearTrackColor: brand.fieldFill,
        circularTrackColor: brand.fieldFill,
        linearMinHeight: 8,
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        showDragHandle: true,
        shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(28))),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: brand.ink,
        contentTextStyle: text.bodyMedium?.copyWith(color: brand.onInk),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      listTileTheme: ListTileThemeData(iconColor: brand.textMuted, shape: rounded),
      dividerTheme: DividerThemeData(color: brand.border, space: 1),
      chipTheme: ChipThemeData(
        side: BorderSide.none,
        backgroundColor: brand.fieldFill,
        shape: const StadiumBorder(),
      ),
      segmentedButtonTheme: SegmentedButtonThemeData(
        // Light: ink with mint text. Dark: ink is close to the background, so the selected item is mint.
        style: SegmentedButton.styleFrom(
          foregroundColor: scheme.onSurface,
          selectedBackgroundColor: brightness == Brightness.light ? brand.ink : brand.mint,
          selectedForegroundColor: brightness == Brightness.light ? brand.mint : brand.ink,
          side: BorderSide(color: brand.border),
        ),
      ),
      datePickerTheme: DatePickerThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      ),
    );
  }
}
