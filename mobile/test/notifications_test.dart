import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/categories/data/category.dart';
import 'package:mizan/features/notifications/data/app_notification.dart';
import 'package:mizan/features/notifications/presentation/notification_text.dart';
import 'package:mizan/features/notifications/presentation/notifications_screen.dart';
import 'package:mizan/l10n/app_localizations_ar.dart';
import 'package:mizan/l10n/app_localizations_en.dart';

import 'helpers/test_app.dart';

class FakeNotificationRepository implements NotificationRepository {
  FakeNotificationRepository(this.items, {this.failMarkRead = false});

  final List<AppNotification> items;
  final bool failMarkRead;
  final markedRead = <String>[];

  @override
  Future<List<AppNotification>> list() async => items;

  @override
  Future<void> markRead(String id) async {
    if (failMarkRead) throw Exception('offline');
    markedRead.add(id);
  }
}

AppNotification _threshold(int percent, {String categoryId = 'fuel', String categoryName = 'الوقود', bool read = false}) =>
    AppNotification(
      id: 'n$percent',
      type: NotificationType.budgetThreshold,
      data: {'threshold': '$percent', 'categoryId': categoryId, 'categoryName': categoryName},
      serverMessage: 'server text',
      createdAt: DateTime(2026, 10, 25, 9, 30),
      isRead: read,
    );

AppNotification _unknown({bool read = false}) => AppNotification(
      id: 'u1',
      type: NotificationType.unknown,
      data: const {},
      serverMessage: 'Something new from the server',
      createdAt: DateTime(2026, 10, 25, 9, 30),
      isRead: read,
    );

void main() {
  group('AppNotification.fromJson', () {
    test('reads a budget alert', () {
      final n = AppNotification.fromJson({
        'id': 'n1',
        'type': 'BudgetThreshold',
        'title': 'تنبيه الميزانية',
        'message': '⚠️ استخدمت 80% من ميزانية الوقود.',
        'data': {'threshold': '80', 'categoryId': 'c1', 'categoryName': 'الوقود'},
        'createdAt': '2026-10-22T09:51:35.721267+00:00',
        'isRead': false,
      });

      expect(n.type, NotificationType.budgetThreshold);
      expect(n.threshold, 80);
      expect(n.createdAt.isUtc, isFalse); // shown in the phone's local time
    });

    test('a type this app version does not know does not crash', () {
      final n = AppNotification.fromJson({
        'id': 'n2',
        'type': 'SomethingAddedLater',
        'message': 'hello',
        'data': <String, dynamic>{},
        'createdAt': '2026-10-22T09:51:35Z',
        'isRead': true,
      });

      expect(n.type, NotificationType.unknown);
    });
  });

  group('describeNotification', () {
    final en = AppLocalizationsEn();
    final ar = AppLocalizationsAr();

    test('writes the sentence in the user language', () {
      expect(describeNotification(_threshold(80), en, {}).message, "You've used 80% of your الوقود budget.");
      expect(describeNotification(_threshold(80), ar, {}).message, 'استخدمت 80% من ميزانية الوقود.');
    });

    test('100% means the budget was exceeded', () {
      final view = describeNotification(_threshold(100), en, {});

      expect(view.title, 'Budget exceeded');
      expect(view.icon, Icons.error_outline);
    });

    test('built-in categories are translated by id', () {
      final view = describeNotification(_threshold(90, categoryId: 'other', categoryName: 'أخرى'), en, {'other': 'Other'});

      expect(view.message, "You've used 90% of your Other budget.");
    });

    test('unknown types fall back to the server sentence', () {
      expect(describeNotification(_unknown(), en, {}).message, 'Something new from the server');
    });
  });

  group('Notifications screen', () {
    Future<FakeNotificationRepository> pump(WidgetTester tester, List<AppNotification> items, {bool failMarkRead = false}) async {
      final repository = FakeNotificationRepository(items, failMarkRead: failMarkRead);
      await tester.pumpWidget(testApp(const NotificationsScreen(), overrides: [
        notificationRepositoryProvider.overrideWithValue(repository),
        categoriesProvider.overrideWith((ref) async => const <Category>[]),
      ]));
      await tester.pumpAndSettle();
      return repository;
    }

    testWidgets('shows an empty state', (tester) async {
      await pump(tester, []);

      expect(find.text('No notifications yet'), findsOneWidget);
    });

    testWidgets('unread notifications show a dot', (tester) async {
      await pump(tester, [_threshold(80), _threshold(100, read: true)]);

      expect(find.text('Budget alert'), findsOneWidget);
      expect(find.text('Budget exceeded'), findsOneWidget);
      expect(find.byIcon(Icons.circle), findsOneWidget);
    });

    testWidgets('tapping marks it read right away', (tester) async {
      final repository = await pump(tester, [_unknown()]);

      await tester.tap(find.text('Something new from the server'));
      await tester.pumpAndSettle();

      expect(repository.markedRead, ['u1']);
      expect(find.byIcon(Icons.circle), findsNothing);
    });

    testWidgets('if the server fails, it goes back to unread', (tester) async {
      await pump(tester, [_unknown()], failMarkRead: true);

      await tester.tap(find.text('Something new from the server'));
      await tester.pumpAndSettle();

      expect(find.byIcon(Icons.circle), findsOneWidget);
      expect(find.byType(SnackBar), findsOneWidget);
    });
  });
}
