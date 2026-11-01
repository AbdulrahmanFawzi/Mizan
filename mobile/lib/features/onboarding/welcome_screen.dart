import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/format/formatters.dart';
import '../../core/network/api_exception.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/mizan_colors.dart';
import '../../core/widgets/ui.dart';
import '../../core/widgets/validators.dart';
import '../../l10n/app_localizations.dart';
import '../auth/presentation/auth_scaffold.dart';
import '../budget/application/budget_actions.dart';
import '../profile/data/profile_repository.dart';
import '../profile/presentation/verify_email_screen.dart';
import 'category_suggestions.dart';
import 'onboarding_controller.dart';

// The guided setup after signing up: confirm the email, choose categories, link a bank.
// Every step can be skipped; finishing (or skipping all) goes home.
class WelcomeScreen extends ConsumerStatefulWidget {
  const WelcomeScreen({super.key});

  @override
  ConsumerState<WelcomeScreen> createState() => _WelcomeScreenState();
}

class _WelcomeScreenState extends ConsumerState<WelcomeScreen> {
  static const _steps = 3;
  int _step = 0;

  void _next() => _step == _steps - 1 ? _finish() : setState(() => _step++);

  // The router sees the setup is done and goes home.
  Future<void> _finish({bool linkBank = false}) async {
    final router = GoRouter.of(context);
    await ref.read(onboardingProvider.notifier).finish();
    if (linkBank) router.push(Routes.linkBank);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;

    return Scaffold(
      body: SafeArea(
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 8, 12, 0),
            child: Row(children: [
              Text(l10n.stepOf(_step + 1, _steps), style: TextStyle(color: colors.textMuted)),
              const Spacer(),
              TextButton(key: const Key('skipSetup'), onPressed: _finish, child: Text(l10n.skipSetup)),
            ]),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(value: (_step + 1) / _steps, minHeight: 6),
            ),
          ),
          Expanded(
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 250),
              child: switch (_step) {
                0 => _Step(
                    key: const ValueKey(0),
                    title: l10n.verifyEmail,
                    later: _next,
                    child: VerifyEmailForm(onVerified: _next),
                  ),
                1 => _Step(
                    key: const ValueKey(1),
                    title: l10n.setupBudgetTitle,
                    later: _next,
                    child: _StarterBudget(onDone: _next),
                  ),
                _ => _Step(
                    key: const ValueKey(2),
                    title: l10n.linkBankTitle,
                    later: _finish,
                    laterLabel: l10n.finishSetup,
                    child: _LinkBankStep(onLink: () => _finish(linkBank: true)),
                  ),
              },
            ),
          ),
        ]),
      ),
    );
  }
}

class _Step extends StatelessWidget {
  const _Step({super.key, required this.title, required this.child, required this.later, this.laterLabel});

  final String title;
  final Widget child;
  final VoidCallback later;
  final String? laterLabel;

  @override
  Widget build(BuildContext context) => ListView(
        padding: const EdgeInsets.fromLTRB(24, 24, 24, 32),
        children: [
          Text(title, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700)),
          const SizedBox(height: 20),
          child,
          const SizedBox(height: 8),
          TextButton(
            key: const Key('stepLater'),
            onPressed: later,
            child: Text(laterLabel ?? AppLocalizations.of(context).later),
          ),
        ],
      );
}

// Suggested categories with limits from the income. The user unticks or changes any of them.
class _StarterBudget extends ConsumerStatefulWidget {
  const _StarterBudget({required this.onDone});

  final VoidCallback onDone;

  @override
  ConsumerState<_StarterBudget> createState() => _StarterBudgetState();
}

