import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/mizan_logo.dart';
import '../../../l10n/app_localizations.dart';
import '../../settings/locale_controller.dart';

// Shared layout for login and register: an ink brand area on top, the form on a rounded sheet below.
class AuthScaffold extends ConsumerWidget {
  const AuthScaffold({super.key, required this.title, required this.children});

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: colors.ink,
      body: Column(children: [
        SafeArea(
          bottom: false,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(24, 8, 24, 28),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Align(
                alignment: AlignmentDirectional.centerEnd,
                child: TextButton(
                  key: const Key('languageToggle'),
                  style: TextButton.styleFrom(foregroundColor: colors.mint),
                  onPressed: () => ref.read(localeProvider.notifier).toggle(Localizations.localeOf(context)),
                  child: Text(l10n.language),
                ),
              ),
              const SizedBox(height: 8),
              const MizanLogo(),
              const SizedBox(height: 20),
              Text(l10n.appName,
                  style: theme.textTheme.headlineMedium?.copyWith(color: colors.onInk, fontWeight: FontWeight.w700)),
              const SizedBox(height: 4),
              Text(l10n.tagline, style: theme.textTheme.bodyLarge?.copyWith(color: colors.onInkMuted)),
            ]),
          ),
        ),
        Expanded(
          child: Container(
            width: double.infinity,
            decoration: BoxDecoration(
              color: theme.scaffoldBackgroundColor,
              borderRadius: const BorderRadius.vertical(top: Radius.circular(32)),
            ),
            child: SafeArea(
              top: false,
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(24, 28, 24, 24),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 440),
                    child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                      Text(title, style: theme.textTheme.titleLarge),
                      const SizedBox(height: 20),
                      ...children,
                    ]),
                  ),
                ),
              ),
            ),
          ),
        ),
      ]),
    );
  }
}

class FormErrorText extends StatelessWidget {
  const FormErrorText(this.message, {super.key});

  final String? message;

  @override
  Widget build(BuildContext context) {
    if (message == null) return const SizedBox.shrink();
    final colors = context.mizan;
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: colors.danger.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(children: [
        Icon(Icons.error_outline_rounded, color: colors.danger, size: 20),
        const SizedBox(width: 8),
        Expanded(child: Text(message!, style: TextStyle(color: colors.danger))),
      ]),
    );
  }
}
