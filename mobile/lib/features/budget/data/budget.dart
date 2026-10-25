import '../../../core/format/json.dart';
import '../../categories/data/category.dart';
import '../../dashboard/data/dashboard.dart' show BudgetPeriod;

class CategoryBudget {
  const CategoryBudget({
    required this.categoryId,
    required this.name,
    required this.kind,
    required this.limit,
    required this.spent,
    required this.remaining,
    required this.percentUsed,
    required this.isOverBudget,
  });

  factory CategoryBudget.fromJson(Map<String, dynamic> json) => CategoryBudget(
        categoryId: json['categoryId'] as String,
        name: json['name'] as String,
        kind: CategoryKind.values.byName((json['kind'] as String).toLowerCase()),
        limit: moneyAmount(json['limit']),
        spent: moneyAmount(json['spent']),
        remaining: moneyAmount(json['remaining']),
        percentUsed: (json['percentUsed'] as num?)?.toDouble(),
        isOverBudget: json['isOverBudget'] as bool,
      );

  final String categoryId;
  final String name;
  final CategoryKind kind;
  final double limit;
  final double spent;
  final double remaining;

  // null when the limit is zero (a percentage of zero has no meaning).
  final double? percentUsed;
  final bool isOverBudget;

  Category get category => Category(id: categoryId, name: name, kind: kind);
}

class Budget {
  const Budget({
    required this.period,
    required this.plannedIncome,
    required this.totalAllocated,
    required this.unallocated,
    required this.isOverAllocated,
    required this.totalSpent,
    required this.categories,
  });

  factory Budget.fromJson(Map<String, dynamic> json) => Budget(
        period: BudgetPeriod.fromJson(json['period'] as Map<String, dynamic>),
        plannedIncome: moneyAmount(json['plannedIncome']),
        totalAllocated: moneyAmount(json['totalAllocated']),
        unallocated: moneyAmount(json['unallocated']),
        isOverAllocated: json['isOverAllocated'] as bool,
        totalSpent: moneyAmount(json['totalSpent']),
        categories: listOf(json['categories'], CategoryBudget.fromJson),
      );

  final BudgetPeriod period;
  final double plannedIncome;
  final double totalAllocated;
  final double unallocated;
  final bool isOverAllocated;
  final double totalSpent;
  final List<CategoryBudget> categories;
}
