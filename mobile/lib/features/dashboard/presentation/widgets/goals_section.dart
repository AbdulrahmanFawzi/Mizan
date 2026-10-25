import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_router.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../goals/data/goal.dart';
import '../../../goals/presentation/widgets/goal_card.dart';
import 'section.dart';

class GoalsSection extends StatelessWidget {
  const GoalsSection({super.key, required this.goals});

  final List<Goal> goals;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Section(
      title: l10n.goals,
      child: goals.isEmpty
          ? EmptyHint(l10n.noGoals, icon: Icons.flag_outlined)
          : Column(children: [
              for (final goal in goals) GoalCard(goal: goal, onTap: () => context.push(Routes.goal(goal.id))),
            ]),
    );
  }
}
