import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../l10n/app_localizations.dart';
import '../../categories/data/category.dart';
import '../../categories/presentation/category_label.dart';
import '../application/transactions_controller.dart';
import 'widgets/transaction_tile.dart';

class TransactionsScreen extends ConsumerWidget {
  const TransactionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final transactions = ref.watch(transactionsControllerProvider);
    final categories = ref.watch(categoriesProvider).value ?? const <Category>[];
    final names = {for (final c in categories) c.id: categoryLabel(c, l10n)};

    return Scaffold(
      appBar: AppBar(title: Text(l10n.tabExpenses)),
      body: switch (transactions) {
        AsyncData(:final value) when value.items.isEmpty => Center(child: Text(l10n.noExpenses)),
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
              child: ListView.builder(
                // Leave room so the floating button doesn't cover the last row.
                padding: const EdgeInsets.only(bottom: 96),
                itemCount: value.items.length + (value.hasMore ? 1 : 0),
                itemBuilder: (context, index) {
                  if (index == value.items.length) {
                    return const Padding(
                      padding: EdgeInsets.all(16),
                      child: Center(child: CircularProgressIndicator()),
                    );
                  }

                  final transaction = value.items[index];
                  final startsNewDay = index == 0 || value.items[index - 1].date != transaction.date;

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (startsNewDay) DayHeader(transaction.date),
                      TransactionTile(
                        transaction: transaction,
                        categoryName: names[transaction.categoryId] ?? '',
                        onTap: () => context.push(Routes.editExpense(transaction.id), extra: transaction),
                      ),
                    ],
                  );
                },
              ),
            ),
          ),
        AsyncError(:final error) => Center(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Text(errorMessage(error, l10n)),
              TextButton(onPressed: () => ref.invalidate(transactionsControllerProvider), child: Text(l10n.retry)),
            ]),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}
