import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
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

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.tabBudget),
        actions: [
          IconButton(
            key: const Key('addCategory'),
            tooltip: l10n.addCategory,
            onPressed: () => showCategoryEditor(context),
            icon: const Icon(Icons.add_circle_outline),
          ),
        ],
      ),
      body: switch (budget) {
        AsyncData(:final value) => RefreshIndicator(
            onRefresh: () => ref.refresh(budgetProvider.future),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
              children: [
                BudgetSummaryCard(budget: value),
                const SizedBox(height: 16),
                for (final item in value.categories)
                  CategoryBudgetCard(item: item, onTap: () => showCategoryEditor(context, existing: item)),
              ],
            ),
          ),
        AsyncError(:final error) => Center(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Text(errorMessage(error, l10n)),
              TextButton(onPressed: () => ref.invalidate(budgetProvider), child: Text(l10n.retry)),
            ]),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}
