import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/budget.dart';

// Income split: how much is planned across categories and how much is still free.
class BudgetSummaryCard extends StatelessWidget {
  const BudgetSummaryCard({super.key, required this.budget});

  final Budget budget;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final planned = budget.plannedIncome == 0 ? 0.0 : (budget.totalAllocated / budget.plannedIncome).clamp(0.0, 1.0);

    return Card(
      color: colors.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              l10n.periodRange(context.shortDate(budget.period.start), context.shortDate(budget.period.end)),
              style: theme.textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(child: _Figure(label: l10n.income, value: context.money(budget.plannedIncome))),
              Expanded(child: _Figure(label: l10n.planned, value: context.money(budget.totalAllocated))),
            ]),
            const SizedBox(height: 12),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: LinearProgressIndicator(
                value: planned,
                minHeight: 10,
                color: budget.isOverAllocated ? colors.error : colors.primary,
                backgroundColor: colors.surface,
              ),
            ),
            const SizedBox(height: 8),
            // BR-B5: planning more than the income is allowed, but the user should know.
            if (budget.isOverAllocated)
              Text(
                l10n.overAllocated(context.money(-budget.unallocated)),
                key: const Key('overAllocatedWarning'),
                style: theme.textTheme.bodyMedium?.copyWith(color: colors.error, fontWeight: FontWeight.bold),
              )
            else
              Text('${l10n.unallocated}: ${context.money(budget.unallocated)}', style: theme.textTheme.bodyMedium),
          ],
        ),
      ),
    );
  }
}

class _Figure extends StatelessWidget {
  const _Figure({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(label, style: theme.textTheme.bodySmall),
      Text(value, style: theme.textTheme.titleMedium),
    ]);
  }
}
