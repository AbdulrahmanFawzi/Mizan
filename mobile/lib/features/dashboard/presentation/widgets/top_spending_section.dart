import 'package:flutter/material.dart';

import '../../../../core/format/formatters.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/dashboard.dart';
import 'section.dart';

class TopSpendingSection extends StatelessWidget {
  const TopSpendingSection({super.key, required this.items, required this.totalSpent, this.nameOf});

  final List<TopSpending> items;
  final double totalSpent;

  // Translated category names by id; falls back to the name the API sent.
  final Map<String, String>? nameOf;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Section(
      title: l10n.topSpending,
      child: items.isEmpty
          ? EmptyHint(l10n.noSpendingYet)
          : Card.outlined(
              child: Column(
                children: [
                  for (final item in items)
                    ListTile(
                      title: Text(nameOf?[item.categoryId] ?? item.name),
                      subtitle: totalSpent > 0
                          ? LinearProgressIndicator(value: item.spent / totalSpent, minHeight: 4)
                          : null,
                      trailing: Text(context.money(item.spent), style: Theme.of(context).textTheme.titleSmall),
                    ),
                ],
              ),
            ),
    );
  }
}
