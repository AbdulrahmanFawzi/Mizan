import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/money_text.dart';
import '../../../../core/widgets/ui.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../categories/data/category.dart';
import '../../../categories/presentation/category_label.dart';
import '../../../banking/presentation/widgets/transaction_badges.dart';
import '../../../categories/presentation/category_visual.dart';
import '../../data/transaction.dart';

class TransactionTile extends StatelessWidget {
  const TransactionTile({super.key, required this.transaction, required this.category, required this.onTap});

  final Transaction transaction;

  // null while the categories are still loading.
  final Category? category;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final category = this.category;
    final visual = transaction.isGoalContribution
        ? categoryVisual('', CategoryKind.savings)
        : categoryVisual(category?.name ?? '', category?.kind ?? CategoryKind.other);
    final subtitle = transaction.isGoalContribution
        ? l10n.goalContribution
        : transaction.isCashWithdrawal
            ? l10n.cashWithdrawal
            : transaction.merchant;

    // Money in shows with a plus in mint; what doesn't count is quieter.
    final moneyIn = transaction.isCredit;
    final quiet = transaction.isFromBank && !transaction.countsInBudget && !moneyIn;
    final amountStyle = Theme.of(context).textTheme.titleMedium?.copyWith(
          color: moneyIn ? colors.mintOnLight : (quiet ? colors.textMuted : null),
          decoration: transaction.isExcluded ? TextDecoration.lineThrough : null,
        );
    final hasBadges = transaction.isFromBank || transaction.possibleDuplicateOf != null;

    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(children: [
          IconTile(icon: visual.icon, color: visual.color),
          const SizedBox(width: 12),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(category == null ? '' : categoryLabel(category, l10n), style: Theme.of(context).textTheme.titleSmall),
              if (subtitle != null)
                Text(subtitle, maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(color: colors.textMuted, fontSize: 13)),
              if (hasBadges) ...[const SizedBox(height: 4), TransactionBadges(transaction: transaction)],
            ]),
          ),
          const SizedBox(width: 8),
          Row(mainAxisSize: MainAxisSize.min, children: [
            if (moneyIn) Text('+', style: amountStyle),
            MoneyText(transaction.amount, style: amountStyle),
          ]),
        ]),
      ),
    );
  }
}

class DayHeader extends StatelessWidget {
  const DayHeader(this.date, {super.key});

  final DateTime date;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final today = DateUtils.dateOnly(DateTime.now());
    final label = switch (today.difference(date).inDays) {
      0 => l10n.today,
      1 => l10n.yesterday,
      _ => context.longDate(date),
    };

    return Padding(
      padding: const EdgeInsetsDirectional.only(start: 4, bottom: 8),
      child: Text(label, style: Theme.of(context).textTheme.labelLarge?.copyWith(color: context.mizan.textMuted)),
    );
  }
}
