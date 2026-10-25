import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/router/app_router.dart';
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
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              const Icon(Icons.notifications_none, size: 48),
              const SizedBox(height: 12),
              Text(l10n.noNotifications),
            ]),
          ),
        AsyncData(:final value) => RefreshIndicator(
            onRefresh: () => ref.refresh(notificationsControllerProvider.future),
            child: ListView.separated(
              itemCount: value.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) => NotificationTile(
                notification: value[index],
                view: describeNotification(value[index], l10n, categoryNames),
                onTap: () => _open(context, ref, value[index]),
              ),
            ),
          ),
        AsyncError(:final error) => Center(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Text(errorMessage(error, l10n)),
              TextButton(onPressed: () => ref.invalidate(notificationsControllerProvider), child: Text(l10n.retry)),
            ]),
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
    final l10n = AppLocalizations.of(context);
    final unread = !notification.isRead;
    final locale = Localizations.localeOf(context).toString();
    final time = DateFormat.MMMd(locale).add_jm().format(notification.createdAt);

    return ListTile(
      key: Key('notification-${notification.id}'),
      onTap: onTap,
      // Unread items stand out with a tinted background, bold title, and a dot.
      tileColor: unread ? theme.colorScheme.primaryContainer.withValues(alpha: 0.35) : null,
      leading: CircleAvatar(
        backgroundColor: (view.color ?? theme.colorScheme.primary).withValues(alpha: 0.15),
        child: Icon(view.icon, color: view.color),
      ),
      title: Text(view.title, style: TextStyle(fontWeight: unread ? FontWeight.bold : FontWeight.normal)),
      subtitle: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(view.message),
        const SizedBox(height: 4),
        Text(time, style: theme.textTheme.bodySmall),
      ]),
      trailing: unread
          ? Semantics(
              label: l10n.unread,
              child: Icon(Icons.circle, size: 10, color: theme.colorScheme.primary),
            )
          : null,
    );
  }
}
