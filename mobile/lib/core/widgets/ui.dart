import 'package:flutter/material.dart';

import '../theme/mizan_colors.dart';

// Small building blocks shared by every screen.

class SectionHeader extends StatelessWidget {
  const SectionHeader(this.title, {super.key, this.action});

  final String title;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 28, bottom: 12),
        child: Row(children: [
          Expanded(child: Text(title, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontSize: 19))),
          ?action,
        ]),
      );
}

// A round, softly tinted icon button, used in screen headers.
class SoftIconButton extends StatelessWidget {
  const SoftIconButton({super.key, required this.icon, required this.onPressed, this.tooltip, this.badge = 0});

  final IconData icon;
  final VoidCallback? onPressed;
  final String? tooltip;
  final int badge;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Tooltip(
      message: tooltip ?? '',
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        shape: CircleBorder(side: BorderSide(color: colors.border)),
        child: InkWell(
          customBorder: const CircleBorder(),
          onTap: onPressed,
          child: SizedBox.square(
            dimension: 46,
            child: Badge.count(
              count: badge,
              isLabelVisible: badge > 0,
              backgroundColor: colors.danger,
              child: Icon(icon, size: 22),
            ),
          ),
        ),
      ),
    );
  }
}

// A friendly empty state: an icon in a mint circle, a sentence, and an optional action.
class EmptyState extends StatelessWidget {
  const EmptyState({super.key, required this.icon, required this.message, this.action});

  final IconData icon;
  final String message;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        Container(
          width: 64,
          height: 64,
          decoration: BoxDecoration(color: colors.mint.withValues(alpha: 0.15), shape: BoxShape.circle),
          child: Icon(icon, color: colors.mintOnLight, size: 30),
        ),
        const SizedBox(height: 12),
        Text(message, textAlign: TextAlign.center, style: TextStyle(color: colors.textMuted)),
        if (action != null) ...[const SizedBox(height: 16), action!],
      ]),
    );
  }
}

// A rounded square with a tinted background behind an icon (categories, notifications, goals).
class IconTile extends StatelessWidget {
  const IconTile({super.key, required this.icon, required this.color, this.size = 44});

  final IconData icon;
  final Color color;
  final double size;

  @override
  Widget build(BuildContext context) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.14),
          borderRadius: BorderRadius.circular(size * 0.32),
        ),
        child: Icon(icon, color: color, size: size * 0.52),
      );
}
