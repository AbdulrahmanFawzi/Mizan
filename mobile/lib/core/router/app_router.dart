import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/application/auth_controller.dart';
import '../../features/auth/presentation/forgot_password_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/banking/presentation/bank_connections_screen.dart';
import '../../features/banking/presentation/bank_link_screen.dart';
import '../../features/banking/presentation/bank_transaction_screen.dart';
import '../../features/banking/presentation/review_screen.dart';
import '../../features/budget/presentation/budget_screen.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/goals/data/goal.dart';
import '../../features/goals/presentation/goal_detail_screen.dart';
import '../../features/goals/presentation/goal_form_screen.dart';
import '../../features/goals/presentation/goals_screen.dart';
import '../../features/notifications/presentation/notifications_screen.dart';
import '../../features/onboarding/welcome_screen.dart';
import '../../features/onboarding/onboarding_controller.dart';
import '../../features/profile/presentation/verify_email_screen.dart';
import '../../features/settings/edit_profile_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../features/transactions/data/transaction.dart';
import '../../features/transactions/presentation/expense_form_screen.dart';
import '../../features/transactions/presentation/transactions_screen.dart';
import '../widgets/app_shell.dart';

abstract final class Routes {
  static const splash = '/splash';
  static const login = '/login';
  static const register = '/register';
  static const forgotPassword = '/forgot-password';
  static const verifyEmail = '/settings/verify-email';
  static const welcome = '/welcome';
  static const home = '/';
  static const budget = '/budget';
  static const expenses = '/expenses';
  static const newExpense = '/expenses/new';
  static const goals = '/goals';
  static const newGoal = '/goals/new';
  static const notifications = '/notifications';
  static const settings = '/settings';
  static const editProfile = '/settings/profile';
  static const banks = '/settings/banks';
  static const linkBank = '/settings/banks/link';
  static const review = '/review';

  static String editExpense(String id) => '/expenses/$id';
  static String goal(String id) => '/goals/$id';
  static String editGoal(String id) => '/goals/$id/edit';
}

final routerProvider = Provider<GoRouter>((ref) {
  // Re-run the redirect whenever the login state changes (login, logout, session expired).
  final authChanged = ValueNotifier(0);
  ref.listen(authControllerProvider, (_, _) => authChanged.value++);
  ref.listen(onboardingProvider, (_, _) => authChanged.value++);
  ref.onDispose(authChanged.dispose);

  return GoRouter(
    initialLocation: Routes.splash,
    refreshListenable: authChanged,
    redirect: (context, state) {
      final auth = ref.read(authControllerProvider);
      final location = state.matchedLocation;

      if (!auth.hasValue) return Routes.splash;

      final loggedIn = auth.value == AuthStatus.authenticated;
      final onAuthPage = location == Routes.login || location == Routes.register || location == Routes.forgotPassword;

      if (!loggedIn) return onAuthPage ? null : Routes.login;
      if (ref.read(onboardingProvider)) return location == Routes.welcome ? null : Routes.welcome;
      if (onAuthPage || location == Routes.splash || location == Routes.welcome) return Routes.home;
      return null;
    },
    routes: [
      GoRoute(path: Routes.splash, builder: (_, _) => const Scaffold(body: Center(child: CircularProgressIndicator()))),
      GoRoute(path: Routes.login, builder: (_, _) => const LoginScreen()),
      GoRoute(path: Routes.register, builder: (_, _) => const RegisterScreen()),
      GoRoute(path: Routes.forgotPassword, builder: (_, _) => const ForgotPasswordScreen()),
      GoRoute(path: Routes.welcome, builder: (_, _) => const WelcomeScreen()),

      // The four tabs.
      StatefulShellRoute.indexedStack(
        builder: (_, _, shell) => AppShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [GoRoute(path: Routes.home, builder: (_, _) => const DashboardScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: Routes.budget, builder: (_, _) => const BudgetScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: Routes.goals, builder: (_, _) => const GoalsScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: Routes.expenses, builder: (_, _) => const TransactionsScreen())]),
        ],
      ),

      // Full-screen forms on top of the tabs. "new" is listed before ":id" so it isn't read as an id.
      GoRoute(path: Routes.newExpense, builder: (_, _) => const ExpenseFormScreen()),
      GoRoute(
        path: '/expenses/:id',
        // Bank transactions have their own screen: amount and date can't be edited (BR-X4).
        builder: (_, state) => switch (state.extra) {
          Transaction(isFromBank: true) && final transaction => BankTransactionScreen(transaction: transaction),
          final extra => ExpenseFormScreen(existing: extra as Transaction?),
        },
      ),
      GoRoute(path: Routes.review, builder: (_, _) => const ReviewScreen()),
      GoRoute(path: Routes.notifications, builder: (_, _) => const NotificationsScreen()),
      GoRoute(
        path: Routes.settings,
        builder: (_, _) => const SettingsScreen(),
        routes: [
          GoRoute(path: 'profile', builder: (_, _) => const EditProfileScreen()),
          GoRoute(path: 'verify-email', builder: (_, _) => const VerifyEmailScreen()),
          GoRoute(
            path: 'banks',
            builder: (_, _) => const BankConnectionsScreen(),
            routes: [GoRoute(path: 'link', builder: (_, _) => const BankLinkScreen())],
          ),
        ],
      ),
      GoRoute(path: Routes.newGoal, builder: (_, _) => const GoalFormScreen()),
      GoRoute(
        path: '/goals/:id',
        builder: (_, state) => GoalDetailScreen(goalId: state.pathParameters['id']!),
        routes: [
          GoRoute(path: 'edit', builder: (_, state) => GoalFormScreen(existing: state.extra as Goal?)),
        ],
      ),
    ],
  );
});
