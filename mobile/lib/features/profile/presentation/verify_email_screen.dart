import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/code_field.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/presentation/auth_scaffold.dart';
import '../data/profile_repository.dart';

// UC-07 (BR-U5): the code was sent when the user signed up; they can ask for a new one here.
// Pops with true once the email is verified.
class VerifyEmailScreen extends ConsumerStatefulWidget {
  const VerifyEmailScreen({super.key});

  @override
  ConsumerState<VerifyEmailScreen> createState() => _VerifyEmailScreenState();
}

class _VerifyEmailScreenState extends ConsumerState<VerifyEmailScreen> {
  final _formKey = GlobalKey<FormState>();
  final _code = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _code.dispose();
    super.dispose();
  }

  Future<void> _verify() async {
    if (!_formKey.currentState!.validate()) return;
    final l10n = AppLocalizations.of(context);
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(profileRepositoryProvider).verifyEmail(_code.text.trim());
      ref.invalidate(profileProvider);
      if (mounted) context.pop(true);
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, l10n));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _resend() async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref.read(profileRepositoryProvider).sendVerificationCode();
      messenger.showSnackBar(SnackBar(content: Text(l10n.codeSent)));
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(error, l10n))));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final email = ref.watch(profileProvider).value?.email ?? '';

    return Scaffold(
      appBar: AppBar(title: Text(l10n.verifyEmail)),
      body: ListView(padding: const EdgeInsets.fromLTRB(24, 16, 24, 32), children: [
        IconTile(icon: Icons.mark_email_read_rounded, color: context.mizan.mintOnLight, size: 52),
        const SizedBox(height: 16),
        Text(l10n.verifyEmailExplain(email), style: const TextStyle(height: 1.5)),
        const SizedBox(height: 24),
        Form(key: _formKey, child: CodeField(controller: _code, onSubmitted: _verify)),
        const SizedBox(height: 16),
        FormErrorText(_error),
        FilledButton(
          key: const Key('verifyEmail'),
          onPressed: _busy ? null : _verify,
          child: Text(l10n.confirm),
        ),
        const SizedBox(height: 8),
        TextButton(key: const Key('resendCode'), onPressed: _resend, child: Text(l10n.resendCode)),
      ]),
    );
  }
}
