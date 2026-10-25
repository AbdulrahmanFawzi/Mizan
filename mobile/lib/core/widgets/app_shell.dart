import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../l10n/app_localizations.dart';
import '../router/app_router.dart';
import '../theme/mizan_colors.dart';

// A floating ink tab bar with the "add expense" button in the middle, where the thumb rests.
// IndexedStack (from StatefulShellRoute) keeps each tab's scroll position when switching.
class AppShell extends StatelessWidget {
  const AppShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      extendBody: true, // content scrolls behind the floating bar
      body: shell,
      bottomNavigationBar: SafeArea(
        minimum: const EdgeInsets.fromLTRB(16, 0, 16, 12),
        child: _TabBar(
          currentIndex: shell.currentIndex,
          onTap: (index) => shell.goBranch(index, initialLocation: index == shell.currentIndex),
          onAdd: () => context.push(Routes.newExpense),
          tabs: [
            (key: const Key('homeTab'), icon: Icons.home_outlined, selected: Icons.home_rounded, label: l10n.tabHome),
            (key: const Key('budgetTab'), icon: Icons.pie_chart_outline_rounded, selected: Icons.pie_chart_rounded, label: l10n.tabBudget),
            (key: const Key('goalsTab'), icon: Icons.flag_outlined, selected: Icons.flag_rounded, label: l10n.tabGoals),
            (key: const Key('expensesTab'), icon: Icons.receipt_long_outlined, selected: Icons.receipt_long_rounded, label: l10n.tabExpenses),
          ],
          addLabel: l10n.addExpense,
        ),
      ),
    );
  }
}

typedef _Tab = ({Key key, IconData icon, IconData selected, String label});

class _TabBar extends StatelessWidget {
  const _TabBar({
    required this.currentIndex,
    required this.onTap,
    required this.onAdd,
    required this.tabs,
    required this.addLabel,
  });

  final int currentIndex;
  final ValueChanged<int> onTap;
  final VoidCallback onAdd;
  final List<_Tab> tabs;
  final String addLabel;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;

    Widget tab(int index) {
      final item = tabs[index];
      final selected = index == currentIndex;
      return Expanded(
        child: Semantics(
          selected: selected,
          button: true,
          label: item.label,
          child: InkResponse(
            key: item.key,
            onTap: () => onTap(index),
            radius: 32,
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Icon(selected ? item.selected : item.icon, color: selected ? colors.mint : colors.onInkMuted),
              const SizedBox(height: 2),
              Text(
                item.label,
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
                  color: selected ? colors.mint : colors.onInkMuted,
                ),
              ),
            ]),
          ),
        ),
      );
    }

    return Container(
      height: 72,
      decoration: BoxDecoration(
        color: colors.ink,
        borderRadius: BorderRadius.circular(28),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.18), blurRadius: 24, offset: const Offset(0, 10))],
      ),
      child: Row(children: [
        tab(0),
        tab(1),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: Tooltip(
            message: addLabel,
            child: Material(
              color: colors.mint,
              shape: const CircleBorder(),
              child: InkWell(
                key: const Key('addExpense'),
                customBorder: const CircleBorder(),
                onTap: onAdd,
                child: SizedBox.square(
                  dimension: 52,
                  child: Icon(Icons.add_rounded, size: 30, color: colors.ink, semanticLabel: addLabel),
                ),
              ),
            ),
          ),
        ),
        tab(2),
        tab(3),
      ]),
    );
  }
}
