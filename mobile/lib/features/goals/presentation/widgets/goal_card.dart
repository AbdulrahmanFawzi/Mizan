import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/money_text.dart';
import '../../../../core/widgets/ui.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/goal.dart';

// One goal in the Goals tab.
class GoalCard extends StatelessWidget {
  const GoalCard({super.key, required this.goal, this.onTap});

  final Goal goal;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final completed = goal.status == GoalStatus.completed;

    final (String hint, Color hintColor) = switch (goal.status) {
      GoalStatus.completed => (l10n.statusCompleted, colors.mintOnLight),
      GoalStatus.overdue => (l10n.statusOverdue, colors.danger),
      GoalStatus.active || GoalStatus.archived => (l10n.saveEachPeriod(context.money(goal.requiredPerPeriod)), colors.mintOnLight),
    };

    return Card(
      child: InkWell(
        key: Key('goalCard-${goal.id}'),
        onTap: onTap,
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Row(children: [
              IconTile(icon: completed ? Icons.emoji_events_rounded : Icons.flag_rounded, color: colors.mintOnLight),
              const SizedBox(width: 12),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(goal.name, style: theme.textTheme.titleMedium, maxLines: 1, overflow: TextOverflow.ellipsis),
                  Text(l10n.goalBy(context.mediumDate(goal.targetDate)), style: TextStyle(color: colors.textMuted, fontSize: 13)),
                ]),
              ),
              Text('${goal.percentComplete.toStringAsFixed(0)}%', style: theme.textTheme.titleLarge),
            ]),
            const SizedBox(height: 14),
            ProgressBar(value: goal.percentComplete / 100, color: goal.status == GoalStatus.overdue ? colors.warning : colors.mintOnLight),
            const SizedBox(height: 10),
            Row(children: [
              MoneyText(goal.currentAmount, style: theme.textTheme.titleSmall),
              Text(' / ${context.money(goal.targetAmount)}', style: TextStyle(color: colors.textMuted, fontSize: 13)),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  hint,
                  textAlign: TextAlign.end,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: hintColor, fontWeight: FontWeight.w600, fontSize: 13),
                ),
              ),
            ]),
          ]),
        ),
      ),
    );
  }
}
