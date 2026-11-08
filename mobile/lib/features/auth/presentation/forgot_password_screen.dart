import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/widgets/code_field.dart';
import '../../../core/widgets/validators.dart';
import '../../../l10n/app_localizations.dart';
import '../application/auth_controller.dart';
import '../data/auth_repository.dart';
import 'auth_scaffold.dart';

// UC-05 in two steps on one screen: the email, then the code with the new password.
// A successful reset logs the user in, and the router takes them home.
class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _code = TextEditingController();
  final _password = TextEditingController();
  bool _codeSent = false;
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _code.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final l10n = AppLocalizations.of(context);
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      if (_codeSent) {
        await ref.read(authControllerProvider.notifier).resetPassword(
              email: _email.text.trim(),
              code: _code.text.trim(),
              newPassword: _password.text,
            );
      } else {
        await ref.read(authRepositoryProvider).forgotPassword(_email.text.trim());
        if (mounted) setState(() => _codeSent = true);
      }
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, l10n));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return AuthScaffold(title: l10n.resetPassword, children: [
      Form(
        key: _formKey,
        child: AutofillGroup(
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            TextFormField(
              controller: _email,
              enabled: !_codeSent,
              decoration: InputDecoration(labelText: l10n.email),
              keyboardType: TextInputType.emailAddress,
              textDirection: TextDirection.ltr,
              autofillHints: const [AutofillHints.email],
              validator: (v) => Validators.email(v, l10n),
            ),
            if (_codeSent) ...[
              const SizedBox(height: 12),
              Text(l10n.resetCodeSent, style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 16),
              CodeField(controller: _code),
              const SizedBox(height: 16),
              TextFormField(
                key: const Key('newPassword'),
                controller: _password,
                decoration: InputDecoration(labelText: l10n.newPassword),
                obscureText: true,
                autofillHints: const [AutofillHints.newPassword],
                onFieldSubmitted: (_) => _submit(),
                validator: (v) => Validators.password(v, l10n),
              ),
            ],
            const SizedBox(height: 24),
            FormErrorText(_error),
            FilledButton(
              key: const Key('resetSubmit'),
              onPressed: _busy ? null : _submit,
              child: _busy
                  ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2))
                  : Text(_codeSent ? l10n.resetPassword : l10n.sendCode),
            ),
            const SizedBox(height: 12),
            TextButton(onPressed: () => context.go(Routes.login), child: Text(l10n.backToLogin)),
          ]),
        ),
      ),
    ]);
  }
}
