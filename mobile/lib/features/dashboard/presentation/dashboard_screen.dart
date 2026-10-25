import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/widgets/language_button.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/application/auth_controller.dart';
import '../../categories/data/category.dart';
import '../../categories/presentation/category_label.dart';
import '../../profile/data/profile_repository.dart';
import '../data/dashboard_repository.dart';
import 'widgets/goals_section.dart';
import 'widgets/insights_section.dart';
import 'widgets/summary_card.dart';
import 'widgets/top_spending_section.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final dashboard = ref.watch(dashboardProvider);
    // The greeting is a nice-to-have: the dashboard still shows if the profile fails.
    final name = ref.watch(profileProvider).value?.name;
    final categoryNames = {
      for (final c in ref.watch(categoriesProvider).value ?? const <Category>[]) c.id: categoryLabel(c, l10n),
    };

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.appName),
        actions: [
          IconButton(
            tooltip: l10n.notifications,
            onPressed: null, // The notifications screen comes in a later step.
            icon: Badge.count(
              count: dashboard.value?.unreadNotifications ?? 0,
              isLabelVisible: (dashboard.value?.unreadNotifications ?? 0) > 0,
              child: const Icon(Icons.notifications_outlined),
            ),
          ),
          const LanguageButton(),
          IconButton(
            tooltip: l10n.logout,
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: switch (dashboard) {
        AsyncData(:final value) => RefreshIndicator(
            // Pull down to reload. Waiting on .future keeps the spinner until the data arrives.
            onRefresh: () => ref.refresh(dashboardProvider.future),
            child: ListView(
              // Bottom padding leaves room for the "Add expense" button.
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
              children: [
                SummaryCard(dashboard: value, greeting: name == null ? null : l10n.welcome(name)),
                InsightsSection(insights: value.insights),
                TopSpendingSection(items: value.topSpending, totalSpent: value.totalSpent, nameOf: categoryNames),
                GoalsSection(goals: value.goals),
              ],
            ),
          ),
        AsyncError(:final error) => Center(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Text(errorMessage(error, l10n)),
              TextButton(onPressed: () => ref.invalidate(dashboardProvider), child: Text(l10n.retry)),
            ]),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}
