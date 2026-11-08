import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:lean_sdk_flutter/lean_sdk_flutter.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../../profile/data/profile_repository.dart';
import '../application/banking_actions.dart';
import '../data/banking.dart';

final _linkSessionProvider = FutureProvider.autoDispose<LinkSession>(
  (ref) => ref.read(bankingActionsProvider).createLinkSession(),
);

// UC-70: Lean's own screen (bank choice, consent, bank login) inside the app.
// The API makes a short-lived session for it, so no secret ever reaches the phone (BR-C12).
class BankLinkScreen extends ConsumerStatefulWidget {
  const BankLinkScreen({super.key, this.linkerBuilder});

  // Tests replace Lean's web view with their own widget.
  final Widget Function(LinkSession session, void Function(bool succeeded) onDone)? linkerBuilder;

  @override
  ConsumerState<BankLinkScreen> createState() => _BankLinkScreenState();
}

class _BankLinkScreenState extends ConsumerState<BankLinkScreen> {
  bool _finishing = false;
  String? _error;

  Future<void> _onDone(bool succeeded) async {
    final l10n = AppLocalizations.of(context);
    if (!succeeded) {
      setState(() => _error = l10n.linkFailed);
      return;
    }

    setState(() => _finishing = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref.read(bankingActionsProvider).finishLinking();
      messenger.showSnackBar(SnackBar(content: Text(l10n.bankLinked)));
      if (mounted) context.pop();
    } catch (error) {
      if (mounted) {
        setState(() {
          _finishing = false;
          _error = errorMessage(error, l10n);
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    // BR-U5: the API refuses a link session until the email is verified, so ask for it first.
    final profile = ref.watch(profileProvider);
    if (profile.value?.emailVerified != true) {
      return Scaffold(
        appBar: AppBar(),
        body: Center(
          child: profile.hasValue
              ? EmptyState(
                  icon: Icons.mark_email_unread_rounded,
                  message: l10n.verifyBeforeLink,
                  action: FilledButton(
                    key: const Key('verifyBeforeLink'),
                    onPressed: () => context.push(Routes.verifyEmail),
                    child: Text(l10n.verifyEmail),
                  ),
                )
              : const CircularProgressIndicator(),
        ),
      );
    }

    final session = ref.watch(_linkSessionProvider);

    if (_finishing) {
      return Scaffold(
        body: Center(
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            const CircularProgressIndicator(),
            const SizedBox(height: 16),
            Text(l10n.linking),
          ]),
        ),
      );
    }

    if (_error != null) {
      return Scaffold(
        appBar: AppBar(),
        body: Center(
          child: EmptyState(
            icon: Icons.link_off_rounded,
            message: _error!,
            action: FilledButton(
              onPressed: () {
                setState(() => _error = null);
                ref.invalidate(_linkSessionProvider);
              },
              child: Text(l10n.retry),
            ),
          ),
        ),
      );
    }

    return Scaffold(
      backgroundColor: context.mizan.ink,
      body: SafeArea(
        child: switch (session) {
          AsyncData(:final value) => widget.linkerBuilder?.call(value, _onDone) ?? _LeanLinker(session: value, onDone: _onDone),
          AsyncError(:final error) => Center(
              child: EmptyState(
                icon: Icons.cloud_off_rounded,
                message: errorMessage(error, l10n),
                action: OutlinedButton(onPressed: () => ref.invalidate(_linkSessionProvider), child: Text(l10n.retry)),
              ),
            ),
          _ => const Center(child: CircularProgressIndicator()),
        },
      ),
    );
  }
}

class _LeanLinker extends StatelessWidget {
  const _LeanLinker({required this.session, required this.onDone});

  final LinkSession session;
  final void Function(bool succeeded) onDone;

  @override
  Widget build(BuildContext context) {
    final arabic = Localizations.localeOf(context).languageCode == 'ar';

    return Lean.connect(
      appToken: session.appToken,
      customerId: session.customerId,
      accessToken: session.accessToken,
      isSandbox: session.sandbox,
      country: LeanCountry.sa,
      language: arabic ? LeanLanguage.ar : LeanLanguage.en,
      permissions: [
        for (final name in session.permissions)
          ?LeanPermissions.values.where((p) => p.name == name).firstOrNull,
      ],
      accessTo: session.accessTo.toUtc().toIso8601String(),
      showConsentExplanation: true,
      // Banks that send the user to their own app only appear when these are set (spike finding).
      successRedirectUrl: session.successRedirectUrl,
      failRedirectUrl: session.failRedirectUrl,
      callback: (response) => onDone(response.status == 'SUCCESS'),
      actionCancelled: () => Navigator.of(context).maybePop(),
    );
  }
}
