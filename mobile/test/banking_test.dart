import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/banking/application/banking_actions.dart';
import 'package:mizan/features/banking/data/banking.dart';
import 'package:mizan/features/banking/presentation/bank_connections_screen.dart';
import 'package:mizan/features/banking/presentation/bank_link_screen.dart';
import 'package:mizan/features/banking/presentation/bank_transaction_screen.dart';
import 'package:mizan/features/banking/presentation/review_screen.dart';
import 'package:mizan/features/categories/data/category.dart';
import 'package:mizan/features/transactions/data/transaction.dart';
import 'package:mizan/features/transactions/presentation/widgets/transaction_tile.dart';

import 'helpers/test_app.dart';

class FakeBankingActions implements BankingActions {
  final calls = <String>[];

  @override
  Future<LinkSession> createLinkSession() async => LinkSession(
        appToken: 'app',
        customerId: 'cust',
        accessToken: 'token',
        sandbox: true,
        permissions: const ['accounts', 'balance', 'transactions'],
        accessTo: DateTime(2027, 10, 25),
        successRedirectUrl: 'https://ok',
        failRedirectUrl: 'https://fail',
      );

  @override
  Future<List<BankConnection>> finishLinking() async {
    calls.add('finishLinking');
    return [];
  }

  @override
  Future<void> syncInBackground() async => calls.add('syncInBackground');

  @override
  Future<void> sync(String connectionId) async => calls.add('sync $connectionId');

  @override
  Future<void> remove(String connectionId, {required bool keepTransactions}) async =>
      calls.add('remove $connectionId keep=$keepTransactions');

  @override
  Future<void> setAccountIncluded(String accountId, bool include) async => calls.add('include $accountId $include');

  @override
  Future<void> resolve(String transactionId, ReviewDecision decision, {bool remember = false}) async =>
      calls.add('resolve $transactionId ${decision.name}${remember ? ' remember' : ''}');

  @override
  Future<void> changeCategory(String transactionId, String categoryId, {required bool rememberMerchant}) async =>
      calls.add('category $transactionId $categoryId remember=$rememberMerchant');

  @override
  Future<void> setExcluded(String transactionId, bool excluded) async => calls.add('excluded $transactionId $excluded');

  @override
  Future<void> setNote(String transactionId, String? note) async => calls.add('note $transactionId $note');
}

Map<String, dynamic> _bankJson({
  String id = 't1',
  double amount = 120,
  String direction = 'Debit',
  String treatment = 'Spending',
  double? effect,
  String? bankStatus = 'Booked',
  String? review,
  bool excluded = false,
}) =>
    {
      'id': id,
      'amount': {'amount': amount, 'currency': 'SAR'},
      'categoryId': 'other',
      'date': '2026-10-24',
      'merchant': 'Al Baik',
      'goalId': null,
      'source': 'Bank',
      'direction': direction,
      'treatment': treatment,
      'categorySource': 'Provider',
      'budgetEffect': {'amount': effect ?? amount, 'currency': 'SAR'},
      'bankStatus': bankStatus,
      'bankAccountId': 'a1',
      'fee': {'amount': 1.15, 'currency': 'SAR'},
      'note': null,
      'isExcluded': excluded,
      'isCashWithdrawal': false,
      'review': review,
      'reviewCandidateId': null,
      'possibleDuplicateOf': null,
    };

const _categories = [
  Category(id: 'other', name: 'أخرى', kind: CategoryKind.other),
  Category(id: 'food', name: 'Food', kind: CategoryKind.regular),
];

BankConnection _connection({bool active = true, int daysLeft = 300}) => BankConnection(
      id: 'c1',
      bankName: 'Mockbank 1',
      isActive: active,
      consentDaysLeft: daysLeft,
      lastSyncedAt: DateTime(2026, 10, 25, 9),
      accounts: const [
        BankAccount(
            id: 'a1', kind: AccountKind.current, currency: 'SAR', ibanMasked: 'SA•• •••• 0001', nickname: 'Current', includeInBudget: true, balance: 5000),
        BankAccount(
            id: 'a2', kind: AccountKind.current, currency: 'USD', ibanMasked: null, nickname: 'Dollars', includeInBudget: false, balance: 10),
      ],
    );

