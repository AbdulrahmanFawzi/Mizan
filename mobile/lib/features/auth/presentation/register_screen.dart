import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/widgets/validators.dart';
import '../../../l10n/app_localizations.dart';
import '../application/auth_controller.dart';
import '../data/auth_repository.dart';
import 'auth_scaffold.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _income = TextEditingController();
  int _periodStartDay = 27; // Government salaries arrive on the 27th.
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    for (final controller in [_name, _email, _password, _income]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _submitting = true;
      _error = null;
    });

    try {
      await ref.read(authControllerProvider.notifier).register(RegisterRequest(
            name: _name.text.trim(),
            email: _email.text.trim(),
            password: _password.text,
            monthlyIncome: double.parse(_income.text.trim()),
            periodStartDay: _periodStartDay,
          ));
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, AppLocalizations.of(context)));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return AuthScaffold(title: l10n.register, children: [
      Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              controller: _name,
              decoration: InputDecoration(labelText: l10n.name),
              textInputAction: TextInputAction.next,
              validator: (v) => Validators.required(v, l10n),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _email,
              decoration: InputDecoration(labelText: l10n.email),
              keyboardType: TextInputType.emailAddress,
              textDirection: TextDirection.ltr,
              textInputAction: TextInputAction.next,
              validator: (v) => Validators.email(v, l10n),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _password,
              decoration: InputDecoration(labelText: l10n.password),
              obscureText: true,
              textInputAction: TextInputAction.next,
              validator: (v) => Validators.password(v, l10n),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _income,
              decoration: InputDecoration(labelText: l10n.monthlyIncome),
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              validator: (v) => Validators.positiveAmount(v, l10n),
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<int>(
              initialValue: _periodStartDay,
              decoration: InputDecoration(labelText: l10n.periodStartDay, helperText: l10n.periodStartDayHint),
              items: [for (var day = 1; day <= 31; day++) DropdownMenuItem(value: day, child: Text('$day'))],
              onChanged: (day) => setState(() => _periodStartDay = day!),
            ),
            const SizedBox(height: 24),
            FormErrorText(_error),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting
                  ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2))
                  : Text(l10n.register),
            ),
            const SizedBox(height: 12),
            TextButton(
              onPressed: () => context.go(Routes.login),
              child: Text(l10n.haveAccount),
            ),
          ],
        ),
      ),
    ]);
  }
}
