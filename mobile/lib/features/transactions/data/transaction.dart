import '../../../core/format/json.dart';

class Transaction {
  const Transaction({
    required this.id,
    required this.amount,
    required this.categoryId,
    required this.date,
    required this.merchant,
    required this.goalId,
  });

  factory Transaction.fromJson(Map<String, dynamic> json) => Transaction(
        id: json['id'] as String,
        amount: moneyAmount(json['amount']),
        categoryId: json['categoryId'] as String,
        date: parseDate(json['date']),
        merchant: json['merchant'] as String?,
        goalId: json['goalId'] as String?,
      );

  final String id;
  final double amount;
  final String categoryId;
  final DateTime date;
  final String? merchant;

  // Set when this is money saved toward a goal; its category must stay "Savings".
  final String? goalId;

  bool get isGoalContribution => goalId != null;
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
