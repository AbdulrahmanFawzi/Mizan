import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/goal.dart';

// Used on the dashboard and in the Goals tab.
class GoalCard extends StatelessWidget {
  const GoalCard({super.key, required this.goal, this.onTap});

  final Goal goal;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    final String hint;
    Color? hintColor;
    switch (goal.status) {
      case GoalStatus.completed:
        hint = l10n.statusCompleted;
        hintColor = Colors.green;
      case GoalStatus.overdue:
        hint = l10n.statusOverdue;
        hintColor = theme.colorScheme.error;
      case GoalStatus.active || GoalStatus.archived:
        hint = l10n.saveEachPeriod(context.money(goal.requiredPerPeriod));
    }

    return Card.outlined(
      child: InkWell(
        key: Key('goalCard-${goal.id}'),
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(children: [
                Expanded(child: Text(goal.name, style: theme.textTheme.titleMedium)),
                Text('${goal.percentComplete.toStringAsFixed(0)}%', style: theme.textTheme.titleMedium),
              ]),
              const SizedBox(height: 8),
              ClipRRect(
                borderRadius: BorderRadius.circular(6),
                child: LinearProgressIndicator(
                  value: goal.percentComplete / 100,
                  minHeight: 8,
                  color: goal.status == GoalStatus.completed ? Colors.green : null,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                '${context.money(goal.currentAmount)} / ${context.money(goal.targetAmount)}'
                ' · ${l10n.goalBy(context.mediumDate(goal.targetDate))}',
                style: theme.textTheme.bodySmall,
              ),
              Text(hint, style: theme.textTheme.bodyMedium?.copyWith(color: hintColor)),
            ],
          ),
        ),
      ),
    );
  }
}
