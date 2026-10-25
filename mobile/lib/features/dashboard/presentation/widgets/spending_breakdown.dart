import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/money_text.dart';
import '../../../../core/widgets/ui.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../budget/data/budget.dart';
import '../../../categories/data/category.dart';
import '../../../categories/presentation/category_label.dart';
import '../../../categories/presentation/category_visual.dart';

// A donut of this period's spending by category, with a legend underneath.
// Savings are left out: putting money aside isn't "where the money went".
class SpendingBreakdown extends StatelessWidget {
  const SpendingBreakdown({super.key, required this.budget});

  static const maxSlices = 5;

  final Budget budget;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);

    final items = budget.categories.where((c) => c.kind != CategoryKind.savings && c.spent > 0).toList()
      ..sort((a, b) => b.spent.compareTo(a.spent));
    final total = items.fold<double>(0, (sum, c) => sum + c.spent);

    if (items.isEmpty) {
      return Card(child: EmptyState(icon: Icons.donut_large_rounded, message: l10n.noSpendingYet));
    }

    // Small categories are grouped so the donut stays readable.
    final shown = items.take(maxSlices).toList();
    final rest = items.skip(maxSlices).fold<double>(0, (sum, c) => sum + c.spent);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(children: [
          SizedBox(
            height: 190,
            child: Stack(alignment: Alignment.center, children: [
              PieChart(
                PieChartData(
                  sectionsSpace: 3,
                  centerSpaceRadius: 62,
                  startDegreeOffset: -90,
                  sections: [
                    for (final c in shown)
                      PieChartSectionData(
                        value: c.spent,
                        color: categoryVisual(c.name, c.kind).color,
                        radius: 22,
                        showTitle: false,
                      ),
                    if (rest > 0) PieChartSectionData(value: rest, color: colors.border, radius: 22, showTitle: false),
                  ],
                ),
                duration: const Duration(milliseconds: 600),
              ),
              Column(mainAxisSize: MainAxisSize.min, children: [
                Text(l10n.spent, style: TextStyle(color: colors.textMuted, fontSize: 12)),
                MoneyText(total, decimals: 0, style: theme.textTheme.titleLarge),
              ]),
            ]),
          ),
          const SizedBox(height: 16),
          for (final c in shown) _LegendRow(item: c, share: c.spent / total),
        ]),
      ),
    );
  }
}

class _LegendRow extends StatelessWidget {
  const _LegendRow({required this.item, required this.share});

  final CategoryBudget item;
  final double share;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final visual = categoryVisual(item.name, item.kind);
    final theme = Theme.of(context);

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(children: [
        IconTile(icon: visual.icon, color: visual.color, size: 38),
        const SizedBox(width: 12),
        Expanded(child: Text(categoryLabel(item.category, l10n), style: theme.textTheme.titleSmall)),
        Text('${(share * 100).toStringAsFixed(0)}%', style: TextStyle(color: context.mizan.textMuted, fontSize: 12)),
        const SizedBox(width: 12),
        MoneyText(item.spent, style: theme.textTheme.titleSmall),
      ]),
    );
  }
}
