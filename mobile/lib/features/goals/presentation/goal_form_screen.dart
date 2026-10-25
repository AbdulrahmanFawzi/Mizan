import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/widgets/amount_field.dart';
import '../../../core/widgets/ui.dart';
import '../../../core/widgets/validators.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/presentation/auth_scaffold.dart' show FormErrorText;
import '../application/goal_actions.dart';
import '../data/goal.dart';

// UC-40 (create) and UC-41 (edit). "Already saved" is only asked when creating:
// afterwards the amount grows through savings, so the history stays correct.
class GoalFormScreen extends ConsumerStatefulWidget {
  const GoalFormScreen({super.key, this.existing});

  final Goal? existing;

  @override
  ConsumerState<GoalFormScreen> createState() => _GoalFormScreenState();
}

class _GoalFormScreenState extends ConsumerState<GoalFormScreen> {
  final _formKey = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.existing?.name ?? '');
  late final _target = TextEditingController(text: _format(widget.existing?.targetAmount));
  final _initial = TextEditingController(text: '0');
  late DateTime _targetDate = widget.existing?.targetDate ?? _tomorrow.add(const Duration(days: 365));
  bool _busy = false;
  String? _error;

  bool get _isEdit => widget.existing != null;

  // BR-G2: the target date must be in the future.
  static DateTime get _tomorrow => DateUtils.dateOnly(DateTime.now()).add(const Duration(days: 1));

  static String _format(double? value) {
    if (value == null) return '';
    return value == value.roundToDouble() ? value.toStringAsFixed(0) : value.toString();
  }

  @override
  void dispose() {
    _name.dispose();
    _target.dispose();
    _initial.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _targetDate.isBefore(_tomorrow) ? _tomorrow : _targetDate,
      firstDate: _tomorrow,
      lastDate: DateTime.now().add(const Duration(days: 365 * 30)),
    );
    if (picked != null) setState(() => _targetDate = picked);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_targetDate.isBefore(_tomorrow)) {
      // An overdue goal keeps its old date until the user picks a new one.
      setState(() => _error = AppLocalizations.of(context).extendDateHint);
      return;
    }

    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final actions = ref.read(goalActionsProvider);
    final name = _name.text.trim();
    final target = double.parse(_target.text.trim());

    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      if (_isEdit) {
        await actions.update(widget.existing!.id, name: name, targetAmount: target, targetDate: _targetDate);
      } else {
        await actions.create(
          name: name,
          targetAmount: target,
          initialAmount: double.parse(_initial.text.trim()),
          targetDate: _targetDate,
        );
      }
      messenger.showSnackBar(SnackBar(content: Text(l10n.goalSaved)));
      if (mounted) context.pop();
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, l10n));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(_isEdit ? l10n.editGoal : l10n.addGoal)),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
          children: [
            TextFormField(
              key: const Key('goalNameField'),
              controller: _name,
              autofocus: !_isEdit,
              decoration: InputDecoration(
                labelText: l10n.goalName,
                hintText: l10n.goalNameHint,
                prefixIcon: const Icon(Icons.flag_rounded),
              ),
              maxLength: 100,
              validator: (v) => Validators.required(v, l10n),
            ),
            const SizedBox(height: 8),
            AmountField(
              key: const Key('goalTargetField'),
              controller: _target,
              label: l10n.targetAmount,
              validator: (v) => Validators.positiveAmount(v, l10n),
            ),
            if (!_isEdit) ...[
              const SizedBox(height: 12),
              AmountField(
                key: const Key('goalInitialField'),
                controller: _initial,
                compact: true,
                label: l10n.initialAmount,
                validator: (v) => Validators.nonNegativeAmount(v, l10n),
              ),
            ],
            const SizedBox(height: 12),
            PickerTile(
              icon: Icons.event_rounded,
              label: l10n.targetDate,
              value: context.longDate(_targetDate),
              onTap: _pickDate,
            ),
            const SizedBox(height: 24),
            FormErrorText(_error),
            FilledButton(
              key: const Key('saveGoal'),
              onPressed: _busy ? null : _save,
              child: Text(l10n.save),
            ),
          ],
        ),
      ),
    );
  }
}
