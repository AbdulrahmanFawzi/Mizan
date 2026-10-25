import 'package:flutter/material.dart';
import 'package:intl/intl.dart' show Bidi;

import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/ui.dart';
import '../../data/dashboard.dart';

class InsightCard extends StatelessWidget {
  const InsightCard({super.key, required this.insight});

  final Insight insight;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    final over = insight.type == InsightType.overBudget;
    final color = over ? colors.danger : colors.warning;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(children: [
          IconTile(icon: over ? Icons.error_outline_rounded : Icons.trending_up_rounded, color: color, size: 40),
          const SizedBox(width: 12),
          Expanded(
            // The API writes the sentence (Arabic for now), so its direction comes from the text itself.
            child: Text(
              insight.message,
              textDirection: Bidi.detectRtlDirectionality(insight.message) ? TextDirection.rtl : TextDirection.ltr,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ),
        ]),
      ),
    );
  }
}
