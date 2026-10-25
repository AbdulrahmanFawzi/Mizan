import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../../budget/data/budget_repository.dart';
import '../../categories/data/category.dart';
import '../application/transactions_controller.dart';
import '../data/transaction.dart';
import 'widgets/transaction_tile.dart';

class TransactionsScreen extends ConsumerWidget {
  const TransactionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final transactions = ref.watch(transactionsControllerProvider);
    final categories = {for (final c in ref.watch(categoriesProvider).value ?? const <Category>[]) c.id: c};
    final spent = ref.watch(budgetProvider).value?.totalSpent;

    final header = PageHeader(
      title: l10n.tabExpenses,
      subtitle: spent == null ? null : '${l10n.spentThisPeriod}: ${context.money(spent)}',
    );

    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: switch (transactions) {
          AsyncData(:final value) => RefreshIndicator(
              onRefresh: () => ref.refresh(transactionsControllerProvider.future),
              child: NotificationListener<ScrollNotification>(
                // Near the bottom? Fetch the next page.
                onNotification: (notification) {
                  if (notification.metrics.extentAfter < 300) {
                    ref.read(transactionsControllerProvider.notifier).loadMore();
                  }
                  return false;
                },
                child: _DayList(
                  header: header,
                  days: _groupByDay(value.items),
                  hasMore: value.hasMore,
                  categories: categories,
                ),
              ),
            ),
          AsyncError(:final error) => Center(
              child: EmptyState(
                icon: Icons.cloud_off_rounded,
                message: errorMessage(error, l10n),
                action: OutlinedButton(
                  onPressed: () => ref.invalidate(transactionsControllerProvider),
                  child: Text(l10n.retry),
                ),
              ),
            ),
          _ => const Center(child: CircularProgressIndicator()),
        },
      ),
    );
  }

  // The API returns expenses newest first, so equal dates are always next to each other.
  static List<List<Transaction>> _groupByDay(List<Transaction> items) {
    final days = <List<Transaction>>[];
    for (final item in items) {
      if (days.isEmpty || days.last.first.date != item.date) {
        days.add([item]);
      } else {
        days.last.add(item);
      }
    }
    return days;
  }
}

class _DayList extends StatelessWidget {
  const _DayList({required this.header, required this.days, required this.hasMore, required this.categories});

  final Widget header;
  final List<List<Transaction>> days;
  final bool hasMore;
  final Map<String, Category> categories;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    if (days.isEmpty) {
      return ListView(padding: const EdgeInsets.fromLTRB(20, 12, 20, 120), children: [
        header,
        Card(child: EmptyState(icon: Icons.receipt_long_rounded, message: l10n.noExpenses)),
      ]);
    }

    // Builds lazily: one item for the header, one per day, and a spinner while more pages load.
    return ListView.builder(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
      itemCount: days.length + 1 + (hasMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index == 0) return header;
        if (index > days.length) {
          return const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator()));
        }

        final day = days[index - 1];
        return Padding(
          padding: const EdgeInsets.only(bottom: 16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            DayHeader(day.first.date),
            Card(
              clipBehavior: Clip.antiAlias,
              child: Column(children: [
                for (final (i, transaction) in day.indexed) ...[
                  if (i > 0) const Divider(indent: 72),
                  TransactionTile(
                    transaction: transaction,
                    category: categories[transaction.categoryId],
                    onTap: () => context.push(Routes.editExpense(transaction.id), extra: transaction),
                  ),
                ],
              ]),
            ),
          ]),
        );
      },
    );
  }
}
