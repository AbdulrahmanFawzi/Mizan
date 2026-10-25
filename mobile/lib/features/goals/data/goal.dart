import '../../../core/format/json.dart';

enum GoalStatus { active, completed, overdue, archived }

class Goal {
  const Goal({
    required this.id,
    required this.name,
    required this.targetAmount,
    required this.initialAmount,
    required this.targetDate,
    required this.currentAmount,
    required this.remaining,
    required this.requiredPerPeriod,
    required this.remainingPeriods,
    required this.percentComplete,
    required this.status,
  });

  factory Goal.fromJson(Map<String, dynamic> json) => Goal(
        id: json['id'] as String,
        name: json['name'] as String,
        targetAmount: moneyAmount(json['targetAmount']),
        initialAmount: moneyAmount(json['initialAmount']),
        targetDate: parseDate(json['targetDate']),
        currentAmount: moneyAmount(json['currentAmount']),
        remaining: moneyAmount(json['remaining']),
        requiredPerPeriod: moneyAmount(json['requiredPerPeriod']),
        remainingPeriods: json['remainingPeriods'] as int,
        percentComplete: (json['percentComplete'] as num).toDouble(),
        status: GoalStatus.values.byName((json['status'] as String).toLowerCase()),
      );

  final String id;
  final String name;
  final double targetAmount;
  final double initialAmount;
  final DateTime targetDate;

  // Everything below is calculated by the API (BR-G3..G9), never by the app,
  // so the phone and the server can't disagree.
  final double currentAmount;
  final double remaining;
  final double requiredPerPeriod;
  final int remainingPeriods;
  final double percentComplete;
  final GoalStatus status;

  bool get canReceiveSavings => status == GoalStatus.active || status == GoalStatus.overdue;
}
