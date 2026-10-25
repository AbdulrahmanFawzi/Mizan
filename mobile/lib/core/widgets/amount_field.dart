import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/mizan_colors.dart';

// The amount is the most important input in a money app, so it gets a big, centered field
// instead of a small box. Digits only, with at most two decimals (halalas).
//
// It's a FormField around a plain TextField: the number field shrinks to fit its digits so the
// currency sits right next to it, and the error is shown full-width under both.
class AmountField extends FormField<String> {
  AmountField({
    super.key,
    required TextEditingController controller,
    required FormFieldValidator<String> validator,
    String? label,
    String? helper,
    bool autofocus = false,
    bool compact = false, // smaller version for sheets and secondary amounts
  }) : super(
          validator: (_) => validator(controller.text),
          builder: (state) => _AmountFieldBody(
            controller: controller,
            label: label,
            helper: helper,
            error: state.errorText,
            autofocus: autofocus,
            compact: compact,
          ),
        );
}

class _AmountFieldBody extends StatelessWidget {
  const _AmountFieldBody({
    required this.controller,
    required this.label,
    required this.helper,
    required this.error,
    required this.autofocus,
    required this.compact,
  });

  final TextEditingController controller;
  final String? label;
  final String? helper;
  final String? error;
  final bool autofocus;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final style = (compact ? theme.textTheme.headlineMedium : theme.textTheme.displaySmall)?.copyWith(
      fontWeight: FontWeight.w700,
      fontFeatures: const [FontFeature.tabularFigures()],
    );

    return Container(
      padding: EdgeInsets.fromLTRB(16, compact ? 12 : 20, 16, compact ? 12 : 16),
      decoration: BoxDecoration(
        color: colors.fieldFill,
        borderRadius: BorderRadius.circular(20),
        border: error == null ? null : Border.all(color: colors.danger),
      ),
      child: Column(children: [
        if (label != null) Text(label!, style: TextStyle(color: colors.textMuted)),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.baseline,
          textBaseline: TextBaseline.alphabetic,
          children: [
            Flexible(
              child: IntrinsicWidth(
                child: TextField(
                  controller: controller,
                  autofocus: autofocus,
                  textAlign: TextAlign.center,
                  style: style,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
                  decoration: InputDecoration(
                    hintText: '0',
                    hintStyle: style?.copyWith(color: colors.textMuted.withValues(alpha: 0.4)),
                    filled: false,
                    isCollapsed: true,
                    border: InputBorder.none,
                    enabledBorder: InputBorder.none,
                    focusedBorder: InputBorder.none,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 6),
            Text(
              isArabic ? 'ر.س' : 'SAR',
              style: TextStyle(color: colors.textMuted, fontSize: compact ? 14 : 18, fontWeight: FontWeight.w600),
            ),
          ],
        ),
        if (error != null) ...[
          const SizedBox(height: 6),
          Text(error!, textAlign: TextAlign.center, style: TextStyle(color: colors.danger, fontSize: 12)),
        ] else if (helper != null) ...[
          const SizedBox(height: 6),
          Text(helper!, textAlign: TextAlign.center, style: TextStyle(color: colors.textMuted, fontSize: 12)),
        ],
      ]),
    );
  }
}
