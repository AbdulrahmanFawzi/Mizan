import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/application/auth_controller.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/budget/presentation/budget_screen.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/goals/data/goal.dart';
import '../../features/goals/presentation/goal_detail_screen.dart';
import '../../features/goals/presentation/goal_form_screen.dart';
import '../../features/goals/presentation/goals_screen.dart';
import '../../features/transactions/data/transaction.dart';
import '../../features/transactions/presentation/expense_form_screen.dart';
import '../../features/transactions/presentation/transactions_screen.dart';
import '../widgets/app_shell.dart';

abstract final class Routes {
  static const splash = '/splash';
  static const login = '/login';
  static const register = '/register';
  static const home = '/';
  static const budget = '/budget';
  static const expenses = '/expenses';
  static const newExpense = '/expenses/new';
  static const goals = '/goals';
  static const newGoal = '/goals/new';

  static String editExpense(String id) => '/expenses/$id';
  static String goal(String id) => '/goals/$id';
  static String editGoal(String id) => '/goals/$id/edit';
}

final routerProvider = Provider<GoRouter>((ref) {
  // Re-run the redirect whenever the login state changes (login, logout, session expired).
  final authChanged = ValueNotifier(0);
  ref.listen(authControllerProvider, (_, _) => authChanged.value++);
  ref.onDispose(authChanged.dispose);

  return GoRouter(
    initialLocation: Routes.splash,
    refreshListenable: authChanged,
    redirect: (context, state) {
      final auth = ref.read(authControllerProvider);
      final location = state.matchedLocation;

      if (!auth.hasValue) return Routes.splash;

      final loggedIn = auth.value == AuthStatus.authenticated;
      final onAuthPage = location == Routes.login || location == Routes.register;

      if (!loggedIn) return onAuthPage ? null : Routes.login;
      if (onAuthPage || location == Routes.splash) return Routes.home;
      return null;
    },
    routes: [
      GoRoute(path: Routes.splash, builder: (_, _) => const Scaffold(body: Center(child: CircularProgressIndicator()))),
      GoRoute(path: Routes.login, builder: (_, _) => const LoginScreen()),
      GoRoute(path: Routes.register, builder: (_, _) => const RegisterScreen()),

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
        builder: (_, state) => ExpenseFormScreen(existing: state.extra as Transaction?),
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
