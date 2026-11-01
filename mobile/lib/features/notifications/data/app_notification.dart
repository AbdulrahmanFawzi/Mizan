import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';

// Named AppNotification, not Notification: Flutter already has a `Notification` class
// (the one NotificationListener uses), and the clash would be confusing.
enum NotificationType { budgetThreshold, goalCompleted, consentExpiring, consentExpired, reviewNeeded, unknown }

class AppNotification {
  const AppNotification({
    required this.id,
    required this.type,
    required this.data,
    required this.serverMessage,
    required this.createdAt,
    required this.isRead,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) => AppNotification(
        id: json['id'] as String,
        type: switch (json['type']) {
          'BudgetThreshold' => NotificationType.budgetThreshold,
          'GoalCompleted' => NotificationType.goalCompleted,
          'ConsentExpiring' => NotificationType.consentExpiring,
          'ConsentExpired' => NotificationType.consentExpired,
          'ReviewNeeded' => NotificationType.reviewNeeded,
          // A type added to the API later must not crash older versions of the app.
          _ => NotificationType.unknown,
        },
        data: (json['data'] as Map<String, dynamic>).cast<String, String>(),
        serverMessage: json['message'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
        isRead: json['isRead'] as bool,
      );

  final String id;
  final NotificationType type;

  // The facts behind the notification (threshold, category, goal). The app writes
  // the sentence from these in the user's language.
  final Map<String, String> data;

  // The API's own (Arabic) sentence, used only for types this app version doesn't know.
  final String serverMessage;
  final DateTime createdAt;
  final bool isRead;

  int? get threshold => int.tryParse(data['threshold'] ?? '');

  AppNotification markedRead() => AppNotification(
        id: id,
        type: type,
        data: data,
        serverMessage: serverMessage,
        createdAt: createdAt,
        isRead: true,
      );
}

class NotificationRepository {
  NotificationRepository(this._dio);

  final Dio _dio;

  Future<List<AppNotification>> list() async {
    final response = await apiCall(() => _dio.get<List<dynamic>>('/api/notifications'));
    return response.data!.cast<Map<String, dynamic>>().map(AppNotification.fromJson).toList();
  }

  Future<void> markRead(String id) => apiCall(() => _dio.post<void>('/api/notifications/$id/read'));
}

final notificationRepositoryProvider = Provider<NotificationRepository>(
  (ref) => NotificationRepository(ref.watch(dioProvider)),
);
