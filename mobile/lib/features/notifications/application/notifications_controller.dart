import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../dashboard/data/dashboard_repository.dart';
import '../data/app_notification.dart';

class NotificationsController extends AsyncNotifier<List<AppNotification>> {
  @override
  Future<List<AppNotification>> build() => ref.read(notificationRepositoryProvider).list();

  // Optimistic update: the notification turns "read" on screen immediately, then the API is told.
  // If the API fails, the change is undone so the screen never lies about the server.
  Future<void> markRead(String id) async {
    final before = state.value;
    if (before == null || before.every((n) => n.id != id || n.isRead)) return;

    state = AsyncData([for (final n in before) n.id == id ? n.markedRead() : n]);
    try {
      await ref.read(notificationRepositoryProvider).markRead(id);
      ref.invalidate(dashboardProvider); // the bell's unread count
    } catch (_) {
      state = AsyncData(before);
      rethrow;
    }
  }
}

final notificationsControllerProvider =
    AsyncNotifierProvider.autoDispose<NotificationsController, List<AppNotification>>(NotificationsController.new);
