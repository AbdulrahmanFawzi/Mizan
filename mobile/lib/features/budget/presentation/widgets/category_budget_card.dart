import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/ui.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../categories/data/category.dart';
import '../../../categories/presentation/category_label.dart';
import '../../../categories/presentation/category_visual.dart';
import '../../data/budget.dart';

// One category: planned vs actual (UC-51).
class CategoryBudgetCard extends StatelessWidget {
  const CategoryBudgetCard({super.key, required this.item, required this.onTap});

  final CategoryBudget item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final visual = categoryVisual(item.name, item.kind);
    final isSavings = item.kind == CategoryKind.savings;

    // Going over the savings plan is good news (BR-A4), so it is never shown in red.
    final warn = item.isOverBudget && !isSavings;
    final barColor = warn ? colors.danger : visual.color;
    final progress = item.limit == 0 ? (item.spent > 0 ? 1.0 : 0.0) : item.spent / item.limit;

    final String status;
    if (item.limit == 0 && item.spent == 0) {
      status = l10n.noLimitSet;
    } else if (isSavings) {
      status = l10n.savedOfTarget(context.money(item.spent), context.money(item.limit));
    } else {
      status = l10n.spentOfLimit(context.money(item.spent), context.money(item.limit));
    }

    return Card(
      child: InkWell(
        key: Key('categoryCard-${item.categoryId}'),
        borderRadius: BorderRadius.circular(20),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Row(children: [
              IconTile(icon: visual.icon, color: visual.color),
              const SizedBox(width: 12),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(categoryLabel(item.category, l10n), style: theme.textTheme.titleMedium),
                  Text(status, style: TextStyle(color: colors.textMuted, fontSize: 13)),
                ]),
              ),
              if (item.percentUsed != null) _PercentPill(percent: item.percentUsed!, color: barColor),
            ]),
            const SizedBox(height: 14),
            ProgressBar(value: progress, color: barColor),
            if (!isSavings && item.limit > 0) ...[
              const SizedBox(height: 8),
              Text(
                warn ? l10n.overBy(context.money(-item.remaining)) : l10n.remainingAmount(context.money(item.remaining)),
                textAlign: TextAlign.end,
                style: TextStyle(
                  fontSize: 13,
                  color: warn ? colors.danger : colors.textMuted,
                  fontWeight: warn ? FontWeight.w700 : FontWeight.w500,
                ),
              ),
            ],
          ]),
        ),
      ),
    );
  }
}

class _PercentPill extends StatelessWidget {
  const _PercentPill({required this.percent, required this.color});

  final double percent;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
        child: Text(
          '${percent.toStringAsFixed(0)}%',
          style: TextStyle(color: color, fontWeight: FontWeight.w700, fontSize: 13),
        ),
      );
}
