import 'package:flutter/material.dart';

import '../theme/mizan_colors.dart';

// The mark: a balance scale (ميزان) in a mint rounded square.
class MizanLogo extends StatelessWidget {
  const MizanLogo({super.key, this.size = 56});

  final double size;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: colors.mint,
        borderRadius: BorderRadius.circular(size * 0.3),
        boxShadow: [BoxShadow(color: colors.mint.withValues(alpha: 0.35), blurRadius: 24, offset: const Offset(0, 8))],
      ),
      child: Icon(Icons.balance_rounded, color: colors.ink, size: size * 0.56),
    );
  }
}
