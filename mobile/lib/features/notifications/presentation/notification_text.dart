import 'package:flutter/material.dart';

import '../../../l10n/app_localizations.dart';
import '../data/app_notification.dart';

typedef NotificationView = ({String title, String message, IconData icon, Color? color});

// Turns the notification's facts into a sentence in the user's language.
// `categoryNames` translates built-in categories ("أخرى" -> "Other") by id.
NotificationView describeNotification(
  AppNotification notification,
  AppLocalizations l10n,
  Map<String, String> categoryNames,
) {
  final data = notification.data;
  final category = categoryNames[data['categoryId']] ?? data['categoryName'] ?? '';

  return switch (notification.type) {
    NotificationType.budgetThreshold when notification.threshold == 100 => (
        title: l10n.notifBudgetExceededTitle,
        message: l10n.notifBudgetExceeded(category),
        icon: Icons.error_outline,
        color: const Color(0xFFE5484D),
      ),
    NotificationType.budgetThreshold => (
        title: l10n.notifBudgetUsedTitle,
        message: l10n.notifBudgetUsed(notification.threshold ?? 0, category),
        icon: Icons.warning_amber_rounded,
        color: const Color(0xFFF59E0B),
      ),
    NotificationType.goalCompleted => (
        title: l10n.notifGoalCompletedTitle,
        message: l10n.notifGoalCompleted(data['goalName'] ?? ''),
        icon: Icons.emoji_events_outlined,
        color: const Color(0xFF0E9F6E),
      ),
    NotificationType.unknown => (
        title: l10n.notifOther,
        message: notification.serverMessage,
        icon: Icons.notifications_outlined,
        color: null,
      ),
  };
}
