import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../../budget/data/budget_repository.dart';
import '../../goals/data/goal.dart';
import '../../profile/data/profile_repository.dart';
import '../data/dashboard_repository.dart';
import 'widgets/goal_strip.dart';
import 'widgets/hero_card.dart';
import 'widgets/insight_card.dart';
import 'widgets/spending_breakdown.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final dashboard = ref.watch(dashboardProvider);
    // Secondary data: the dashboard still shows if these fail.
    final name = ref.watch(profileProvider).value?.name;
    final budget = ref.watch(budgetProvider).value;

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: switch (dashboard) {
          AsyncData(:final value) => RefreshIndicator(
              onRefresh: () {
                ref.invalidate(budgetProvider);
                return ref.refresh(dashboardProvider.future);
              },
              child: ListView(
                // Bottom padding keeps the last card above the floating tab bar.
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
                children: [
                  _Header(name: name, unread: value.unreadNotifications),
                  const SizedBox(height: 20),
                  HeroCard(dashboard: value),
                  if (value.insights.isNotEmpty) ...[
                    SectionHeader(l10n.insights),
                    for (final insight in value.insights)
                      Padding(padding: const EdgeInsets.only(bottom: 10), child: InsightCard(insight: insight)),
                  ],
                  SectionHeader(
                    l10n.whereMoneyWent,
                    action: TextButton(onPressed: () => context.go(Routes.budget), child: Text(l10n.seeAll)),
                  ),
                  if (budget == null)
                    const Card(child: SizedBox(height: 120, child: Center(child: CircularProgressIndicator())))
                  else
                    SpendingBreakdown(budget: budget),
                  SectionHeader(
                    l10n.goals,
                    action: TextButton(onPressed: () => context.go(Routes.goals), child: Text(l10n.seeAll)),
                  ),
                  _Goals(goals: value.goals),
                ],
              ),
            ),
          AsyncError(:final error) => Center(
              child: EmptyState(
                icon: Icons.cloud_off_rounded,
                message: errorMessage(error, l10n),
                action: OutlinedButton(onPressed: () => ref.invalidate(dashboardProvider), child: Text(l10n.retry)),
              ),
            ),
          _ => const Center(child: CircularProgressIndicator()),
        },
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.name, required this.unread});

  final String? name;
  final int unread;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final greeting = DateTime.now().hour < 12 ? l10n.goodMorning : l10n.goodEvening;

    return Row(children: [
      Expanded(
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(greeting, style: TextStyle(color: context.mizan.textMuted)),
          Text(name ?? l10n.appName, style: Theme.of(context).textTheme.headlineSmall, maxLines: 1, overflow: TextOverflow.ellipsis),
        ]),
      ),
      SoftIconButton(
        key: const Key('notificationsBell'),
        icon: Icons.notifications_none_rounded,
        tooltip: l10n.notifications,
        badge: unread,
        onPressed: () => context.push(Routes.notifications),
      ),
      const SizedBox(width: 10),
      SoftIconButton(
        key: const Key('openSettings'),
        icon: Icons.tune_rounded,
        tooltip: l10n.settings,
        onPressed: () => context.push(Routes.settings),
      ),
    ]);
  }
}

class _Goals extends StatelessWidget {
  const _Goals({required this.goals});

  final List<Goal> goals;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    if (goals.isEmpty) {
      return Card(
        child: EmptyState(
          icon: Icons.flag_rounded,
          message: l10n.noGoals,
          action: FilledButton.tonal(onPressed: () => context.push(Routes.newGoal), child: Text(l10n.addGoal)),
        ),
      );
    }
    return GoalStrip(goals: goals);
  }
}
