import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/money_text.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../../categories/data/category.dart';
import '../../categories/presentation/category_chip.dart';
import '../../categories/presentation/category_label.dart';
import '../../transactions/data/transaction.dart';
import '../application/banking_actions.dart';
import '../data/banking.dart';
import 'widgets/bank_labels.dart';
import 'widgets/transaction_badges.dart';

// UC-91 and UC-92: a bank transaction. Amount and date come from the bank (BR-X4);
// the user picks the category, adds a note, or leaves it out of the budget.
class BankTransactionScreen extends ConsumerStatefulWidget {
  const BankTransactionScreen({super.key, required this.transaction});

  final Transaction transaction;

  @override
  ConsumerState<BankTransactionScreen> createState() => _BankTransactionScreenState();
}

class _BankTransactionScreenState extends ConsumerState<BankTransactionScreen> {
  late String _categoryId = widget.transaction.categoryId;
  late bool _counted = !widget.transaction.isExcluded;
  late final _note = TextEditingController(text: widget.transaction.note ?? '');
  bool _remember = false;
  bool _saving = false;
  String? _error;

  Transaction get _original => widget.transaction;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final actions = ref.read(bankingActionsProvider);
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      if (_categoryId != _original.categoryId || _remember) {
        await actions.changeCategory(_original.id, _categoryId, rememberMerchant: _remember);
      }
      if (_counted == _original.isExcluded) {
        await actions.setExcluded(_original.id, !_counted);
      }
      final note = _note.text.trim();
      if (note != (_original.note ?? '')) {
        await actions.setNote(_original.id, note.isEmpty ? null : note);
      }

      messenger.showSnackBar(SnackBar(content: Text(l10n.expenseSaved)));
      if (mounted) context.pop();
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, l10n));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final categories = ref.watch(categoriesProvider).value ?? const <Category>[];
    final transaction = _original;
    final canCount = transaction.treatment == Treatment.spending || transaction.treatment == Treatment.refund;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.bankTransaction)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
        children: [
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(color: colors.fieldFill, borderRadius: BorderRadius.circular(20)),
            child: Column(children: [
              MoneyText(transaction.amount, style: theme.textTheme.displaySmall?.copyWith(fontWeight: FontWeight.w700)),
              const SizedBox(height: 4),
              Text(
                [transaction.isCashWithdrawal ? l10n.cashWithdrawal : transaction.merchant, context.longDate(transaction.date)]
                    .whereType<String>()
                    .join(' · '),
                textAlign: TextAlign.center,
                style: TextStyle(color: colors.textMuted),
              ),
              const SizedBox(height: 10),
              TransactionBadges(transaction: transaction, centered: true),
              if (transaction.fee case final fee?) ...[
                const SizedBox(height: 8),
                Text(l10n.bankFee(context.money(fee)), style: TextStyle(color: colors.textMuted, fontSize: 12)),
              ],
            ]),
          ),
          const SizedBox(height: 12),
          if (transaction.treatment == Treatment.outsideBudget)
            _OutsideBudgetNotice(transaction: transaction)
          else
            Row(children: [
              Icon(Icons.info_outline_rounded, size: 16, color: colors.textMuted),
              const SizedBox(width: 6),
              Expanded(child: Text(l10n.bankTransactionNote, style: TextStyle(color: colors.textMuted, fontSize: 12))),
            ]),
          FieldLabel(l10n.category),
          if (transaction.categoryIsAutomatic && _categoryId == transaction.categoryId)
            Padding(
              padding: const EdgeInsetsDirectional.only(start: 4, bottom: 8),
              child: Text(l10n.autoCategorized, style: TextStyle(color: colors.mintOnLight, fontSize: 12)),
            ),
          Wrap(spacing: 8, runSpacing: 8, children: [
            for (final category in categories)
              CategoryChip(
                key: Key('categoryChip-${category.id}'),
                category: category,
                label: categoryLabel(category, l10n),
                selected: category.id == _categoryId,
                onTap: transaction.isGoalContribution ? null : () => setState(() => _categoryId = category.id),
              ),
          ]),
          if (transaction.merchant case final merchant? when !transaction.isCashWithdrawal)
            SwitchListTile(
              key: const Key('rememberMerchant'),
              contentPadding: EdgeInsets.zero,
              value: _remember,
              onChanged: (value) => setState(() => _remember = value),
              title: Text(l10n.rememberMerchant(merchant)),
            ),
          if (canCount)
            SwitchListTile(
              key: const Key('countInBudget'),
              contentPadding: EdgeInsets.zero,
              value: _counted,
              onChanged: (value) => setState(() => _counted = value),
              title: Text(l10n.countInBudget),
            ),
          const SizedBox(height: 8),
          TextField(
            key: const Key('noteField'),
            controller: _note,
            maxLength: 200,
            decoration: InputDecoration(labelText: l10n.noteLabel, prefixIcon: const Icon(Icons.edit_note_rounded)),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Text(_error!, style: TextStyle(color: colors.danger)),
            ),
          FilledButton(
            key: const Key('saveBankTransaction'),
            onPressed: _saving ? null : _save,
            child: _saving
                ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2))
                : Text(l10n.save),
          ),
        ],
      ),
    );
  }
}

// OB-14: a transaction from an account outside the budget never counts, so the screen says why
// and offers to bring the whole account in (which re-counts what it already brought in).
class _OutsideBudgetNotice extends ConsumerStatefulWidget {
  const _OutsideBudgetNotice({required this.transaction});

  final Transaction transaction;

  @override
  ConsumerState<_OutsideBudgetNotice> createState() => _OutsideBudgetNoticeState();
}

class _OutsideBudgetNoticeState extends ConsumerState<_OutsideBudgetNotice> {
  bool _busy = false;

  Future<void> _include(String accountId) async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _busy = true);
    try {
      await ref.read(bankingActionsProvider).setAccountIncluded(accountId, true);
      messenger.showSnackBar(SnackBar(content: Text(l10n.accountIncluded)));
      if (mounted) context.pop();
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(error, l10n))));
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final accounts = ref.watch(bankConnectionsProvider).value?.expand((c) => c.accounts) ?? const <BankAccount>[];
    final account = accounts.where((a) => a.id == widget.transaction.bankAccountId).firstOrNull;
    final name = account == null ? l10n.accountOther : '${account.nickname} · ${accountKindLabel(account.kind, l10n)}';

    return Container(
      key: const Key('outsideBudgetNotice'),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(color: colors.warning.withValues(alpha: 0.10), borderRadius: BorderRadius.circular(16)),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Icon(Icons.remove_circle_outline_rounded, size: 18, color: colors.warning),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              account != null && !account.isSar ? l10n.otherCurrencyNote : l10n.outsideBudgetExplain(name),
              style: const TextStyle(height: 1.4),
            ),
          ),
        ]),
        if (account != null && account.isSar) ...[
          const SizedBox(height: 10),
          FilledButton.tonalIcon(
            key: const Key('includeAccount'),
            onPressed: _busy ? null : () => _include(account.id),
            icon: const Icon(Icons.add_task_rounded),
            label: Text(l10n.includeAccount),
          ),
        ],
      ]),
    );
  }
}
