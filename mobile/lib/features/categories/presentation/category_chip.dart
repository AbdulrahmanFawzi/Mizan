import 'package:flutter/material.dart';

import '../../../core/theme/mizan_colors.dart';
import '../data/category.dart';
import 'category_visual.dart';

// Categories as tappable chips with their icon: one tap instead of opening a dropdown.
class CategoryChip extends StatelessWidget {
  const CategoryChip({super.key, required this.category, required this.label, required this.selected, this.onTap});

  final Category category;
  final String label;
  final bool selected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    final scheme = Theme.of(context).colorScheme;
    final visual = categoryVisual(category.name, category.kind);
    final disabled = onTap == null && !selected;

    return Opacity(
      opacity: disabled ? 0.4 : 1,
      child: Material(
        color: selected ? scheme.primary : scheme.surface,
        shape: StadiumBorder(side: BorderSide(color: selected ? scheme.primary : colors.border)),
        child: InkWell(
          customBorder: const StadiumBorder(),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsetsDirectional.fromSTEB(6, 6, 14, 6),
            child: Row(mainAxisSize: MainAxisSize.min, children: [
              Container(
                width: 28,
                height: 28,
                decoration: BoxDecoration(
                  color: selected ? scheme.onPrimary.withValues(alpha: 0.15) : visual.color.withValues(alpha: 0.14),
                  shape: BoxShape.circle,
                ),
                child: Icon(visual.icon, size: 16, color: selected ? scheme.onPrimary : visual.color),
              ),
              const SizedBox(width: 8),
              Text(
                label,
                style: TextStyle(fontWeight: FontWeight.w600, color: selected ? scheme.onPrimary : scheme.onSurface),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}
