import '../../../core/format/json.dart';

// How a transaction counts in the budget (decided by the API, never by the app).
enum Treatment { spending, refund, ownTransfer, incoming, outsideBudget }

enum ReviewReason { possibleDuplicate, possibleTransfer }

class Transaction {
  const Transaction({
    required this.id,
    required this.amount,
    required this.categoryId,
    required this.date,
    required this.merchant,
    required this.goalId,
    this.isFromBank = false,
    this.isCredit = false,
    this.treatment = Treatment.spending,
    this.categoryIsAutomatic = false,
    double? budgetEffect,
    this.isPending = false,
    this.bankAccountId,
    this.fee,
    this.note,
    this.isExcluded = false,
    this.isCashWithdrawal = false,
    this.review,
    this.reviewCandidateId,
    this.possibleDuplicateOf,
  }) : budgetEffect = budgetEffect ?? amount;

  // Fields added for bank transactions (MVP 1) are optional, so older API answers still parse.
  factory Transaction.fromJson(Map<String, dynamic> json) {
    final amount = moneyAmount(json['amount']);
    return Transaction(
      id: json['id'] as String,
      amount: amount,
      categoryId: json['categoryId'] as String,
      date: parseDate(json['date']),
      merchant: json['merchant'] as String?,
      goalId: json['goalId'] as String?,
      isFromBank: json['source'] == 'Bank',
      isCredit: json['direction'] == 'Credit',
      treatment: switch (json['treatment']) {
        'Refund' => Treatment.refund,
        'OwnTransfer' => Treatment.ownTransfer,
        'Incoming' => Treatment.incoming,
        'OutsideBudget' => Treatment.outsideBudget,
        _ => Treatment.spending,
      },
      categoryIsAutomatic: json['categorySource'] is String && json['categorySource'] != 'User',
      budgetEffect: json['budgetEffect'] == null ? amount : moneyAmount(json['budgetEffect']),
      isPending: json['bankStatus'] == 'Pending',
      bankAccountId: json['bankAccountId'] as String?,
      fee: json['fee'] == null ? null : moneyAmount(json['fee']),
      note: json['note'] as String?,
      isExcluded: json['isExcluded'] as bool? ?? false,
      isCashWithdrawal: json['isCashWithdrawal'] as bool? ?? false,
      review: switch (json['review']) {
        'PossibleDuplicate' => ReviewReason.possibleDuplicate,
        'PossibleTransfer' => ReviewReason.possibleTransfer,
        _ => null,
      },
      reviewCandidateId: json['reviewCandidateId'] as String?,
      possibleDuplicateOf: json['possibleDuplicateOf'] as String?,
    );
  }

  final String id;
  final double amount;
  final String categoryId;
  final DateTime date;
  final String? merchant;

  // Set when this is money saved toward a goal; its category must stay "Savings".
  final String? goalId;

  // Bank transactions (MVP 1).
  final bool isFromBank;
  final bool isCredit;
  final Treatment treatment;
  final bool categoryIsAutomatic;

  // What it adds to its category's spending: 0 for transfers, exclusions and pending reviews,
  // negative for refunds.
  final double budgetEffect;
  final bool isPending;
  final String? bankAccountId;
  final double? fee;
  final String? note;
  final bool isExcluded;
  final bool isCashWithdrawal;
  final ReviewReason? review;
  final String? reviewCandidateId;

  // A manual expense waiting next to a bank transaction that may be the same purchase.
  final String? possibleDuplicateOf;

  bool get isGoalContribution => goalId != null;

  bool get needsReview => review != null;

  bool get countsInBudget => budgetEffect != 0;
}

// What the form sends for both "add" and "edit".
class ExpenseInput {
  const ExpenseInput({required this.amount, required this.categoryId, required this.date, this.merchant});

  final double amount;
  final String categoryId;
  final DateTime date;
  final String? merchant;

  Map<String, dynamic> toJson() => {
        'amount': amount,
        'categoryId': categoryId,
        'date': apiDate(date),
        if (merchant != null && merchant!.trim().isNotEmpty) 'merchant': merchant!.trim(),
      };
}

class TransactionPage {
  const TransactionPage({required this.items, required this.totalCount});

  factory TransactionPage.fromJson(Map<String, dynamic> json) => TransactionPage(
        items: listOf(json['items'], Transaction.fromJson),
        totalCount: json['totalCount'] as int,
      );

  final List<Transaction> items;
  final int totalCount;
}
