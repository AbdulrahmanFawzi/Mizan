import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/transaction.dart';

class TransactionTile extends StatelessWidget {
  const TransactionTile({super.key, required this.transaction, required this.categoryName, required this.onTap});

  final Transaction transaction;
  final String categoryName;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final merchant = transaction.merchant;

    return ListTile(
      onTap: onTap,
      leading: CircleAvatar(
        child: Icon(transaction.isGoalContribution ? Icons.savings_outlined : Icons.receipt_outlined),
      ),
      title: Text(categoryName),
      subtitle: transaction.isGoalContribution
          ? Text(l10n.goalContribution)
          : (merchant == null ? null : Text(merchant)),
      trailing: Text(context.money(transaction.amount), style: Theme.of(context).textTheme.titleSmall),
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
      padding: const EdgeInsetsDirectional.fromSTEB(16, 16, 16, 4),
      child: Text(label, style: Theme.of(context).textTheme.labelLarge),
    );
  }
}
