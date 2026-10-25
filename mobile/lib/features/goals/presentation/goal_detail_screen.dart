import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/money_text.dart';
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
              icon: const Icon(Icons.edit_rounded),
            ),
            IconButton(
              key: const Key('archiveGoal'),
              tooltip: l10n.archiveGoal,
              onPressed: () => _archive(context, ref, loaded),
              icon: const Icon(Icons.archive_rounded),
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
    final colors = context.mizan;
    final completed = goal.status == GoalStatus.completed;
    final overdue = goal.status == GoalStatus.overdue;

    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
      children: [
        _ProgressHero(goal: goal),
        const SizedBox(height: 16),
        Row(children: [
          Expanded(
            child: _StatTile(
              icon: Icons.hourglass_bottom_rounded,
              label: l10n.remainingLabel,
              value: MoneyText(goal.remaining, style: Theme.of(context).textTheme.titleMedium),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: _StatTile(
              icon: Icons.event_rounded,
              label: l10n.targetDate,
              value: Text(context.mediumDate(goal.targetDate), style: Theme.of(context).textTheme.titleMedium),
            ),
          ),
        ]),
        if (!completed && !overdue) ...[
          const SizedBox(height: 12),
          _StatTile(
            key: const Key('requiredPerMonth'),
            icon: Icons.calendar_month_rounded,
            label: l10n.perMonth,
            value: MoneyText(goal.requiredPerPeriod, style: Theme.of(context).textTheme.titleLarge?.copyWith(color: colors.mintOnLight)),
            trailing: Text(l10n.monthsLeft(goal.remainingPeriods), style: TextStyle(color: colors.textMuted)),
          ),
        ],
        const SizedBox(height: 24),
        if (goal.canReceiveSavings)
          FilledButton.icon(
            key: const Key('addSaving'),
            onPressed: () => showContributionSheet(context, goal),
            icon: const Icon(Icons.savings_rounded),
            label: Text(l10n.addSaving),
          ),
      ],
    );
  }
}

// The ring is the goal: how far along it is, at a glance.
class _ProgressHero extends StatelessWidget {
  const _ProgressHero({required this.goal});

  final Goal goal;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final completed = goal.status == GoalStatus.completed;
    final overdue = goal.status == GoalStatus.overdue;

    return Container(
      padding: const EdgeInsets.fromLTRB(22, 28, 22, 22),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        gradient: LinearGradient(
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
          colors: [colors.ink, colors.inkSoft],
        ),
      ),
      child: Column(children: [
        SizedBox.square(
          dimension: 170,
          child: Stack(fit: StackFit.expand, children: [
            TweenAnimationBuilder<double>(
              tween: Tween(begin: 0, end: (goal.percentComplete / 100).clamp(0, 1)),
              duration: const Duration(milliseconds: 900),
              curve: Curves.easeOutCubic,
              builder: (_, value, _) => CircularProgressIndicator(
                value: value,
                strokeWidth: 14,
                strokeCap: StrokeCap.round,
                color: overdue ? colors.warning : colors.mint,
                backgroundColor: colors.onInk.withValues(alpha: 0.1),
              ),
            ),
            Center(
              child: Text(
                '${goal.percentComplete.toStringAsFixed(0)}%',
                style: theme.textTheme.displaySmall?.copyWith(color: colors.onInk, fontWeight: FontWeight.w700),
              ),
            ),
          ]),
        ),
        const SizedBox(height: 20),
        MoneyText(
          goal.currentAmount,
          style: theme.textTheme.headlineSmall?.copyWith(color: colors.onInk, fontWeight: FontWeight.w700),
          currencyColor: colors.onInkMuted,
        ),
        Text(l10n.ofAmount(context.money(goal.targetAmount)), style: TextStyle(color: colors.onInkMuted)),
        if (completed) ...[
          const SizedBox(height: 14),
          _Badge(
            key: const Key('goalCompletedChip'),
            icon: Icons.emoji_events_rounded,
            text: l10n.statusCompleted,
            color: colors.mint,
          ),
        ],
        if (overdue) ...[
          const SizedBox(height: 14),
          _Badge(icon: Icons.schedule_rounded, text: '${l10n.statusOverdue} · ${l10n.extendDateHint}', color: colors.warning),
        ],
      ]),
    );
  }
}

class _Badge extends StatelessWidget {
  const _Badge({super.key, required this.icon, required this.text, required this.color});

  final IconData icon;
  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(20)),
        child: Row(mainAxisSize: MainAxisSize.min, children: [
          Icon(icon, color: color, size: 18),
          const SizedBox(width: 6),
          Flexible(child: Text(text, style: TextStyle(color: color, fontWeight: FontWeight.w600))),
        ]),
      );
}

class _StatTile extends StatelessWidget {
  const _StatTile({super.key, required this.icon, required this.label, required this.value, this.trailing});

  final IconData icon;
  final String label;
  final Widget value;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(children: [
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Icon(icon, size: 20, color: colors.textMuted),
              const SizedBox(height: 10),
              Text(label, style: TextStyle(color: colors.textMuted, fontSize: 13)),
              const SizedBox(height: 2),
              FittedBox(fit: BoxFit.scaleDown, alignment: AlignmentDirectional.centerStart, child: value),
            ]),
          ),
          ?trailing,
        ]),
      ),
    );
  }
}
