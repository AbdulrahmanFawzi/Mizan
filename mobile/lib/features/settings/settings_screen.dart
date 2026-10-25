import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/format/formatters.dart';
import '../../core/network/api_exception.dart';
import '../../core/router/app_router.dart';
import '../../l10n/app_localizations.dart';
import '../auth/application/auth_controller.dart';
import '../profile/data/profile_repository.dart';
import 'locale_controller.dart';

class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  // UC-06. Deleting is permanent, so the dialog says exactly what will be lost
  // and the confirm button is red and explicit, not just "OK".
  Future<void> _deleteAccount(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final colors = Theme.of(context).colorScheme;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        icon: Icon(Icons.warning_amber_rounded, color: colors.error),
        title: Text(l10n.deleteAccountQuestion),
        content: Text(l10n.deleteAccountExplain),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: Text(l10n.cancel)),
          FilledButton(
            key: const Key('confirmDeleteAccount'),
            style: FilledButton.styleFrom(backgroundColor: colors.error, foregroundColor: colors.onError),
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.deleteAccountConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      // On success the router sees "logged out" and shows the login screen.
      await ref.read(authControllerProvider.notifier).deleteAccount();
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(error, l10n))));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final profile = ref.watch(profileProvider).value;
    final locale = ref.watch(localeProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.settings)),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          _Header(l10n.profile),
          if (profile == null)
            const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator()))
          else ...[
            ListTile(
              leading: const Icon(Icons.person_outline),
              title: Text(profile.name),
              subtitle: Text(profile.email, textDirection: TextDirection.ltr),
            ),
            ListTile(
              leading: const Icon(Icons.payments_outlined),
              title: Text(l10n.income),
              trailing: Text(context.money(profile.monthlyIncome)),
            ),
            ListTile(
              leading: const Icon(Icons.event_repeat_outlined),
              title: Text(l10n.periodStartDay),
              trailing: Text('${profile.periodStartDay}'),
            ),
            ListTile(
              key: const Key('editProfile'),
              leading: const Icon(Icons.edit_outlined),
              title: Text(l10n.editProfile),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push(Routes.editProfile),
            ),
          ],
          const Divider(),
          _Header(l10n.languageSetting),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: SegmentedButton<String>(
              key: const Key('languageSegment'),
              segments: [
                ButtonSegment(value: 'system', label: Text(l10n.languageSystem)),
                const ButtonSegment(value: 'ar', label: Text('العربية')),
                const ButtonSegment(value: 'en', label: Text('English')),
              ],
              selected: {locale?.languageCode ?? 'system'},
              onSelectionChanged: (selection) {
                final value = selection.single;
                ref.read(localeProvider.notifier).set(value == 'system' ? null : Locale(value));
              },
            ),
          ),
          const Divider(),
          _Header(l10n.account),
          ListTile(
            key: const Key('logout'),
            leading: const Icon(Icons.logout),
            title: Text(l10n.logout),
            onTap: () => ref.read(authControllerProvider.notifier).logout(),
          ),
          ListTile(
            key: const Key('deleteAccount'),
            leading: Icon(Icons.delete_forever_outlined, color: theme.colorScheme.error),
            title: Text(l10n.deleteAccount, style: TextStyle(color: theme.colorScheme.error)),
            onTap: () => _deleteAccount(context, ref),
          ),
        ],
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsetsDirectional.fromSTEB(16, 16, 16, 4),
        child: Text(text, style: Theme.of(context).textTheme.labelLarge?.copyWith(color: Theme.of(context).colorScheme.primary)),
      );
}
