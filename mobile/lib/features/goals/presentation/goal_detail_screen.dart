import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../l10n/app_localizations.dart';
import '../application/goal_actions.dart';
import '../data/goal.dart';
import '../data/goal_repository.dart';
import 'widgets/contribution_sheet.dart';

// UC-43 in detail, plus the actions on one goal: add saving (UC-42), edit (UC-41), archive (UC-44).
class GoalDetailScreen extends ConsumerWidget {
  const GoalDetailScreen({super.key, required this.goalId});

  final String goalId;

  Future<void> _archive(BuildContext context, WidgetRef ref, Goal goal) async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.archiveGoalQuestion(goal.name)),
        content: Text(l10n.archiveGoalExplain),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: Text(l10n.cancel)),
          TextButton(
            key: const Key('confirmArchiveGoal'),
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.archiveGoal),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    try {
      await ref.read(goalActionsProvider).archive(goal.id);
      messenger.showSnackBar(SnackBar(content: Text(l10n.goalArchived)));
      if (context.mounted) context.pop();
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(error, l10n))));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final goal = ref.watch(goalProvider(goalId));
    final loaded = goal.value;

    return Scaffold(
      appBar: AppBar(
        title: Text(loaded?.name ?? ''),
        actions: [
          if (loaded != null && loaded.status != GoalStatus.archived) ...[
            IconButton(
              key: const Key('editGoal'),
              tooltip: l10n.editGoal,
              onPressed: () => context.push(Routes.editGoal(loaded.id), extra: loaded),
              icon: const Icon(Icons.edit_outlined),
            ),
            IconButton(
              key: const Key('archiveGoal'),
              tooltip: l10n.archiveGoal,
              onPressed: () => _archive(context, ref, loaded),
              icon: const Icon(Icons.archive_outlined),
            ),
          ],
        ],
      ),
      body: switch (goal) {
        AsyncData(:final value) => _GoalDetails(goal: value),
        AsyncError(:final error) => Center(child: Text(errorMessage(error, l10n))),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _GoalDetails extends StatelessWidget {
  const _GoalDetails({required this.goal});

  final Goal goal;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final completed = goal.status == GoalStatus.completed;
    final overdue = goal.status == GoalStatus.overdue;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Center(
          child: SizedBox.square(
            dimension: 180,
            child: Stack(fit: StackFit.expand, children: [
              CircularProgressIndicator(
                value: goal.percentComplete / 100,
                strokeWidth: 14,
                color: completed ? Colors.green : null,
                backgroundColor: theme.colorScheme.surfaceContainerHighest,
              ),
              Center(
                child: Text(
                  '${goal.percentComplete.toStringAsFixed(0)}%',
                  style: theme.textTheme.displaySmall?.copyWith(fontWeight: FontWeight.bold),
                ),
              ),
            ]),
          ),
        ),
        const SizedBox(height: 16),
        if (completed)
          Center(
            child: Chip(
              key: const Key('goalCompletedChip'),
              avatar: const Icon(Icons.check_circle, color: Colors.green),
              label: Text(l10n.statusCompleted),
            ),
          ),
        if (overdue)
          Center(
            child: Chip(
              avatar: Icon(Icons.schedule, color: theme.colorScheme.error),
              label: Text('${l10n.statusOverdue} · ${l10n.extendDateHint}'),
            ),
          ),
        const SizedBox(height: 8),
        Card.outlined(
          child: Column(children: [
            _Row(l10n.saved, '${context.money(goal.currentAmount)} / ${context.money(goal.targetAmount)}'),
            _Row(l10n.remainingLabel, context.money(goal.remaining)),
            _Row(l10n.targetDate, context.longDate(goal.targetDate)),
            if (!completed && !overdue) ...[
              _Row(l10n.perMonth, context.money(goal.requiredPerPeriod), key: const Key('requiredPerMonth')),
              _Row('', l10n.monthsLeft(goal.remainingPeriods)),
            ],
          ]),
        ),
        const SizedBox(height: 24),
        if (goal.canReceiveSavings)
          FilledButton.icon(
            key: const Key('addSaving'),
            onPressed: () => showContributionSheet(context, goal),
            icon: const Icon(Icons.savings_outlined),
            label: Text(l10n.addSaving),
          ),
      ],
    );
  }
}

class _Row extends StatelessWidget {
  const _Row(this.label, this.value, {super.key});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => ListTile(
        dense: true,
        title: Text(label),
        trailing: Text(value, style: Theme.of(context).textTheme.titleSmall),
      );
}
