import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../categories/data/category.dart';
import '../../../categories/presentation/category_label.dart';
import '../../data/budget.dart';

// One category: planned vs actual (UC-51).
class CategoryBudgetCard extends StatelessWidget {
  const CategoryBudgetCard({super.key, required this.item, required this.onTap});

  final CategoryBudget item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final isSavings = item.kind == CategoryKind.savings;

    // Going over the savings plan is good news (BR-A4), so it is never shown in red.
    final warn = item.isOverBudget && !isSavings;
    final barColor = isSavings ? Colors.green : (warn ? colors.error : colors.primary);
    final progress = item.limit == 0 ? (item.spent > 0 ? 1.0 : 0.0) : (item.spent / item.limit).clamp(0.0, 1.0);

    final String status;
    if (item.limit == 0 && item.spent == 0) {
      status = l10n.noLimitSet;
    } else if (isSavings) {
      status = l10n.savedOfTarget(context.money(item.spent), context.money(item.limit));
    } else {
      status = l10n.spentOfLimit(context.money(item.spent), context.money(item.limit));
    }

    return Card.outlined(
      child: InkWell(
        key: Key('categoryCard-${item.categoryId}'),
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(children: [
                if (isSavings) const Padding(padding: EdgeInsetsDirectional.only(end: 8), child: Icon(Icons.savings_outlined, size: 20)),
                Expanded(child: Text(categoryLabel(item.category, l10n), style: theme.textTheme.titleMedium)),
                if (item.percentUsed != null)
                  Text('${item.percentUsed!.toStringAsFixed(0)}%',
                      style: theme.textTheme.titleSmall?.copyWith(color: warn ? colors.error : null)),
              ]),
              const SizedBox(height: 8),
              ClipRRect(
                borderRadius: BorderRadius.circular(6),
                child: LinearProgressIndicator(value: progress, minHeight: 8, color: barColor),
              ),
              const SizedBox(height: 8),
              Row(children: [
                Expanded(child: Text(status, style: theme.textTheme.bodySmall)),
                if (!isSavings && item.limit > 0)
                  Text(
                    warn ? l10n.overBy(context.money(-item.remaining)) : l10n.remainingAmount(context.money(item.remaining)),
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: warn ? colors.error : null,
                      fontWeight: warn ? FontWeight.bold : null,
                    ),
                  ),
              ]),
            ],
          ),
        ),
      ),
    );
  }
}
