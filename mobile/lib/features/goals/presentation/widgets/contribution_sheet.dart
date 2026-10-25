import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/format/formatters.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/amount_field.dart';
import '../../../../core/widgets/ui.dart';
import '../../../../core/widgets/validators.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/auth_scaffold.dart' show FormErrorText;
import '../../application/goal_actions.dart';
import '../../data/goal.dart';

Future<void> showContributionSheet(BuildContext context, Goal goal) => showModalBottomSheet<void>(
      context: context,
      useRootNavigator: true,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => ContributionSheet(goal: goal),
    );

// UC-42: put money toward a goal.
class ContributionSheet extends ConsumerStatefulWidget {
  const ContributionSheet({super.key, required this.goal});

  final Goal goal;

  @override
  ConsumerState<ContributionSheet> createState() => _ContributionSheetState();
}

class _ContributionSheetState extends ConsumerState<ContributionSheet> {
  final _formKey = GlobalKey<FormState>();
  final _amount = TextEditingController();
  DateTime _date = DateUtils.dateOnly(DateTime.now());
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _amount.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(2020),
      lastDate: DateUtils.dateOnly(DateTime.now()), // a saving can't be in the future (BR-T3)
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final goal = await ref
          .read(goalActionsProvider)
          .contribute(widget.goal.id, amount: double.parse(_amount.text.trim()), date: _date);
      final justCompleted = goal.status == GoalStatus.completed && widget.goal.status != GoalStatus.completed;
      messenger.showSnackBar(SnackBar(content: Text(justCompleted ? l10n.goalReached : l10n.savingAdded)));
      if (mounted) Navigator.pop(context);
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, l10n));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);

    return Padding(
      padding: EdgeInsets.fromLTRB(20, 0, 20, 20 + MediaQuery.viewInsetsOf(context).bottom),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(children: [
              IconTile(icon: Icons.savings_rounded, color: colors.mintOnLight, size: 48),
              const SizedBox(width: 12),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(l10n.addSaving, style: theme.textTheme.titleLarge),
                  Text(widget.goal.name, style: TextStyle(color: colors.textMuted)),
                ]),
              ),
            ]),
            const SizedBox(height: 20),
            AmountField(
              key: const Key('contributionAmountField'),
              controller: _amount,
              autofocus: true,
              compact: true,
              label: l10n.amount,
              helper: '${l10n.remainingLabel}: ${context.money(widget.goal.remaining)}',
              validator: (v) => Validators.positiveAmount(v, l10n),
            ),
            const SizedBox(height: 12),
            PickerTile(icon: Icons.calendar_today_rounded, label: l10n.date, value: context.longDate(_date), onTap: _pickDate),
            const SizedBox(height: 12),
            Row(children: [
              Icon(Icons.info_outline_rounded, size: 16, color: colors.textMuted),
              const SizedBox(width: 6),
              Expanded(child: Text(l10n.addSavingExplain, style: TextStyle(color: colors.textMuted, fontSize: 12))),
            ]),
            const SizedBox(height: 20),
            FormErrorText(_error),
            FilledButton(
              key: const Key('saveContribution'),
              onPressed: _busy ? null : _save,
              child: Text(l10n.save),
            ),
          ],
        ),
      ),
    );
  }
}
