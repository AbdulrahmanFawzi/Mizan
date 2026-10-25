import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/network/api_exception.dart';
import '../../core/widgets/amount_field.dart';
import '../../core/widgets/validators.dart';
import '../../l10n/app_localizations.dart';
import '../auth/presentation/auth_scaffold.dart' show FormErrorText;
import '../profile/data/profile_repository.dart';
import 'profile_actions.dart';

// UC-04 (name), UC-11 (income), and the salary day (BR-P4).
class EditProfileScreen extends ConsumerWidget {
  const EditProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final profile = ref.watch(profileProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.editProfile)),
      body: switch (profile) {
        AsyncData(:final value) => _ProfileForm(profile: value),
        AsyncError(:final error) => Center(child: Text(errorMessage(error, l10n))),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class _ProfileForm extends ConsumerStatefulWidget {
  const _ProfileForm({required this.profile});

  final Profile profile;

  @override
  ConsumerState<_ProfileForm> createState() => _ProfileFormState();
}

class _ProfileFormState extends ConsumerState<_ProfileForm> {
  final _formKey = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.profile.name);
  late final _income = TextEditingController(text: _format(widget.profile.monthlyIncome));
  late int _periodStartDay = widget.profile.periodStartDay;
  bool _busy = false;
  String? _error;

  static String _format(double value) => value == value.roundToDouble() ? value.toStringAsFixed(0) : value.toString();

  @override
  void dispose() {
    _name.dispose();
    _income.dispose();
    super.dispose();
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
      await ref.read(profileActionsProvider).update(
            name: _name.text.trim(),
            monthlyIncome: double.parse(_income.text.trim()),
            periodStartDay: _periodStartDay,
          );
      messenger.showSnackBar(SnackBar(content: Text(l10n.profileSaved)));
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

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
        children: [
          TextFormField(
            key: const Key('profileNameField'),
            controller: _name,
            decoration: InputDecoration(labelText: l10n.name, prefixIcon: const Icon(Icons.person_rounded)),
            validator: (v) => Validators.required(v, l10n),
          ),
          const SizedBox(height: 16),
          AmountField(
            key: const Key('profileIncomeField'),
            controller: _income,
            label: l10n.monthlyIncome,
            helper: l10n.incomeChangeHint,
            validator: (v) => Validators.positiveAmount(v, l10n),
          ),
          const SizedBox(height: 16),
          DropdownButtonFormField<int>(
            initialValue: _periodStartDay,
            borderRadius: BorderRadius.circular(16),
            menuMaxHeight: 320,
            decoration: InputDecoration(
              labelText: l10n.periodStartDay,
              helperText: l10n.salaryDayChangeHint,
              helperMaxLines: 2,
              prefixIcon: const Icon(Icons.event_repeat_rounded),
            ),
            items: [for (var day = 1; day <= 31; day++) DropdownMenuItem(value: day, child: Text('$day'))],
            onChanged: (day) => setState(() => _periodStartDay = day!),
          ),
          const SizedBox(height: 24),
          FormErrorText(_error),
          FilledButton(
            key: const Key('saveProfile'),
            onPressed: _busy ? null : _save,
            child: Text(l10n.save),
          ),
        ],
      ),
    );
  }
}
