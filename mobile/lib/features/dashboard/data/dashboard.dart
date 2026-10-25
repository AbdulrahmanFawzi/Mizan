import '../../../core/format/json.dart';
import '../../goals/data/goal.dart';

class BudgetPeriod {
  const BudgetPeriod({required this.start, required this.end, required this.lengthInDays, required this.daysElapsed});

  factory BudgetPeriod.fromJson(Map<String, dynamic> json) => BudgetPeriod(
        start: parseDate(json['start']),
        end: parseDate(json['end']),
        lengthInDays: json['lengthInDays'] as int,
        daysElapsed: json['daysElapsed'] as int,
      );

  final DateTime start;
  final DateTime end;
  final int lengthInDays;
  final int daysElapsed;

  int get daysLeft => lengthInDays - daysElapsed;
}

class TopSpending {
  const TopSpending({required this.categoryId, required this.name, required this.spent});

  factory TopSpending.fromJson(Map<String, dynamic> json) => TopSpending(
        categoryId: json['categoryId'] as String,
        name: json['name'] as String,
        spent: moneyAmount(json['spent']),
      );

  final String categoryId;
  final String name;
  final double spent;
}

enum InsightType { overBudget, projectedOverBudget }

class Insight {
  const Insight({required this.type, required this.categoryName, required this.message});

  factory Insight.fromJson(Map<String, dynamic> json) => Insight(
        type: InsightType.values.byName(_lowerFirst(json['type'] as String)),
        categoryName: json['categoryName'] as String,
        message: json['message'] as String,
      );

  final InsightType type;
  final String categoryName;
  final String message;
}

class Dashboard {
  const Dashboard({
    required this.period,
    required this.monthlyIncome,
    required this.totalSpent,
    required this.available,
    required this.budgetUsagePercent,
    required this.topSpending,
    required this.goals,
    required this.insights,
    required this.unreadNotifications,
  });

  factory Dashboard.fromJson(Map<String, dynamic> json) => Dashboard(
        period: BudgetPeriod.fromJson(json['period'] as Map<String, dynamic>),
        monthlyIncome: moneyAmount(json['monthlyIncome']),
        totalSpent: moneyAmount(json['totalSpent']),
        available: moneyAmount(json['available']),
        budgetUsagePercent: (json['budgetUsagePercent'] as num?)?.toDouble(),
        topSpending: listOf(json['topSpending'], TopSpending.fromJson),
        goals: listOf(json['goals'], Goal.fromJson),
        insights: listOf(json['insights'], Insight.fromJson),
        unreadNotifications: json['unreadNotifications'] as int,
      );

  final BudgetPeriod period;
  final double monthlyIncome;
  final double totalSpent;
  final double available;

  // null when no category has a limit yet.
  final double? budgetUsagePercent;
  final List<TopSpending> topSpending;
  final List<Goal> goals;
  final List<Insight> insights;
  final int unreadNotifications;
}

// The API sends enum names as "OverBudget"; Dart enums are "overBudget".
String _lowerFirst(String value) => value[0].toLowerCase() + value.substring(1);
