import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart' show DateFormat;

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/money_text.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../application/banking_actions.dart';
import '../data/banking.dart';
import 'widgets/bank_labels.dart';

// UC-72 to UC-74: the linked banks, their accounts, and what counts in the budget.
class BankConnectionsScreen extends ConsumerWidget {
  const BankConnectionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final connections = ref.watch(bankConnectionsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.banks)),
      body: switch (connections) {
        AsyncData(:final value) => RefreshIndicator(
            onRefresh: () => ref.refresh(bankConnectionsProvider.future),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
              children: [
                if (value.isEmpty)
                  const LinkBankIntro()
                else ...[
                  for (final connection in value)
                    Padding(padding: const EdgeInsets.only(bottom: 16), child: _ConnectionCard(connection: connection)),
                  OutlinedButton.icon(
                    key: const Key('linkAnotherBank'),
                    onPressed: () => context.push(Routes.linkBank),
                    icon: const Icon(Icons.add_rounded),
                    label: Text(l10n.linkBank),
                  ),
                ],
              ],
            ),
          ),
        AsyncError(:final error) => Center(
            child: EmptyState(
              icon: Icons.cloud_off_rounded,
              message: errorMessage(error, l10n),
              action: OutlinedButton(onPressed: () => ref.invalidate(bankConnectionsProvider), child: Text(l10n.retry)),
            ),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

// Shown before the first link: what linking does, and that the money can't be moved (SAMA asks
// apps to explain what is shared before the consent screen).
class LinkBankIntro extends StatelessWidget {
  const LinkBankIntro({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        gradient: LinearGradient(
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
          colors: [colors.ink, colors.inkSoft],
        ),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        IconTile(icon: Icons.account_balance_rounded, color: colors.mint, size: 52),
        const SizedBox(height: 16),
        Text(l10n.linkBankTitle, style: theme.textTheme.titleLarge?.copyWith(color: colors.onInk)),
        const SizedBox(height: 8),
        Text(l10n.linkBankExplain, style: TextStyle(color: colors.onInkMuted, height: 1.5)),
        const SizedBox(height: 14),
        Row(children: [
          Icon(Icons.verified_user_rounded, size: 18, color: colors.mint),
          const SizedBox(width: 6),
          Expanded(child: Text(l10n.linkBankSecure, style: TextStyle(color: colors.onInkMuted, fontSize: 12))),
        ]),
        const SizedBox(height: 20),
        FilledButton.icon(
          key: const Key('linkBank'),
          style: FilledButton.styleFrom(backgroundColor: colors.mint, foregroundColor: colors.ink),
          onPressed: () => context.push(Routes.linkBank),
          icon: const Icon(Icons.link_rounded),
          label: Text(l10n.linkBank),
        ),
      ]),
    );
  }
}

class _ConnectionCard extends ConsumerWidget {
  const _ConnectionCard({required this.connection});

  final BankConnection connection;

  Future<void> _run(BuildContext context, Future<void> Function() action, {String? done}) async {
    final messenger = ScaffoldMessenger.of(context);
    final l10n = AppLocalizations.of(context);
    try {
      await action();
      if (done != null) messenger.showSnackBar(SnackBar(content: Text(done)));
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(error, l10n))));
    }
  }

  Future<void> _remove(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;

    // BR-C9: the user decides what happens to what this bank brought in.
    final keep = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.removeBankQuestion(connection.bankName)),
        content: Text(l10n.removeBankExplain),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: Text(l10n.cancel)),
          TextButton(
            key: const Key('deleteBankTransactions'),
            style: TextButton.styleFrom(foregroundColor: colors.danger),
            onPressed: () => Navigator.pop(context, false),
            child: Text(l10n.deleteTransactions),
          ),
          FilledButton(
            key: const Key('keepBankTransactions'),
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.keepTransactions),
          ),
        ],
      ),
    );
    if (keep == null || !context.mounted) return;

    await _run(
      context,
      () => ref.read(bankingActionsProvider).remove(connection.id, keepTransactions: keep),
      done: l10n.bankRemoved,
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final locale = Localizations.localeOf(context).toString();
    final synced = connection.lastSyncedAt;

    final String status;
    final Color statusColor;
    if (!connection.isActive) {
      status = l10n.consentEnded;
      statusColor = colors.danger;
    } else if (connection.expiresSoon) {
      status = l10n.consentDaysLeft(connection.consentDaysLeft);
      statusColor = colors.warning;
    } else {
      status = synced == null ? l10n.notSyncedYet : l10n.lastSynced(DateFormat.MMMd(locale).add_jm().format(synced));
      statusColor = colors.textMuted;
    }

    return Card(
      key: Key('connection-${connection.id}'),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Row(children: [
            IconTile(icon: Icons.account_balance_rounded, color: colors.mintOnLight),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(connection.bankName, style: theme.textTheme.titleMedium),
                Text(status, style: TextStyle(color: statusColor, fontSize: 13)),
              ]),
            ),
            if (connection.isActive)
              IconButton(
                key: Key('sync-${connection.id}'),
                tooltip: l10n.syncNow,
                onPressed: () => _run(context, () => ref.read(bankingActionsProvider).sync(connection.id)),
                icon: const Icon(Icons.sync_rounded),
              ),
          ]),
          const SizedBox(height: 8),
          for (final account in connection.accounts) _AccountRow(account: account),
          const SizedBox(height: 8),
          Row(children: [
            if (!connection.isActive || connection.expiresSoon)
              Expanded(
                child: FilledButton.tonal(
                  onPressed: () => context.push(Routes.linkBank),
                  child: Text(l10n.relink),
                ),
              )
            else
              const Spacer(),
            TextButton.icon(
              key: Key('remove-${connection.id}'),
              style: TextButton.styleFrom(foregroundColor: colors.danger),
              onPressed: () => _remove(context, ref),
              icon: const Icon(Icons.link_off_rounded, size: 18),
              label: Text(l10n.removeBank),
            ),
          ]),
        ]),
      ),
    );
  }
}

class _AccountRow extends ConsumerWidget {
  const _AccountRow({required this.account});

  final BankAccount account;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final subtitle = [
      accountKindLabel(account.kind, l10n),
      ?account.ibanMasked,
      if (!account.isSar) account.currency,
    ].join(' · ');

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(children: [
        Icon(accountKindIcon(account.kind), size: 22, color: colors.textMuted),
        const SizedBox(width: 12),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(account.nickname, style: Theme.of(context).textTheme.titleSmall, maxLines: 1, overflow: TextOverflow.ellipsis),
            Text(subtitle, style: TextStyle(color: colors.textMuted, fontSize: 12), textDirection: TextDirection.ltr),
            if (account.balance != null && account.isSar)
              MoneyText(account.balance!, style: TextStyle(color: colors.textMuted, fontSize: 12)),
          ]),
        ),
        Tooltip(
          message: account.isSar ? l10n.countsInBudget : l10n.otherCurrencyNote,
          child: Switch(
            key: Key('include-${account.id}'),
            value: account.includeInBudget,
            onChanged: account.isSar
                ? (value) => ref.read(bankingActionsProvider).setAccountIncluded(account.id, value)
                : null,
          ),
        ),
      ]),
    );
  }
}
