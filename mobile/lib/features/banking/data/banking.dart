import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/format/json.dart';
import '../../../core/network/api_client.dart';
import '../../transactions/data/transaction.dart';

enum AccountKind { current, savings, creditCard, chargeCard, prepaid, eMoney, other }

class BankAccount {
  const BankAccount({
    required this.id,
    required this.kind,
    required this.currency,
    required this.ibanMasked,
    required this.nickname,
    required this.includeInBudget,
    required this.balance,
  });

  factory BankAccount.fromJson(Map<String, dynamic> json) => BankAccount(
        id: json['id'] as String,
        kind: switch (json['kind']) {
          'Current' => AccountKind.current,
          'Savings' => AccountKind.savings,
          'CreditCard' => AccountKind.creditCard,
          'ChargeCard' => AccountKind.chargeCard,
          'Prepaid' => AccountKind.prepaid,
          'EMoney' => AccountKind.eMoney,
          _ => AccountKind.other,
        },
        currency: json['currency'] as String,
        ibanMasked: json['ibanMasked'] as String?,
        nickname: json['nickname'] as String,
        includeInBudget: json['includeInBudget'] as bool,
        balance: json['balance'] == null ? null : moneyAmount(json['balance']),
      );

  final String id;
  final AccountKind kind;
  final String currency;
  final String? ibanMasked;
  final String nickname;
  final bool includeInBudget;
  final double? balance;

  bool get isSar => currency == 'SAR';
}

class BankConnection {
  const BankConnection({
    required this.id,
    required this.bankName,
    required this.isActive,
    required this.consentDaysLeft,
    required this.lastSyncedAt,
    required this.accounts,
  });

  factory BankConnection.fromJson(Map<String, dynamic> json) => BankConnection(
        id: json['id'] as String,
        bankName: json['bankName'] as String,
        isActive: json['status'] == 'Active',
        consentDaysLeft: json['consentDaysLeft'] as int,
        lastSyncedAt: json['lastSyncedAt'] == null ? null : DateTime.parse(json['lastSyncedAt'] as String).toLocal(),
        accounts: listOf(json['accounts'], BankAccount.fromJson),
      );

  final String id;
  final String bankName;
  final bool isActive;
  final int consentDaysLeft;
  final DateTime? lastSyncedAt;
  final List<BankAccount> accounts;

  // BR-C5: the app starts warning two weeks before the consent ends.
  bool get expiresSoon => isActive && consentDaysLeft <= 14;
}

// What the Lean link screen needs. Made by the API (it holds the secrets) for one session.
class LinkSession {
  const LinkSession({
    required this.appToken,
    required this.customerId,
    required this.accessToken,
    required this.sandbox,
    required this.permissions,
    required this.accessTo,
    required this.successRedirectUrl,
    required this.failRedirectUrl,
  });

  factory LinkSession.fromJson(Map<String, dynamic> json) => LinkSession(
        appToken: json['appToken'] as String,
        customerId: json['customerId'] as String,
        accessToken: json['accessToken'] as String,
        sandbox: json['sandbox'] as bool,
        permissions: (json['permissions'] as List<dynamic>).cast<String>(),
        accessTo: DateTime.parse(json['accessTo'] as String),
        successRedirectUrl: json['successRedirectUrl'] as String,
        failRedirectUrl: json['failRedirectUrl'] as String,
      );

  final String appToken;
  final String customerId;
  final String accessToken;
  final bool sandbox;
  final List<String> permissions;
  final DateTime accessTo;
  final String successRedirectUrl;
  final String failRedirectUrl;
}

class ReviewItem {
  const ReviewItem({required this.transaction, required this.reason, required this.candidate});

  factory ReviewItem.fromJson(Map<String, dynamic> json) => ReviewItem(
        transaction: Transaction.fromJson(json['transaction'] as Map<String, dynamic>),
        reason: json['reason'] == 'PossibleDuplicate' ? ReviewReason.possibleDuplicate : ReviewReason.possibleTransfer,
        candidate: json['candidate'] == null ? null : Transaction.fromJson(json['candidate'] as Map<String, dynamic>),
      );

  final Transaction transaction;
  final ReviewReason reason;

  // For a possible duplicate: the manual expense it may repeat.
  final Transaction? candidate;
}

enum ReviewDecision { sameAsManual, notDuplicate, ownTransfer, spending }

class BankingRepository {
  BankingRepository(this._dio);

  final Dio _dio;

  Future<LinkSession> createLinkSession() async {
    final response = await apiCall(() => _dio.post<Map<String, dynamic>>('/api/bank-connections/link'));
    return LinkSession.fromJson(response.data!);
  }

  Future<List<BankConnection>> discover() async {
    final response = await apiCall(() => _dio.post<List<dynamic>>('/api/bank-connections/discover'));
    return listOf(response.data, BankConnection.fromJson);
  }

  Future<List<BankConnection>> list() async {
    final response = await apiCall(() => _dio.get<List<dynamic>>('/api/bank-connections'));
    return listOf(response.data, BankConnection.fromJson);
  }

  Future<void> syncAll() => apiCall(() => _dio.post<void>('/api/bank-connections/sync'));

  Future<void> sync(String connectionId) => apiCall(() => _dio.post<void>('/api/bank-connections/$connectionId/sync'));

  Future<void> remove(String connectionId, {required bool keepTransactions}) => apiCall(() => _dio.delete<void>(
        '/api/bank-connections/$connectionId',
        queryParameters: {'keepTransactions': keepTransactions},
      ));

  Future<void> setAccountIncluded(String accountId, bool include) =>
      apiCall(() => _dio.patch<void>('/api/bank-accounts/$accountId', data: {'includeInBudget': include}));

  Future<List<ReviewItem>> reviewItems() async {
    final response = await apiCall(() => _dio.get<List<dynamic>>('/api/transactions/review'));
    return listOf(response.data, ReviewItem.fromJson);
  }

  Future<void> resolve(String transactionId, ReviewDecision decision) => apiCall(() => _dio.post<void>(
        '/api/transactions/$transactionId/review',
        data: {
          'decision': switch (decision) {
            ReviewDecision.sameAsManual => 'SameAsManual',
            ReviewDecision.notDuplicate => 'NotDuplicate',
            ReviewDecision.ownTransfer => 'OwnTransfer',
            ReviewDecision.spending => 'Spending',
          },
        },
      ));

  Future<void> changeCategory(String transactionId, String categoryId, {required bool rememberMerchant}) =>
      apiCall(() => _dio.put<void>(
            '/api/transactions/$transactionId/category',
            data: {'categoryId': categoryId, 'rememberMerchant': rememberMerchant},
          ));

  Future<void> setExcluded(String transactionId, bool excluded) =>
      apiCall(() => _dio.put<void>('/api/transactions/$transactionId/excluded', data: {'excluded': excluded}));

  Future<void> setNote(String transactionId, String? note) =>
      apiCall(() => _dio.put<void>('/api/transactions/$transactionId/note', data: {'note': note}));
}

final bankingRepositoryProvider = Provider<BankingRepository>((ref) => BankingRepository(ref.watch(dioProvider)));

final bankConnectionsProvider = FutureProvider.autoDispose<List<BankConnection>>(
  (ref) => ref.watch(bankingRepositoryProvider).list(),
);

final reviewItemsProvider = FutureProvider.autoDispose<List<ReviewItem>>(
  (ref) => ref.watch(bankingRepositoryProvider).reviewItems(),
);
