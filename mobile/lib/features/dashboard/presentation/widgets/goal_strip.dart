import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/format/formatters.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/money_text.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../goals/data/goal.dart';

// Goals as a horizontal row of compact cards: more visible without pushing everything down.
class GoalStrip extends StatelessWidget {
  const GoalStrip({super.key, required this.goals});

  final List<Goal> goals;

  @override
  Widget build(BuildContext context) => SizedBox(
        height: 168,
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          clipBehavior: Clip.none,
          itemCount: goals.length,
          separatorBuilder: (_, _) => const SizedBox(width: 12),
          itemBuilder: (context, index) => _GoalTile(goal: goals[index]),
        ),
      );
}

class _GoalTile extends StatelessWidget {
  const _GoalTile({required this.goal});

  final Goal goal;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);

    return SizedBox(
      width: 210,
      child: Card(
        child: InkWell(
          borderRadius: BorderRadius.circular(20),
          onTap: () => context.push(Routes.goal(goal.id)),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(children: [
                SizedBox.square(
                  dimension: 44,
                  child: Stack(fit: StackFit.expand, children: [
                    CircularProgressIndicator(
                      value: goal.percentComplete / 100,
                      strokeWidth: 5,
                      strokeCap: StrokeCap.round,
                      color: colors.mintOnLight,
                    ),
                    Center(
                      child: Text('${goal.percentComplete.toStringAsFixed(0)}%',
                          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700)),
                    ),
                  ]),
                ),
                const Spacer(),
                Icon(Icons.flag_rounded, color: colors.textMuted, size: 18),
              ]),
              const Spacer(),
              Text(goal.name, maxLines: 1, overflow: TextOverflow.ellipsis, style: theme.textTheme.titleMedium),
              const SizedBox(height: 2),
              MoneyText(goal.currentAmount, style: theme.textTheme.bodyMedium?.copyWith(color: colors.textMuted)),
              const SizedBox(height: 4),
              Text(
                goal.status == GoalStatus.overdue
                    ? l10n.statusOverdue
                    : l10n.saveEachPeriod(context.money(goal.requiredPerPeriod)),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 12,
                  color: goal.status == GoalStatus.overdue ? colors.danger : colors.mintOnLight,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}
