import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../l10n/app_localizations.dart';
import 'validators.dart';

// The 6-digit code from an email. The phone can fill it from the email (oneTimeCode).
class CodeField extends StatelessWidget {
  const CodeField({super.key, required this.controller, this.onSubmitted});

  final TextEditingController controller;
  final VoidCallback? onSubmitted;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(labelText: l10n.code, counterText: ''),
      keyboardType: TextInputType.number,
      textDirection: TextDirection.ltr,
      textAlign: TextAlign.center,
      style: const TextStyle(fontSize: 22, letterSpacing: 8, fontWeight: FontWeight.w600),
      maxLength: 6,
      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
      autofillHints: const [AutofillHints.oneTimeCode],
      onFieldSubmitted: (_) => onSubmitted?.call(),
      validator: (v) => Validators.emailCode(v, l10n),
    );
  }
}
