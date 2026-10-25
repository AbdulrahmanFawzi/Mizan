import 'package:flutter/material.dart';

import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/money_text.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/dashboard.dart';

// The first thing the user sees: not their balance, but what they can safely spend today (BR-N7).
class HeroCard extends StatelessWidget {
  const HeroCard({super.key, required this.dashboard});

  final Dashboard dashboard;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final safe = dashboard.safeToSpendToday ?? 0;
    final usage = dashboard.budgetUsagePercent;

    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        gradient: LinearGradient(
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
          colors: [colors.ink, colors.inkSoft],
        ),
      ),
      child: Stack(children: [
        // Soft mint glows for depth. Decorative only.
        PositionedDirectional(top: -60, end: -40, child: _Glow(size: 200, color: colors.mint.withValues(alpha: 0.18))),
        PositionedDirectional(bottom: -80, start: -30, child: _Glow(size: 180, color: colors.mint.withValues(alpha: 0.08))),
        Padding(
          padding: const EdgeInsets.all(22),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(l10n.safeToSpendToday, style: theme.textTheme.bodyMedium?.copyWith(color: colors.onInkMuted)),
                  const SizedBox(height: 6),
                  AnimatedMoney(
                    safe,
                    key: const Key('safeToSpend'),
                    style: theme.textTheme.displaySmall?.copyWith(
                      color: safe > 0 ? colors.mint : colors.danger,
                      fontWeight: FontWeight.w700,
                      height: 1.1,
                    ),
                    currencyColor: colors.onInkMuted,
                  ),
                  const SizedBox(height: 6),
                  Text(
                    // Same day count the API divided by (it includes today), so the text matches the number.
                    safe > 0 ? l10n.daysLeft(dashboard.daysLeft) : l10n.safeToSpendNone,
                    style: theme.textTheme.bodySmall?.copyWith(color: colors.onInkMuted),
                  ),
                ]),
              ),
              if (usage != null) _UsageRing(percent: usage),
            ]),
            const SizedBox(height: 20),
            Row(children: [
              Expanded(child: _Stat(label: l10n.income, amount: dashboard.monthlyIncome)),
              const SizedBox(width: 8),
              Expanded(child: _Stat(key: const Key('totalSpent'), label: l10n.spent, amount: dashboard.totalSpent)),
              const SizedBox(width: 8),
              Expanded(child: _Stat(key: const Key('availableAmount'), label: l10n.available, amount: dashboard.available)),
            ]),
          ]),
        ),
      ]),
    );
  }
}

class _Glow extends StatelessWidget {
  const _Glow({required this.size, required this.color});

  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(shape: BoxShape.circle, gradient: RadialGradient(colors: [color, color.withValues(alpha: 0)])),
      );
}

class _UsageRing extends StatelessWidget {
  const _UsageRing({required this.percent});

  final double percent;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    final over = percent > 100;

    return SizedBox.square(
      dimension: 76,
      child: Stack(fit: StackFit.expand, children: [
        TweenAnimationBuilder<double>(
          tween: Tween(begin: 0, end: (percent / 100).clamp(0, 1)),
          duration: const Duration(milliseconds: 900),
          curve: Curves.easeOutCubic,
          builder: (_, value, _) => CircularProgressIndicator(
            value: value,
            strokeWidth: 7,
            strokeCap: StrokeCap.round,
            color: over ? colors.danger : colors.mint,
            backgroundColor: colors.onInk.withValues(alpha: 0.1),
          ),
        ),
        Center(
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Text('${percent.toStringAsFixed(0)}%',
                style: TextStyle(color: colors.onInk, fontWeight: FontWeight.w700, fontSize: 17)),
            Text(AppLocalizations.of(context).budgetShort, style: TextStyle(color: colors.onInkMuted, fontSize: 9)),
          ]),
        ),
      ]),
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({super.key, required this.label, required this.amount});

  final String label;
  final double amount;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: colors.onInk.withValues(alpha: 0.07),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(label, style: TextStyle(color: colors.onInkMuted, fontSize: 11)),
        const SizedBox(height: 2),
        FittedBox(
          fit: BoxFit.scaleDown,
          alignment: AlignmentDirectional.centerStart,
          child: MoneyText(
            amount,
            decimals: 0,
            style: TextStyle(color: amount < 0 ? colors.danger : colors.onInk, fontWeight: FontWeight.w600, fontSize: 15),
            currencyColor: colors.onInkMuted,
          ),
        ),
      ]),
    );
  }
}
