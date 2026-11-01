import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../budget/data/budget_repository.dart';
import '../../dashboard/data/dashboard_repository.dart';
import '../../notifications/application/notifications_controller.dart';
import '../../transactions/application/transactions_controller.dart';
import '../data/banking.dart';

// Everything that changes bank data. After each change the screens that show money reload:
// one bank transaction can move the budget, the dashboard, the expense list and notifications.
class BankingActions {
  BankingActions(this._ref);

  final Ref _ref;

  BankingRepository get _repository => _ref.read(bankingRepositoryProvider);

  Future<LinkSession> createLinkSession() => _repository.createLinkSession();

  // Right after the link screen: the API picks up the new bank and imports it.
  Future<List<BankConnection>> finishLinking() async {
    final connections = await _repository.discover();
    _refreshAll();
    return connections;
  }

  // OB-6: called when the app opens. Silent: a failure here must never bother the user.
  Future<void> syncInBackground() async {
    try {
      await _repository.syncAll();
      _refreshAll();
    } catch (_) {}
  }

  Future<void> sync(String connectionId) async {
    await _repository.sync(connectionId);
    _refreshAll();
  }

  Future<void> remove(String connectionId, {required bool keepTransactions}) async {
    await _repository.remove(connectionId, keepTransactions: keepTransactions);
    _refreshAll();
  }

  // The API also re-counts what this account already brought in, so every money screen reloads.
  Future<void> setAccountIncluded(String accountId, bool include) async {
    await _repository.setAccountIncluded(accountId, include);
    _refreshAll();
  }

  Future<void> resolve(String transactionId, ReviewDecision decision) async {
    await _repository.resolve(transactionId, decision);
    _refreshAll();
  }

  Future<void> changeCategory(String transactionId, String categoryId, {required bool rememberMerchant}) async {
    await _repository.changeCategory(transactionId, categoryId, rememberMerchant: rememberMerchant);
    _refreshAll();
  }

  Future<void> setExcluded(String transactionId, bool excluded) async {
    await _repository.setExcluded(transactionId, excluded);
    _refreshAll();
  }

  Future<void> setNote(String transactionId, String? note) async {
    await _repository.setNote(transactionId, note);
    _ref.invalidate(transactionsControllerProvider);
  }

  void _refreshAll() {
    _ref
      ..invalidate(bankConnectionsProvider)
      ..invalidate(reviewItemsProvider)
      ..invalidate(transactionsControllerProvider)
      ..invalidate(dashboardProvider)
      ..invalidate(budgetProvider)
      ..invalidate(notificationsControllerProvider);
  }
}

final bankingActionsProvider = Provider<BankingActions>(BankingActions.new);
