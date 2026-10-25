import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../l10n/app_localizations.dart';
import '../router/app_router.dart';

// The bottom tabs (Home | Budget | Goals | Expenses) and the "Add expense" button shared by both.
// IndexedStack keeps each tab's scroll position when switching.
class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      body: shell,
      floatingActionButton: FloatingActionButton.extended(
        key: const Key('addExpense'),
        onPressed: () => context.push(Routes.newExpense),
        icon: const Icon(Icons.add),
        label: Text(l10n.addExpense),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: shell.currentIndex,
        // Tapping the current tab again returns it to its start.
        onDestinationSelected: (index) => shell.goBranch(index, initialLocation: index == shell.currentIndex),
        destinations: [
          NavigationDestination(icon: const Icon(Icons.home_outlined), selectedIcon: const Icon(Icons.home), label: l10n.tabHome),
          NavigationDestination(
            key: const Key('budgetTab'),
            icon: const Icon(Icons.pie_chart_outline),
            selectedIcon: const Icon(Icons.pie_chart),
            label: l10n.tabBudget,
          ),
          NavigationDestination(
            key: const Key('goalsTab'),
            icon: const Icon(Icons.flag_outlined),
            selectedIcon: const Icon(Icons.flag),
            label: l10n.tabGoals,
          ),
          NavigationDestination(
            key: const Key('expensesTab'),
            icon: const Icon(Icons.receipt_long_outlined),
            selectedIcon: const Icon(Icons.receipt_long),
            label: l10n.tabExpenses,
          ),
        ],
      ),
    );
  }
}
