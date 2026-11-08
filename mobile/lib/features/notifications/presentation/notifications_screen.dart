import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
import '../../../core/theme/mizan_colors.dart';
import '../../../core/widgets/ui.dart';
import '../../../l10n/app_localizations.dart';
import '../../categories/data/category.dart';
import '../../categories/presentation/category_label.dart';
import '../application/notifications_controller.dart';
import '../data/app_notification.dart';
import 'notification_text.dart';

// UC-60 (list) and UC-61 (mark as read). Tapping a notification also takes the user
// to where they can act on it: the budget for an alert, the goal for a completed goal.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  Future<void> _open(BuildContext context, WidgetRef ref, AppNotification notification) async {
    final messenger = ScaffoldMessenger.of(context);
    final l10n = AppLocalizations.of(context);

    try {
      await ref.read(notificationsControllerProvider.notifier).markRead(notification.id);
    } catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(errorMessage(error, l10n))));
      return;
    }
    if (!context.mounted) return;

    switch (notification.type) {
      case NotificationType.budgetThreshold:
        context.go(Routes.budget);
      case NotificationType.goalCompleted when notification.data['goalId'] != null:
        context.pushReplacement(Routes.goal(notification.data['goalId']!));
      case NotificationType.consentExpiring || NotificationType.consentExpired:
        context.pushReplacement(Routes.banks);
      case NotificationType.reviewNeeded:
        context.pushReplacement(Routes.review);
      default:
        break;
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final notifications = ref.watch(notificationsControllerProvider);
    final categoryNames = {
      for (final c in ref.watch(categoriesProvider).value ?? const <Category>[]) c.id: categoryLabel(c, l10n),
    };

    return Scaffold(
      appBar: AppBar(title: Text(l10n.notifications)),
      body: switch (notifications) {
        AsyncData(:final value) when value.isEmpty => Center(
            child: EmptyState(icon: Icons.notifications_none_rounded, message: l10n.noNotifications),
          ),
        AsyncData(:final value) => RefreshIndicator(
            onRefresh: () => ref.refresh(notificationsControllerProvider.future),
            child: ListView(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 32),
              children: [
                // Unread first, under "New"; the rest under "Earlier".
                for (final (title, group) in [
                  (l10n.notifNew, value.where((n) => !n.isRead).toList()),
                  (l10n.notifEarlier, value.where((n) => n.isRead).toList()),
                ])
                  if (group.isNotEmpty) ...[
                    SectionHeader(title),
                    for (final notification in group)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: NotificationTile(
                          notification: notification,
                          view: describeNotification(notification, l10n, categoryNames),
                          onTap: () => _open(context, ref, notification),
                        ),
                      ),
                  ],
              ],
            ),
          ),
        AsyncError(:final error) => Center(
            child: EmptyState(
              icon: Icons.cloud_off_rounded,
              message: errorMessage(error, l10n),
              action: OutlinedButton(
                onPressed: () => ref.invalidate(notificationsControllerProvider),
                child: Text(l10n.retry),
              ),
            ),
          ),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }
}

class NotificationTile extends StatelessWidget {
  const NotificationTile({super.key, required this.notification, required this.view, required this.onTap});

  final AppNotification notification;
  final NotificationView view;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = context.mizan;
    final l10n = AppLocalizations.of(context);
    final unread = !notification.isRead;
    final locale = Localizations.localeOf(context).toString();
    final time = DateFormat.MMMd(locale).add_jm().format(notification.createdAt);
    final accent = view.color ?? colors.mintOnLight;

    return Material(
      key: Key('notification-${notification.id}'),
      // Unread items stand out with a mint tint, a mint border, and a dot.
      color: unread ? colors.mint.withValues(alpha: 0.08) : theme.colorScheme.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: BorderSide(color: unread ? colors.mint.withValues(alpha: 0.5) : colors.border),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(20),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            IconTile(icon: view.icon, color: accent),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(view.title, style: theme.textTheme.titleSmall?.copyWith(fontWeight: unread ? FontWeight.w700 : FontWeight.w600)),
                const SizedBox(height: 2),
                Text(view.message),
                const SizedBox(height: 6),
                Text(time, style: TextStyle(color: colors.textMuted, fontSize: 12)),
              ]),
            ),
            if (unread)
              Semantics(
                label: l10n.unread,
                child: Padding(
                  padding: const EdgeInsetsDirectional.only(start: 8, top: 4),
                  child: Icon(Icons.circle, size: 10, color: colors.mintOnLight),
                ),
              ),
          ]),
        ),
      ),
    );
  }
}
