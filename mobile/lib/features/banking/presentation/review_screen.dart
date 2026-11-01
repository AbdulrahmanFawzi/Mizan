import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/money_text.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../../transactions/data/transaction.dart';
import '../application/banking_actions.dart';
import '../data/banking.dart';

// UC-92, UC-93 (OB-1, OB-3): bank transactions that need the user's decision.
class ReviewScreen extends ConsumerWidget {
  const ReviewScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final items = ref.watch(reviewItemsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.reviewTitle)),
      body: switch (items) {
        AsyncData(:final value) when value.isEmpty => Center(
            child: EmptyState(icon: Icons.task_alt_rounded, message: l10n.reviewEmpty),
          ),
        AsyncData(:final value) => RefreshIndicator(
            onRefresh: () => ref.refresh(reviewItemsProvider.future),
            child: ListView.separated(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
              itemCount: value.length,
              separatorBuilder: (_, _) => const SizedBox(height: 12),
              itemBuilder: (_, index) => ReviewCard(item: value[index]),
            ),
          ),
        AsyncError(:final error) => Center(
            child: EmptyState(
              icon: Icons.cloud_off_rounded,
              message: errorMessage(error, l10n),
              action: OutlinedButton(onPressed: () => ref.invalidate(reviewItemsProvider), child: Text(l10n.retry)),
            ),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class ReviewCard extends ConsumerStatefulWidget {
  const ReviewCard({super.key, required this.item});

  final ReviewItem item;

  @override
  ConsumerState<ReviewCard> createState() => _ReviewCardState();
}

class _ReviewCardState extends ConsumerState<ReviewCard> {
  bool _busy = false;
  bool _remember = false;

  Future<void> _decide(ReviewDecision decision) async {
    final messenger = ScaffoldMessenger.of(context);
    final l10n = AppLocalizations.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(bankingActionsProvider).resolve(widget.item.transaction.id, decision, remember: _remember);
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(error, l10n))));
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final item = widget.item;
    final isDuplicate = item.reason == ReviewReason.possibleDuplicate;

    return Card(
      key: Key('review-${item.transaction.id}'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Row(children: [
            IconTile(
              icon: isDuplicate ? Icons.content_copy_rounded : Icons.swap_horiz_rounded,
              color: colors.warning,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(isDuplicate ? l10n.duplicateQuestion : l10n.transferQuestion, style: theme.textTheme.titleSmall),
            ),
          ]),
          const SizedBox(height: 12),
          _Line(label: l10n.fromBank, transaction: item.transaction),
          if (item.candidate case final candidate?) _Line(label: l10n.youRecorded, transaction: candidate),
          const SizedBox(height: 6),
          Text(isDuplicate ? l10n.duplicateExplain : l10n.transferExplain, style: TextStyle(color: colors.textMuted, fontSize: 12)),
          // BR-R6: answer once for every transfer into this account.
          if (!isDuplicate && item.transferTo != null)
            CheckboxListTile(
              key: Key('remember-${item.transaction.id}'),
              value: _remember,
              onChanged: _busy ? null : (value) => setState(() => _remember = value ?? false),
              title: Text(l10n.rememberTransfer(item.transferTo!), style: const TextStyle(fontSize: 13)),
              controlAffinity: ListTileControlAffinity.leading,
              contentPadding: EdgeInsets.zero,
              dense: true,
            ),
          const SizedBox(height: 14),
          Row(children: [
            Expanded(
              child: OutlinedButton(
                key: Key('no-${item.transaction.id}'),
                onPressed: _busy ? null : () => _decide(isDuplicate ? ReviewDecision.notDuplicate : ReviewDecision.spending),
                child: Text(isDuplicate ? l10n.notDuplicate : l10n.itsSpending, textAlign: TextAlign.center),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: FilledButton(
                key: Key('yes-${item.transaction.id}'),
                onPressed: _busy ? null : () => _decide(isDuplicate ? ReviewDecision.sameAsManual : ReviewDecision.ownTransfer),
                child: Text(isDuplicate ? l10n.sameAsManual : l10n.ownTransfer, textAlign: TextAlign.center),
              ),
            ),
          ]),
        ]),
      ),
    );
  }
}

class _Line extends StatelessWidget {
  const _Line({required this.label, required this.transaction});

  final String label;
  final Transaction transaction;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(children: [
        SizedBox(width: 92, child: Text(label, style: TextStyle(color: colors.textMuted, fontSize: 12))),
        Expanded(
          child: Text(
            '${transaction.merchant ?? '—'} · ${context.shortDate(transaction.date)}',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
        MoneyText(transaction.amount, style: Theme.of(context).textTheme.titleSmall),
      ]),
    );
  }
}

// A card on the dashboard and the expense list when something waits for the user.
class ReviewBanner extends ConsumerWidget {
  const ReviewBanner({super.key, required this.onOpen});

  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final count = ref.watch(reviewItemsProvider).value?.length ?? 0;
    if (count == 0) return const SizedBox.shrink();

    final colors = context.mizan;
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Material(
        key: const Key('reviewBanner'),
        color: colors.warning.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(18),
        child: InkWell(
          borderRadius: BorderRadius.circular(18),
          onTap: onOpen,
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(children: [
              Icon(Icons.rule_rounded, color: colors.warning),
              const SizedBox(width: 10),
              Expanded(
                child: Text(AppLocalizations.of(context).reviewBanner(count),
                    style: Theme.of(context).textTheme.titleSmall),
              ),
              Icon(Icons.chevron_right_rounded, color: colors.textMuted),
            ]),
          ),
        ),
      ),
    );
  }
}
