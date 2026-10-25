import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../l10n/app_localizations.dart';
import '../data/goal_repository.dart';
import 'widgets/goal_card.dart';

// UC-43: all goals that are not archived.
class GoalsScreen extends ConsumerWidget {
  const GoalsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final goals = ref.watch(goalsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.tabGoals),
        actions: [
          IconButton(
            key: const Key('addGoal'),
            tooltip: l10n.addGoal,
            onPressed: () => context.push(Routes.newGoal),
            icon: const Icon(Icons.add_circle_outline),
          ),
        ],
      ),
      body: switch (goals) {
        AsyncData(:final value) when value.isEmpty => Center(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              const Icon(Icons.flag_outlined, size: 48),
              const SizedBox(height: 12),
              Text(l10n.noGoals),
              const SizedBox(height: 12),
              FilledButton.tonal(onPressed: () => context.push(Routes.newGoal), child: Text(l10n.addGoal)),
            ]),
          ),
        AsyncData(:final value) => RefreshIndicator(
            onRefresh: () => ref.refresh(goalsProvider.future),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
              children: [
                for (final goal in value)
                  GoalCard(goal: goal, onTap: () => context.push(Routes.goal(goal.id))),
              ],
            ),
          ),
        AsyncError(:final error) => Center(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Text(errorMessage(error, l10n)),
              TextButton(onPressed: () => ref.invalidate(goalsProvider), child: Text(l10n.retry)),
            ]),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}
