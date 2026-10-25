import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../data/budget_repository.dart';
import 'widgets/budget_summary_card.dart';
import 'widgets/category_budget_card.dart';
import 'widgets/category_editor_sheet.dart';

// UC-20..UC-24 and UC-51: the plan for the current period, category by category.
class BudgetScreen extends ConsumerWidget {
  const BudgetScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final budget = ref.watch(budgetProvider);
    final period = budget.value?.period;

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: switch (budget) {
          AsyncData(:final value) => RefreshIndicator(
              onRefresh: () => ref.refresh(budgetProvider.future),
              child: ListView(
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
                children: [
                  PageHeader(
                    title: l10n.tabBudget,
                    subtitle: period == null ? null : l10n.periodRange(context.shortDate(period.start), context.shortDate(period.end)),
                    action: SoftIconButton(
                      key: const Key('addCategory'),
                      icon: Icons.add_rounded,
                      tooltip: l10n.addCategory,
                      onPressed: () => showCategoryEditor(context),
                    ),
                  ),
                  BudgetSummaryCard(budget: value),
                  const SizedBox(height: 20),
                  for (final item in value.categories)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: CategoryBudgetCard(item: item, onTap: () => showCategoryEditor(context, existing: item)),
                    ),
                ],
              ),
            ),
          AsyncError(:final error) => Center(
              child: EmptyState(
                icon: Icons.cloud_off_rounded,
                message: errorMessage(error, l10n),
                action: OutlinedButton(onPressed: () => ref.invalidate(budgetProvider), child: Text(l10n.retry)),
              ),
            ),
          _ => const Center(child: CircularProgressIndicator()),
        },
      ),
    );
  }
}
