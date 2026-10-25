import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../budget/data/budget_repository.dart';
import '../../dashboard/data/dashboard_repository.dart';
import '../data/transaction.dart';
import '../data/transaction_repository.dart';

class TransactionList {
  const TransactionList({required this.items, required this.totalCount, this.isLoadingMore = false});

  final List<Transaction> items;
  final int totalCount;
  final bool isLoadingMore;

  bool get hasMore => items.length < totalCount;

  TransactionList copyWith({List<Transaction>? items, int? totalCount, bool? isLoadingMore}) => TransactionList(
        items: items ?? this.items,
        totalCount: totalCount ?? this.totalCount,
        isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      );
}

// Loads expenses one page at a time: the first page on open, the next when the user scrolls to the end.
class TransactionsController extends AsyncNotifier<TransactionList> {
  int _page = 1;

  @override
  Future<TransactionList> build() async {
    _page = 1;
    final first = await ref.read(transactionRepositoryProvider).list(page: 1);
    return TransactionList(items: first.items, totalCount: first.totalCount);
  }

  Future<void> loadMore() async {
    final current = state.value;
    if (current == null || !current.hasMore || current.isLoadingMore) return;

    state = AsyncData(current.copyWith(isLoadingMore: true));
    try {
      final next = await ref.read(transactionRepositoryProvider).list(page: _page + 1);
      _page++;
      state = AsyncData(current.copyWith(
        items: [...current.items, ...next.items],
        totalCount: next.totalCount,
        isLoadingMore: false,
      ));
    } catch (_) {
      // Keep what is already on screen; scrolling again retries.
      state = AsyncData(current.copyWith(isLoadingMore: false));
    }
  }
}

final transactionsControllerProvider =
    AsyncNotifierProvider.autoDispose<TransactionsController, TransactionList>(TransactionsController.new);

// Add / edit / delete. After every change the list, the dashboard, and the budget reload,
// because one expense changes totals, category progress, insights, and maybe notifications.
class ExpenseActions {
  ExpenseActions(this._ref);

  final Ref _ref;

  TransactionRepository get _repository => _ref.read(transactionRepositoryProvider);

  Future<void> add(ExpenseInput input) async {
    await _repository.create(input);
    _refresh();
  }

  Future<void> update(String id, ExpenseInput input) async {
    await _repository.update(id, input);
    _refresh();
  }

  Future<void> delete(String id) async {
    await _repository.delete(id);
    _refresh();
  }

  void _refresh() {
    _ref.invalidate(transactionsControllerProvider);
    _ref.invalidate(dashboardProvider);
    _ref.invalidate(budgetProvider);
  }
}

final expenseActionsProvider = Provider<ExpenseActions>(ExpenseActions.new);