void main() {
  late FakeBankingActions actions;

  setUp(() => actions = FakeBankingActions());

  group('JSON', () {
    test('a bank transaction reads its bank fields', () {
      final t = Transaction.fromJson(_bankJson(treatment: 'Refund', direction: 'Credit', effect: -120, bankStatus: 'Pending'));

      expect(t.isFromBank, isTrue);
      expect(t.isCredit, isTrue);
      expect(t.treatment, Treatment.refund);
      expect(t.budgetEffect, -120);
      expect(t.isPending, isTrue);
      expect(t.fee, 1.15);
      expect(t.categoryIsAutomatic, isTrue);
    });

    test('an old-style answer (no bank fields) is a counted manual expense', () {
      final t = Transaction.fromJson({
        'id': 't1',
        'amount': {'amount': 50, 'currency': 'SAR'},
        'categoryId': 'c',
        'date': '2026-10-24',
        'merchant': null,
        'goalId': null,
      });

      expect(t.isFromBank, isFalse);
      expect(t.budgetEffect, 50);
      expect(t.countsInBudget, isTrue);
    });

    test('a connection and a review item parse', () {
      final c = BankConnection.fromJson({
        'id': 'c1',
        'bankIdentifier': 'LEA1',
        'bankName': 'Mockbank 1',
        'status': 'Expired',
        'consentExpiresAt': '2027-10-25T00:00:00Z',
        'consentDaysLeft': 0,
        'lastSyncedAt': null,
        'historyImported': true,
        'accounts': [
          {'id': 'a1', 'kind': 'CreditCard', 'currency': 'SAR', 'ibanMasked': null, 'nickname': 'Card', 'includeInBudget': true, 'balance': null, 'balanceAsOf': null},
        ],
      });
      final item = ReviewItem.fromJson({'transaction': _bankJson(review: 'PossibleTransfer'), 'reason': 'PossibleTransfer', 'candidate': null});

      expect(c.isActive, isFalse);
      expect(c.accounts.single.kind, AccountKind.creditCard);
      expect(item.reason, ReviewReason.possibleTransfer);
      expect(item.transaction.needsReview, isTrue);
    });
  });

  group('Bank accounts screen', () {
    Future<void> pump(WidgetTester tester, List<BankConnection> connections) async {
      usePhoneScreen(tester);
      await tester.pumpWidget(testApp(const BankConnectionsScreen(), overrides: [
        bankConnectionsProvider.overrideWith((ref) async => connections),
        bankingActionsProvider.overrideWithValue(actions),
      ]));
      await tester.pumpAndSettle();
    }

    testWidgets('with no bank, explains linking before anything is shared', (tester) async {
      await pump(tester, []);

      expect(find.text('Your expenses, automatically'), findsOneWidget);
      expect(find.textContaining('we can never move your money'), findsOneWidget);
      expect(find.byKey(const Key('linkBank')), findsOneWidget);
    });

    testWidgets('lists accounts; other currencies cannot be included', (tester) async {
      await pump(tester, [_connection()]);

      expect(find.text('Current'), findsOneWidget);
      await tester.tap(find.byKey(const Key('include-a1')));
      await tester.pump();
      expect(actions.calls, ['include a1 false']);

      final usd = tester.widget<Switch>(find.byKey(const Key('include-a2')));
      expect(usd.onChanged, isNull);
    });

    testWidgets('removing asks what to do with the imported transactions', (tester) async {
      await pump(tester, [_connection()]);

      await tester.tap(find.byKey(const Key('remove-c1')));
      await tester.pumpAndSettle();
      expect(find.text('Remove Mockbank 1?'), findsOneWidget);

      await tester.tap(find.byKey(const Key('keepBankTransactions')));
      await tester.pumpAndSettle();
      expect(actions.calls, ['remove c1 keep=true']);
    });

    testWidgets('an expiring link warns and offers to link again', (tester) async {
      await pump(tester, [_connection(daysLeft: 3)]);

      expect(find.text('Link ends in 3 days'), findsOneWidget);
      expect(find.text('Link again'), findsOneWidget);
    });
  });

  group('Review', () {
    Future<void> pump(WidgetTester tester, List<ReviewItem> items) async {
      usePhoneScreen(tester);
      await tester.pumpWidget(testApp(const ReviewScreen(), overrides: [
        reviewItemsProvider.overrideWith((ref) async => items),
        bankingActionsProvider.overrideWithValue(actions),
      ]));
      await tester.pumpAndSettle();
    }

    testWidgets('a possible duplicate shows both sides and can be merged', (tester) async {
      final bank = Transaction.fromJson(_bankJson(review: 'PossibleDuplicate'));
      final manual = Transaction(id: 'm1', amount: 120, categoryId: 'food', date: DateTime(2026, 10, 23), merchant: 'Albaik', goalId: null);
      await pump(tester, [ReviewItem(transaction: bank, reason: ReviewReason.possibleDuplicate, candidate: manual)]);

      expect(find.text('Is this the purchase you recorded yourself?'), findsOneWidget);
      expect(find.text('From the bank'), findsOneWidget);
      expect(find.text('You recorded'), findsOneWidget);

      await tester.tap(find.byKey(const Key('yes-t1')));
      await tester.pump();
      expect(actions.calls, ['resolve t1 sameAsManual']);
    });

    testWidgets('a possible transfer can be answered "spending"', (tester) async {
      final bank = Transaction.fromJson(_bankJson(review: 'PossibleTransfer', effect: 0));
      await pump(tester, [ReviewItem(transaction: bank, reason: ReviewReason.possibleTransfer, candidate: null)]);

      await tester.tap(find.byKey(const Key('no-t1')));
      await tester.pump();
      expect(actions.calls, ['resolve t1 spending']);
    });

    testWidgets('a transfer answer can be remembered for the account', (tester) async {
      final bank = Transaction.fromJson(_bankJson(review: 'PossibleTransfer', effect: 0));
      await pump(tester, [ReviewItem(transaction: bank, reason: ReviewReason.possibleTransfer, candidate: null, transferTo: 'My card')]);

      expect(find.text('Remember my answer for every transfer to “My card”'), findsOneWidget);
      await tester.tap(find.byKey(const Key('remember-t1')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('yes-t1')));
      await tester.pump();
      expect(actions.calls, ['resolve t1 ownTransfer remember']);
    });

    testWidgets('nothing to review says so', (tester) async {
      await pump(tester, []);

      expect(find.text('All clear — nothing to review'), findsOneWidget);
    });
  });

  group('Bank transaction', () {
    testWidgets('changing the category can remember the merchant, and it can be left out', (tester) async {
      usePhoneScreen(tester);
      await tester.pumpWidget(testApp(BankTransactionScreen(transaction: Transaction.fromJson(_bankJson())), overrides: [
        categoriesProvider.overrideWith((ref) async => _categories),
        bankingActionsProvider.overrideWithValue(actions),
      ]));
      await tester.pumpAndSettle();

      expect(find.text('Includes a 1.15 SAR bank fee'), findsOneWidget);
      expect(find.text('Category picked automatically'), findsOneWidget);

      await tester.tap(find.text('Food'));
      await tester.tap(find.byKey(const Key('rememberMerchant')));
      await tester.tap(find.byKey(const Key('countInBudget')));
      await tester.pump();
      await tester.tap(find.byKey(const Key('saveBankTransaction')));
      await tester.pumpAndSettle();

      expect(actions.calls, ['category t1 food remember=true', 'excluded t1 true']);
    });

    testWidgets('a transaction outside the budget says why, and can bring its account in', (tester) async {
      usePhoneScreen(tester);
      await tester.pumpWidget(testApp(
        BankTransactionScreen(transaction: Transaction.fromJson(_bankJson(treatment: 'OutsideBudget', effect: 0))),
        overrides: [
          categoriesProvider.overrideWith((ref) async => _categories),
          bankConnectionsProvider.overrideWith((ref) async => [_connection()]),
          bankingActionsProvider.overrideWithValue(actions),
        ],
      ));
      await tester.pumpAndSettle();

      expect(find.textContaining('which isn\'t part of your budget'), findsOneWidget);
      expect(find.byKey(const Key('countInBudget')), findsNothing);

      await tester.tap(find.byKey(const Key('includeAccount')));
      await tester.pumpAndSettle();
      expect(actions.calls, ['include a1 true']);
    });

    testWidgets('the tile marks money in and pending holds', (tester) async {
      usePhoneScreen(tester);
      await tester.pumpWidget(testApp(Material(
        child: Column(children: [
          TransactionTile(
            transaction: Transaction.fromJson(_bankJson(id: 'r', treatment: 'Refund', direction: 'Credit', effect: -120)),
            category: _categories.first,
            onTap: () {},
          ),
          TransactionTile(
            transaction: Transaction.fromJson(_bankJson(id: 'p', bankStatus: 'Pending')),
            category: _categories.first,
            onTap: () {},
          ),
        ]),
      )));
      await tester.pumpAndSettle();

      expect(find.text('+'), findsOneWidget);
      expect(find.text('Refund'), findsOneWidget);
      expect(find.text('Pending'), findsOneWidget);
      expect(find.text('From the bank'), findsNWidgets(2));
    });
  });

  testWidgets('a successful link imports the bank', (tester) async {
    usePhoneScreen(tester);
    await tester.pumpWidget(testApp(
      BankLinkScreen(
        linkerBuilder: (session, onDone) => TextButton(
          key: const Key('fakeLean'),
          onPressed: () => onDone(true),
          child: Text(session.customerId),
        ),
      ),
      overrides: [bankingActionsProvider.overrideWithValue(actions)],
    ));
    await tester.pumpAndSettle();

    expect(find.text('cust'), findsOneWidget);
    await tester.tap(find.byKey(const Key('fakeLean')));
    await tester.pumpAndSettle();

    expect(actions.calls, ['finishLinking']);
  });
}
