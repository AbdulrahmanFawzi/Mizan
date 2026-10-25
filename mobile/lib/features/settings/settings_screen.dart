import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/network/api_exception.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/mizan_colors.dart';
import '../../core/widgets/money_text.dart';
import '../../core/widgets/ui.dart';
import '../../l10n/app_localizations.dart';
import '../auth/application/auth_controller.dart';
import '../profile/data/profile_repository.dart';
import 'locale_controller.dart';
import 'theme_mode_controller.dart';

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
    final colors = context.mizan;
    final profile = ref.watch(profileProvider).value;
    final locale = ref.watch(localeProvider);
    final themeMode = ref.watch(themeModeProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.settings)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
        children: [
          if (profile == null)
            const Card(child: SizedBox(height: 180, child: Center(child: CircularProgressIndicator())))
          else
            _ProfileCard(profile: profile),
          SectionHeader(l10n.languageSetting),
          SizedBox(
            width: double.infinity,
            child: SegmentedButton<String>(
              key: const Key('languageSegment'),
              showSelectedIcon: false,
              segments: [
                ButtonSegment(value: 'system', label: Text(l10n.languageSystem), icon: const Icon(Icons.smartphone_rounded)),
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
          SectionHeader(l10n.appearance),
          SizedBox(
            width: double.infinity,
            child: SegmentedButton<ThemeMode>(
              key: const Key('themeSegment'),
              showSelectedIcon: false,
              segments: [
                ButtonSegment(value: ThemeMode.system, label: Text(l10n.themeSystem), icon: const Icon(Icons.brightness_auto_rounded)),
                ButtonSegment(value: ThemeMode.light, label: Text(l10n.themeLight), icon: const Icon(Icons.light_mode_rounded)),
                ButtonSegment(value: ThemeMode.dark, label: Text(l10n.themeDark), icon: const Icon(Icons.dark_mode_rounded)),
              ],
              selected: {themeMode},
              onSelectionChanged: (selection) => ref.read(themeModeProvider.notifier).set(selection.single),
            ),
          ),
          SectionHeader(l10n.account),
          Card(
            clipBehavior: Clip.antiAlias,
            child: Column(children: [
              _SettingsRow(
                key: const Key('logout'),
                icon: Icons.logout_rounded,
                color: colors.textMuted,
                label: l10n.logout,
                onTap: () => ref.read(authControllerProvider.notifier).logout(),
              ),
              const Divider(indent: 68),
              _SettingsRow(
                key: const Key('deleteAccount'),
                icon: Icons.delete_forever_rounded,
                color: colors.danger,
                label: l10n.deleteAccount,
                onTap: () => _deleteAccount(context, ref),
              ),
            ]),
          ),
        ],
      ),
    );
  }
}

class _ProfileCard extends StatelessWidget {
  const _ProfileCard({required this.profile});

  final Profile profile;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = context.mizan;
    final theme = Theme.of(context);
    final initial = profile.name.trim().isEmpty ? '?' : profile.name.trim().characters.first.toUpperCase();

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        gradient: LinearGradient(
          begin: AlignmentDirectional.topStart,
          end: AlignmentDirectional.bottomEnd,
          colors: [colors.ink, colors.inkSoft],
        ),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Row(children: [
          CircleAvatar(
            radius: 28,
            backgroundColor: colors.mint,
            child: Text(initial, style: TextStyle(color: colors.ink, fontSize: 22, fontWeight: FontWeight.w700)),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(profile.name, style: theme.textTheme.titleLarge?.copyWith(color: colors.onInk)),
              Text(
                profile.email,
                textDirection: TextDirection.ltr,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: colors.onInkMuted),
              ),
            ]),
          ),
        ]),
        const SizedBox(height: 18),
        Row(children: [
          Expanded(
            child: _InkStat(
              icon: Icons.payments_rounded,
              label: l10n.income,
              value: MoneyText(
                profile.monthlyIncome,
                style: TextStyle(color: colors.onInk, fontWeight: FontWeight.w600, fontSize: 16),
                currencyColor: colors.onInkMuted,
              ),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: _InkStat(
              icon: Icons.event_repeat_rounded,
              label: l10n.periodStartDay,
              value: Text('${profile.periodStartDay}',
                  style: TextStyle(color: colors.onInk, fontWeight: FontWeight.w600, fontSize: 16)),
            ),
          ),
        ]),
        const SizedBox(height: 14),
        TextButton.icon(
          key: const Key('editProfile'),
          onPressed: () => context.push(Routes.editProfile),
          icon: const Icon(Icons.edit_rounded, size: 18),
          label: Text(l10n.editProfile),
          style: TextButton.styleFrom(
            foregroundColor: colors.mint,
            backgroundColor: colors.mint.withValues(alpha: 0.12),
            minimumSize: const Size.fromHeight(48),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          ),
        ),
      ]),
    );
  }
}

class _InkStat extends StatelessWidget {
  const _InkStat({required this.icon, required this.label, required this.value});

  final IconData icon;
  final String label;
  final Widget value;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: colors.onInk.withValues(alpha: 0.07), borderRadius: BorderRadius.circular(14)),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Icon(icon, size: 14, color: colors.onInkMuted),
          const SizedBox(width: 4),
          Expanded(
            child: Text(label, maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(color: colors.onInkMuted, fontSize: 12)),
          ),
        ]),
        const SizedBox(height: 4),
        FittedBox(fit: BoxFit.scaleDown, alignment: AlignmentDirectional.centerStart, child: value),
      ]),
    );
  }
}

class _SettingsRow extends StatelessWidget {
  const _SettingsRow({super.key, required this.icon, required this.color, required this.label, required this.onTap});

  final IconData icon;
  final Color color;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(children: [
            IconTile(icon: icon, color: color, size: 40),
            const SizedBox(width: 12),
            Expanded(child: Text(label, style: Theme.of(context).textTheme.titleSmall?.copyWith(color: color == context.mizan.danger ? color : null))),
            Icon(Icons.chevron_right_rounded, color: context.mizan.textMuted),
          ]),
        ),
      );
}
