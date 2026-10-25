import 'package:flutter/material.dart';

// Mizan's brand palette: deep "ink" for surfaces that matter, "mint" for the numbers and actions
// the user should notice. Material's ColorScheme has no slot for brand ideas like "the hero card"
// or "chart colors", so they live in this ThemeExtension and switch with light/dark mode.
@immutable
class MizanColors extends ThemeExtension<MizanColors> {
  const MizanColors({
    required this.ink,
    required this.inkSoft,
    required this.mint,
    required this.mintOnLight,
    required this.onInk,
    required this.onInkMuted,
    required this.textMuted,
    required this.fieldFill,
    required this.border,
    required this.warning,
    required this.danger,
    required this.chart,
  });

  static const light = MizanColors(
    ink: Color(0xFF0B1220),
    inkSoft: Color(0xFF16213A),
    mint: Color(0xFF2EE6A6),
    mintOnLight: Color(0xFF0E9F6E), // mint is too light for text on white; this one is readable
    onInk: Colors.white,
    onInkMuted: Color(0x99FFFFFF),
    textMuted: Color(0xFF667085),
    fieldFill: Color(0xFFEEF2F6),
    border: Color(0xFFE4E9F0),
    warning: Color(0xFFF59E0B),
    danger: Color(0xFFE5484D),
    chart: _chart,
  );

  static const dark = MizanColors(
    ink: Color(0xFF111A2E),
    inkSoft: Color(0xFF1A2540),
    mint: Color(0xFF2EE6A6),
    mintOnLight: Color(0xFF2EE6A6),
    onInk: Colors.white,
    onInkMuted: Color(0x99FFFFFF),
    textMuted: Color(0xFF98A2B3),
    fieldFill: Color(0xFF162036),
    border: Color(0xFF1F2A44),
    warning: Color(0xFFFBBF24),
    danger: Color(0xFFFF6369),
    chart: _chart,
  );

  static const _chart = [
    Color(0xFF2EE6A6),
    Color(0xFF6C8CFF),
    Color(0xFFFF8A3D),
    Color(0xFFEC4899),
    Color(0xFF8B5CF6),
    Color(0xFF14B8A6),
    Color(0xFFF59E0B),
    Color(0xFF64748B),
  ];

  final Color ink;
  final Color inkSoft;
  final Color mint;
  final Color mintOnLight;
  final Color onInk;
  final Color onInkMuted;
  final Color textMuted;
  final Color fieldFill;
  final Color border;
  final Color warning;
  final Color danger;
  final List<Color> chart;

  @override
  MizanColors copyWith({Color? ink, Color? mint}) => MizanColors(
        ink: ink ?? this.ink,
        inkSoft: inkSoft,
        mint: mint ?? this.mint,
        mintOnLight: mintOnLight,
        onInk: onInk,
        onInkMuted: onInkMuted,
        textMuted: textMuted,
        fieldFill: fieldFill,
        border: border,
        warning: warning,
        danger: danger,
        chart: chart,
      );

  @override
  MizanColors lerp(MizanColors? other, double t) {
    if (other == null) return this;
    Color mix(Color a, Color b) => Color.lerp(a, b, t)!;
    return MizanColors(
      ink: mix(ink, other.ink),
      inkSoft: mix(inkSoft, other.inkSoft),
      mint: mix(mint, other.mint),
      mintOnLight: mix(mintOnLight, other.mintOnLight),
      onInk: mix(onInk, other.onInk),
      onInkMuted: mix(onInkMuted, other.onInkMuted),
      textMuted: mix(textMuted, other.textMuted),
      fieldFill: mix(fieldFill, other.fieldFill),
      border: mix(border, other.border),
      warning: mix(warning, other.warning),
      danger: mix(danger, other.danger),
      chart: chart,
    );
  }
}

extension MizanColorsContext on BuildContext {
  MizanColors get mizan => Theme.of(this).extension<MizanColors>()!;
}