class _StarterBudgetState extends ConsumerState<_StarterBudget> {
  final _formKey = GlobalKey<FormState>();
  final _chosen = List.filled(categorySuggestions.length, true);
  List<TextEditingController>? _limits;
  TextEditingController? _savings;
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    for (final c in _limits ?? const <TextEditingController>[]) {
      c.dispose();
    }
    _savings?.dispose();
    super.dispose();
  }

  void _fill(double income) {
    if (_limits != null) return;
    String text(double v) => v.toStringAsFixed(0);
    _limits = [for (final s in categorySuggestions) TextEditingController(text: text(s.limitFor(income)))];
    _savings = TextEditingController(text: text((income * savingsShare / 50).round() * 50));
  }

  double get _total =>
      [
        for (var i = 0; i < categorySuggestions.length; i++)
          if (_chosen[i]) double.tryParse(_limits![i].text) ?? 0,
        double.tryParse(_savings!.text) ?? 0,
      ].fold(0, (a, b) => a + b);

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    final l10n = AppLocalizations.of(context);
    final language = Localizations.localeOf(context).languageCode;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(budgetActionsProvider).createStarterBudget(
        categories: {
          for (var i = 0; i < categorySuggestions.length; i++)
            if (_chosen[i]) categorySuggestions[i].name(language): double.parse(_limits![i].text),
        },
        savingsLimit: double.parse(_savings!.text),
      );
      widget.onDone();
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
    final profile = ref.watch(profileProvider);
    final language = Localizations.localeOf(context).languageCode;

    if (profile.value case final p?) {
      _fill(p.monthlyIncome);
    } else {
      return const Padding(padding: EdgeInsets.all(32), child: Center(child: CircularProgressIndicator()));
    }
    final income = profile.value!.monthlyIncome;

    Widget amountField(TextEditingController controller, {bool enabled = true, Key? key}) => SizedBox(
          width: 110,
          child: TextFormField(
            key: key,
            controller: controller,
            enabled: enabled,
            textAlign: TextAlign.end,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            textDirection: TextDirection.ltr,
            decoration: const InputDecoration(isDense: true),
            onChanged: (_) => setState(() {}),
            validator: enabled ? (v) => Validators.nonNegativeAmount(v, l10n) : null,
          ),
        );

    return Form(
      key: _formKey,
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(l10n.setupBudgetExplain, style: TextStyle(color: colors.textMuted, height: 1.5)),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 4),
            child: Column(children: [
              for (var i = 0; i < categorySuggestions.length; i++)
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  child: Row(children: [
                    Checkbox(
                      key: Key('suggestion-$i'),
                      value: _chosen[i],
                      onChanged: (v) => setState(() => _chosen[i] = v ?? false),
                    ),
                    Expanded(child: Text(categorySuggestions[i].name(language))),
                    amountField(_limits![i], enabled: _chosen[i]),
                  ]),
                ),
              const Divider(),
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 2, 8, 6),
                child: Row(children: [
                  Icon(Icons.savings_rounded, color: colors.mintOnLight),
                  const SizedBox(width: 12),
                  Expanded(child: Text(l10n.categorySavings)),
                  amountField(_savings!, key: const Key('savingsLimit')),
                ]),
              ),
            ]),
          ),
        ),
        const SizedBox(height: 12),
        Text(
          l10n.setupBudgetTotal(context.money(_total), context.money(income)),
          style: TextStyle(color: _total > income ? colors.danger : colors.textMuted),
        ),
        const SizedBox(height: 16),
        FormErrorText(_error),
        FilledButton(
          key: const Key('createBudget'),
          onPressed: _busy ? null : _save,
          child: _busy
              ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2))
              : Text(l10n.createBudget),
        ),
      ]),
    );
  }
}

class _LinkBankStep extends StatelessWidget {
  const _LinkBankStep({required this.onLink});

  final VoidCallback onLink;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;

    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Align(
        alignment: AlignmentDirectional.centerStart,
        child: IconTile(icon: Icons.account_balance_rounded, color: colors.mintOnLight, size: 52),
      ),
      const SizedBox(height: 16),
      Text(l10n.linkBankExplain, style: const TextStyle(height: 1.5)),
      const SizedBox(height: 12),
      Row(children: [
        Icon(Icons.verified_user_rounded, size: 18, color: colors.mintOnLight),
        const SizedBox(width: 6),
        Expanded(child: Text(l10n.linkBankSecure, style: TextStyle(color: colors.textMuted, fontSize: 12))),
      ]),
      const SizedBox(height: 24),
      FilledButton.icon(
        key: const Key('setupLinkBank'),
        onPressed: onLink,
        icon: const Icon(Icons.link_rounded),
        label: Text(l10n.linkBank),
      ),
    ]);
  }
}
