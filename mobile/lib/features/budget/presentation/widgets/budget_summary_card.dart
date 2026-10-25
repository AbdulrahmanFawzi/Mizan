import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/money_text.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../categories/presentation/category_visual.dart';
import '../../data/budget.dart';

// How the income is split: one bar, one colored piece per category, the empty part is what's left to plan.
class BudgetSummaryCard extends StatelessWidget {
  const BudgetSummaryCard({super.key, required this.budget});

  final Budget budget;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        gradient: LinearGradient(
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
          colors: [colors.ink, colors.inkSoft],
        ),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(l10n.planned, style: TextStyle(color: colors.onInkMuted)),
        const SizedBox(height: 4),
        Row(crossAxisAlignment: CrossAxisAlignment.baseline, textBaseline: TextBaseline.alphabetic, children: [
          Flexible(
            child: FittedBox(
              fit: BoxFit.scaleDown,
              child: AnimatedMoney(
                budget.totalAllocated,
                style: theme.textTheme.headlineMedium?.copyWith(color: colors.onInk, fontWeight: FontWeight.w700),
                currencyColor: colors.onInkMuted,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Text(l10n.ofAmount(context.money(budget.plannedIncome)), style: TextStyle(color: colors.onInkMuted)),
        ]),
        const SizedBox(height: 18),
        _AllocationBar(budget: budget),
        const SizedBox(height: 14),
        // BR-B5: planning more than the income is allowed, but the user should know.
        if (budget.isOverAllocated)
          Row(children: [
            Icon(Icons.error_outline_rounded, color: colors.danger, size: 18),
            const SizedBox(width: 6),
            Expanded(
              child: Text(
                l10n.overAllocated(context.money(-budget.unallocated)),
                key: const Key('overAllocatedWarning'),
                style: TextStyle(color: colors.danger, fontWeight: FontWeight.w600),
              ),
            ),
          ])
        else
          Text('${l10n.unallocated}: ${context.money(budget.unallocated)}', style: TextStyle(color: colors.onInk)),
        const SizedBox(height: 14),
        Divider(color: colors.onInk.withValues(alpha: 0.1)),
        const SizedBox(height: 10),
        Row(children: [
          Expanded(child: Text(l10n.spentThisPeriod, style: TextStyle(color: colors.onInkMuted))),
          MoneyText(
            budget.totalSpent,
            style: TextStyle(color: colors.mint, fontWeight: FontWeight.w700, fontSize: 17),
            currencyColor: colors.onInkMuted,
          ),
        ]),
      ]),
    );
  }
}

class _AllocationBar extends StatelessWidget {
  const _AllocationBar({required this.budget});

  final Budget budget;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    // Flex needs whole numbers, so amounts become halalas.
    final pieces = [
      for (final item in budget.categories)
        if (item.limit > 0) (flex: (item.limit * 100).round(), color: categoryVisual(item.name, item.kind).color),
      if (budget.unallocated > 0) (flex: (budget.unallocated * 100).round(), color: colors.onInk.withValues(alpha: 0.1)),
    ];

    if (pieces.isEmpty) {
      return Container(
        height: 12,
        decoration: BoxDecoration(color: colors.onInk.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(6)),
      );
    }

    return ClipRRect(
      borderRadius: BorderRadius.circular(6),
      child: SizedBox(
        height: 12,
        child: Row(children: [
          for (final (index, piece) in pieces.indexed)
            Expanded(
              flex: piece.flex,
              child: Container(
                // A thin gap between pieces so neighbors with similar colors stay readable.
                margin: EdgeInsetsDirectional.only(end: index == pieces.length - 1 ? 0 : 2),
                color: piece.color,
              ),
            ),
        ]),
      ),
    );
  }
}
