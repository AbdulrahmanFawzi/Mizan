import 'package:flutter/material.dart';
import 'package:intl/intl.dart' show Bidi;

import '../../../../l10n/app_localizations.dart';
import '../../data/dashboard.dart';
import 'section.dart';

class InsightsSection extends StatelessWidget {
  const InsightsSection({super.key, required this.insights});

  final List<Insight> insights;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = Theme.of(context).colorScheme;

    return Section(
      title: l10n.insights,
      child: insights.isEmpty
          ? EmptyHint(l10n.allOnTrack, icon: Icons.check_circle_outline)
          : Column(
              children: [
                for (final insight in insights)
                  Card.outlined(
                    child: ListTile(
                      leading: Icon(
                        insight.type == InsightType.overBudget ? Icons.error_outline : Icons.trending_up,
                        color: insight.type == InsightType.overBudget ? colors.error : Colors.orange,
                      ),
                      // The API writes the sentence (Arabic for now), so its direction comes from
                      // the text itself, not from the app's language.
                      title: Text(
                        insight.message,
                        textDirection: Bidi.detectRtlDirectionality(insight.message)
                            ? TextDirection.rtl
                            : TextDirection.ltr,
                      ),
                    ),
                  ),
              ],
            ),
    );
  }
}
