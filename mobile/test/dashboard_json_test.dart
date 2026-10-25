import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/dashboard/data/dashboard.dart';
import 'package:mizan/features/goals/data/goal.dart';

void main() {
  test('Dashboard.fromJson reads the API response', () {
    final dashboard = Dashboard.fromJson({
      'period': {'start': '2026-10-01', 'end': '2026-10-31', 'lengthInDays': 31, 'daysElapsed': 22},
      'monthlyIncome': {'amount': 10000.00, 'currency': 'SAR'},
      'totalSpent': {'amount': 460, 'currency': 'SAR'},
      'available': {'amount': 9540.00, 'currency': 'SAR'},
      'safeToSpendToday': {'amount': 1060, 'currency': 'SAR'},
      'daysLeft': 9,
      'budgetUsagePercent': 92,
      'topSpending': [
        {'categoryId': 'c1', 'name': 'الوقود', 'spent': {'amount': 460.00, 'currency': 'SAR'}},
      ],
      'goals': [
        {
          'id': 'g1',
          'name': 'Travel',
          'targetAmount': {'amount': 10000, 'currency': 'SAR'},
          'initialAmount': {'amount': 3500, 'currency': 'SAR'},
          'targetDate': '2027-12-31',
          'currentAmount': {'amount': 3500, 'currency': 'SAR'},
          'remaining': {'amount': 6500, 'currency': 'SAR'},
          'requiredPerPeriod': {'amount': 407, 'currency': 'SAR'},
          'remainingPeriods': 16,
          'percentComplete': 35.00,
          'status': 'Active',
        },
      ],
      'insights': [
        {'type': 'ProjectedOverBudget', 'categoryId': 'c1', 'categoryName': 'الوقود', 'message': 'msg'},
      ],
      'unreadNotifications': 2,
    });

    expect(dashboard.period.daysLeft, 9);
    expect(dashboard.available, 9540);
    expect(dashboard.safeToSpendToday, 1060);
    expect(dashboard.daysLeft, 9);
    expect(dashboard.budgetUsagePercent, 92);
    expect(dashboard.topSpending.single.name, 'الوقود');
    expect(dashboard.goals.single.status, GoalStatus.active);
    expect(dashboard.goals.single.requiredPerPeriod, 407);
    expect(dashboard.insights.single.type, InsightType.projectedOverBudget);
  });

  test('budgetUsagePercent is null for a new user without limits', () {
    final dashboard = Dashboard.fromJson({
      'period': {'start': '2026-10-01', 'end': '2026-10-31', 'lengthInDays': 31, 'daysElapsed': 22},
      'monthlyIncome': {'amount': 10000.00, 'currency': 'SAR'},
      'totalSpent': {'amount': 0, 'currency': 'SAR'},
      'available': {'amount': 10000.00, 'currency': 'SAR'},
      'safeToSpendToday': null,
      'daysLeft': 9,
      'budgetUsagePercent': null,
      'topSpending': <Object>[],
      'goals': <Object>[],
      'insights': <Object>[],
      'unreadNotifications': 0,
    });

    expect(dashboard.budgetUsagePercent, isNull);
    expect(dashboard.goals, isEmpty);
  });
}
