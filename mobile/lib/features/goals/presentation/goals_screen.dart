import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/money_text.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../data/goal.dart';
import '../data/goal_repository.dart';
import 'widgets/goal_card.dart';

// UC-43: all goals that are not archived.
class GoalsScreen extends ConsumerWidget {
  const GoalsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final goals = ref.watch(goalsProvider);

    final header = PageHeader(
      title: l10n.tabGoals,
      action: SoftIconButton(
        key: const Key('addGoal'),
        icon: Icons.add_rounded,
        tooltip: l10n.addGoal,
        onPressed: () => context.push(Routes.newGoal),
      ),
    );

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: switch (goals) {
          AsyncData(:final value) => RefreshIndicator(
              onRefresh: () => ref.refresh(goalsProvider.future),
              child: ListView(
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
                children: [
                  header,
                  if (value.isEmpty)
                    Card(
                      child: EmptyState(
                        icon: Icons.flag_rounded,
                        message: l10n.noGoals,
                        action: FilledButton(onPressed: () => context.push(Routes.newGoal), child: Text(l10n.addGoal)),
                      ),
                    )
                  else ...[
                    _TotalCard(goals: value),
                    const SizedBox(height: 20),
                    for (final goal in value)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: GoalCard(goal: goal, onTap: () => context.push(Routes.goal(goal.id))),
                      ),
                  ],
                ],
              ),
            ),
          AsyncError(:final error) => Center(
              child: EmptyState(
                icon: Icons.cloud_off_rounded,
                message: errorMessage(error, l10n),
                action: OutlinedButton(onPressed: () => ref.invalidate(goalsProvider), child: Text(l10n.retry)),
              ),
            ),
          _ => const Center(child: CircularProgressIndicator()),
        },
      ),
    );
  }
}

// All goals together: how much is saved out of everything the user is aiming for.
class _TotalCard extends StatelessWidget {
  const _TotalCard({required this.goals});

  final List<Goal> goals;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final saved = goals.fold(0.0, (sum, goal) => sum + goal.currentAmount);
    final target = goals.fold(0.0, (sum, goal) => sum + goal.targetAmount);

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
        Text(l10n.savedForGoals, style: TextStyle(color: colors.onInkMuted)),
        const SizedBox(height: 4),
        Row(crossAxisAlignment: CrossAxisAlignment.baseline, textBaseline: TextBaseline.alphabetic, children: [
          Flexible(
            child: FittedBox(
              fit: BoxFit.scaleDown,
              child: AnimatedMoney(
                saved,
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(color: colors.mint, fontWeight: FontWeight.w700),
                currencyColor: colors.onInkMuted,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Text(l10n.ofAmount(context.money(target)), style: TextStyle(color: colors.onInkMuted)),
        ]),
        const SizedBox(height: 16),
        ProgressBar(value: target == 0 ? 0 : saved / target, color: colors.mint),
      ]),
    );
  }
}
