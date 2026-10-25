import 'package:flutter/material.dart';

import '../data/category.dart';

typedef CategoryVisual = ({IconData icon, Color color});

// The API stores categories as names only, so the app picks an icon and color from the name
// (Arabic or English). Unknown names still get a stable color: the same name always maps
// to the same color, so a category never changes color between screens or launches.
CategoryVisual categoryVisual(String name, CategoryKind kind) {
  if (kind == CategoryKind.savings) return (icon: Icons.savings_rounded, color: const Color(0xFF0E9F6E));
  if (kind == CategoryKind.other) return (icon: Icons.category_rounded, color: const Color(0xFF64748B));

  final text = name.toLowerCase();
  for (final rule in _rules) {
    if (rule.keywords.any(text.contains)) return (icon: rule.icon, color: rule.color);
  }

  final color = _fallbackColors[_stableHash(text) % _fallbackColors.length];
  return (icon: Icons.label_rounded, color: color);
}

class _Rule {
  const _Rule(this.keywords, this.icon, this.color);

  final List<String> keywords;
  final IconData icon;
  final Color color;
}

const _rules = [
  _Rule(['مطعم', 'مطاعم', 'أكل', 'اكل', 'طعام', 'قهوة', 'كافي', 'food', 'restaurant', 'dining', 'coffee'],
      Icons.restaurant_rounded, Color(0xFFFF8A3D)),
  _Rule(['بقالة', 'سوبر', 'مقاضي', 'grocer', 'supermarket'], Icons.shopping_basket_rounded, Color(0xFF22C55E)),
  _Rule(['وقود', 'بنزين', 'سيارة', 'مواصلات', 'نقل', 'fuel', 'gas', 'car', 'transport', 'uber'],
      Icons.local_gas_station_rounded, Color(0xFF3B82F6)),
  _Rule(['تسوق', 'ملابس', 'shopping', 'clothes'], Icons.shopping_bag_rounded, Color(0xFFEC4899)),
  _Rule(['فاتورة', 'فواتير', 'كهرباء', 'ماء', 'جوال', 'انترنت', 'إنترنت', 'bill', 'utilit', 'phone', 'internet'],
      Icons.receipt_long_rounded, Color(0xFF8B5CF6)),
  _Rule(['سكن', 'إيجار', 'ايجار', 'بيت', 'rent', 'housing', 'home'], Icons.home_rounded, Color(0xFF14B8A6)),
  _Rule(['صحة', 'صيدلية', 'طبيب', 'مستشفى', 'health', 'pharmacy', 'doctor'], Icons.favorite_rounded, Color(0xFFEF4444)),
  _Rule(['ترفيه', 'سينما', 'ألعاب', 'العاب', 'entertain', 'movie', 'game'], Icons.movie_rounded, Color(0xFFF59E0B)),
  _Rule(['تعليم', 'دراسة', 'كتب', 'education', 'school', 'course', 'book'], Icons.school_rounded, Color(0xFF6366F1)),
  _Rule(['سفر', 'رحلة', 'طيران', 'travel', 'trip', 'flight'], Icons.flight_rounded, Color(0xFF0EA5E9)),
  _Rule(['صدقة', 'تبرع', 'زكاة', 'charity', 'donation'], Icons.volunteer_activism_rounded, Color(0xFF10B981)),
  _Rule(['اشتراك', 'اشتراكات', 'subscription', 'netflix'], Icons.autorenew_rounded, Color(0xFFA855F7)),
];

const _fallbackColors = [
  Color(0xFF6C8CFF),
  Color(0xFFFF8A3D),
  Color(0xFFEC4899),
  Color(0xFF14B8A6),
  Color(0xFF8B5CF6),
  Color(0xFFF59E0B),
];

// String.hashCode can change between runs; this one never does.
int _stableHash(String text) => text.codeUnits.fold(7, (hash, unit) => (hash * 31 + unit) & 0x7fffffff);
