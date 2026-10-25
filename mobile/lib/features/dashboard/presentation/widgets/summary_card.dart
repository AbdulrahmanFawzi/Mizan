import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/dashboard.dart';

// The top card: the period, how much is left, and how much of the budget is used.
class SummaryCard extends StatelessWidget {
  const SummaryCard({super.key, required this.dashboard, this.greeting});

  final Dashboard dashboard;
  final String? greeting;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final period = dashboard.period;
    final usage = dashboard.budgetUsagePercent;
    final overBudget = usage != null && usage > 100;

    return Card(
      color: colors.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (greeting != null) Text(greeting!, style: theme.textTheme.titleMedium),
            Text(
              '${l10n.periodRange(context.shortDate(period.start), context.shortDate(period.end))}'
              ' · ${l10n.daysLeft(period.daysLeft)}',
              style: theme.textTheme.bodySmall,
            ),
            const SizedBox(height: 16),
            Text(l10n.available, style: theme.textTheme.labelLarge),
            Text(
              context.money(dashboard.available),
              key: const Key('availableAmount'),
              style: theme.textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.bold,
                color: dashboard.available < 0 ? colors.error : colors.onPrimaryContainer,
              ),
            ),
            const SizedBox(height: 16),
            Row(children: [
              Expanded(child: _Figure(label: l10n.income, value: context.money(dashboard.monthlyIncome))),
              Expanded(child: _Figure(key: const Key('totalSpent'), label: l10n.spent, value: context.money(dashboard.totalSpent))),
            ]),
            const SizedBox(height: 16),
            if (usage == null)
              Text(l10n.noLimitsYet, style: theme.textTheme.bodySmall)
            else ...[
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: LinearProgressIndicator(
                  value: (usage / 100).clamp(0, 1),
                  minHeight: 10,
                  color: overBudget ? colors.error : colors.primary,
                  backgroundColor: colors.surface,
                ),
              ),
              const SizedBox(height: 6),
              Text(l10n.budgetUsed(usage.toStringAsFixed(0)), style: theme.textTheme.bodySmall),
            ],
          ],
        ),
      ),
    );
  }
}

class _Figure extends StatelessWidget {
  const _Figure({super.key, required this.label, required this.value});

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
