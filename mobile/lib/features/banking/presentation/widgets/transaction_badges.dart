import 'package:flutter/material.dart';

import '../../../../core/theme/mizan_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../transactions/data/transaction.dart';

// Small labels that explain why a bank transaction counts (or doesn't): pending, refund, transfer…
class TransactionBadges extends StatelessWidget {
  const TransactionBadges({super.key, required this.transaction, this.centered = false});

  final Transaction transaction;
  final bool centered;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;

    final badges = <(String, Color, IconData)>[
      if (transaction.isFromBank) (l10n.fromBank, colors.mintOnLight, Icons.account_balance_rounded),
      if (transaction.isPending) (l10n.pendingLabel, colors.warning, Icons.schedule_rounded),
      if (transaction.needsReview || transaction.possibleDuplicateOf != null)
        (l10n.reviewTitle, colors.warning, Icons.rule_rounded),
      if (transaction.isExcluded) (l10n.excludedLabel, colors.textMuted, Icons.block_rounded),
      ...switch (transaction.treatment) {
        Treatment.refund => [(l10n.refundLabel, colors.mintOnLight, Icons.undo_rounded)],
        Treatment.ownTransfer => [(l10n.transferLabel, colors.textMuted, Icons.swap_horiz_rounded)],
        Treatment.incoming => [(l10n.incomingLabel, colors.mintOnLight, Icons.south_west_rounded)],
        Treatment.outsideBudget => [(l10n.outsideBudgetLabel, colors.textMuted, Icons.remove_circle_outline_rounded)],
        Treatment.spending => const <(String, Color, IconData)>[],
      },
    ];

    return Wrap(
      alignment: centered ? WrapAlignment.center : WrapAlignment.start,
      spacing: 6,
      runSpacing: 4,
      children: [
        for (final (label, color, icon) in badges)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(10)),
            child: Row(mainAxisSize: MainAxisSize.min, children: [
              Icon(icon, size: 12, color: color),
              const SizedBox(width: 4),
              Text(label, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w600)),
            ]),
          ),
      ],
    );
  }
}
