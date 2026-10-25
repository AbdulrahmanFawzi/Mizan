import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

// Shows an amount with a smaller, quieter currency next to it: "7,060 ر.س".
// Tabular figures keep every digit the same width, so numbers don't jump while animating.
class MoneyText extends StatelessWidget {
  const MoneyText(this.amount, {super.key, this.style, this.currencyColor, this.decimals});

  final double amount;
  final TextStyle? style;
  final Color? currencyColor;
  final int? decimals;

  @override
  Widget build(BuildContext context) {
    final locale = Localizations.localeOf(context).toString();
    final base = (style ?? DefaultTextStyle.of(context).style)
        .copyWith(fontFeatures: const [FontFeature.tabularFigures()]);
    final digits = decimals ?? (amount == amount.roundToDouble() ? 0 : 2);
    final number = NumberFormat.decimalPatternDigits(locale: locale, decimalDigits: digits).format(amount);
    final symbol = locale.startsWith('ar') ? 'ر.س' : 'SAR';

    return Text.rich(
      TextSpan(children: [
        TextSpan(text: number),
        TextSpan(
          text: ' $symbol',
          style: TextStyle(
            fontSize: (base.fontSize ?? 14) * 0.5 < 11 ? 11 : (base.fontSize ?? 14) * 0.5,
            fontWeight: FontWeight.w500,
            color: currencyColor ?? base.color?.withValues(alpha: 0.6),
          ),
        ),
      ]),
      style: base,
      maxLines: 1,
    );
  }
}

// Counts up to the amount when it first appears or changes. Small touch, big "alive" feeling.
class AnimatedMoney extends StatelessWidget {
  const AnimatedMoney(this.amount, {super.key, this.style, this.currencyColor});

  final double amount;
  final TextStyle? style;
  final Color? currencyColor;

  @override
  Widget build(BuildContext context) => TweenAnimationBuilder<double>(
        tween: Tween(begin: 0, end: amount),
        duration: const Duration(milliseconds: 900),
        curve: Curves.easeOutCubic,
        builder: (_, value, _) => MoneyText(value.roundToDouble(), style: style, currencyColor: currencyColor, decimals: 0),
      );
}
