import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mizan/features/auth/application/auth_controller.dart';
import 'package:mizan/features/auth/data/auth_repository.dart';
import 'package:mizan/features/auth/presentation/forgot_password_screen.dart';

import 'helpers/test_app.dart';

class FakeAuthRepository implements AuthRepository {
  final calls = <String>[];

  @override
  Future<void> forgotPassword(String email) async => calls.add('forgot $email');

  @override
  Future<void> resetPassword({required String email, required String code, required String newPassword}) async =>
      calls.add('reset $email $code $newPassword');

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class _LoggedOut extends AuthController {
  @override
  Future<AuthStatus> build() async => AuthStatus.unauthenticated;
}

void main() {
  testWidgets('forgot password: email, then the code and a new password', (tester) async {
    usePhoneScreen(tester);
    final repository = FakeAuthRepository();
    await tester.pumpWidget(testApp(const ForgotPasswordScreen(), overrides: [
      authRepositoryProvider.overrideWithValue(repository),
      authControllerProvider.overrideWith(_LoggedOut.new),
    ]));

    await tester.enterText(find.byType(TextFormField).first, 'a@example.com');
    await tester.tap(find.byKey(const Key('resetSubmit')));
    await tester.pumpAndSettle();
    expect(repository.calls, ['forgot a@example.com']);

    // A short code is caught before it reaches the API.
    await tester.enterText(find.byType(TextFormField).at(1), '12');
    await tester.enterText(find.byKey(const Key('newPassword')), 'N3w-passw0rd');
    await tester.tap(find.byKey(const Key('resetSubmit')));
    await tester.pumpAndSettle();
    expect(find.text('Enter the 6-digit code'), findsOneWidget);

    await tester.enterText(find.byType(TextFormField).at(1), '123456');
    await tester.tap(find.byKey(const Key('resetSubmit')));
    await tester.pumpAndSettle();
    expect(repository.calls.last, 'reset a@example.com 123456 N3w-passw0rd');
  });
}
