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

// The top of a tab screen: a big title, an optional line under it, and an action on the side.
// Tab screens use this instead of an AppBar so they feel like the dashboard.
class PageHeader extends StatelessWidget {
  const PageHeader({super.key, required this.title, this.subtitle, this.action});

  final String title;
  final String? subtitle;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 20),
        child: Row(children: [
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(title, style: Theme.of(context).textTheme.headlineSmall),
              if (subtitle != null) Text(subtitle!, style: TextStyle(color: context.mizan.textMuted)),
            ]),
          ),
          ?action,
        ]),
      );
}

// A thick rounded progress bar that animates to its value.
class ProgressBar extends StatelessWidget {
  const ProgressBar({super.key, required this.value, required this.color, this.height = 10});

  final double value;
  final Color color;
  final double height;

  @override
  Widget build(BuildContext context) => ClipRRect(
        borderRadius: BorderRadius.circular(height),
        child: TweenAnimationBuilder<double>(
          tween: Tween(begin: 0, end: value.clamp(0, 1)),
          duration: const Duration(milliseconds: 700),
          curve: Curves.easeOutCubic,
          builder: (_, animated, _) => LinearProgressIndicator(
            value: animated,
            minHeight: height,
            color: color,
            backgroundColor: color.withValues(alpha: 0.12),
          ),
        ),
      );
}

// A tappable row that looks like a form field: used for pickers (date, salary day).
class PickerTile extends StatelessWidget {
  const PickerTile({super.key, required this.icon, required this.label, required this.value, this.onTap});

  final IconData icon;
  final String label;
  final String value;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final colors = context.mizan;
    return Material(
      color: colors.fieldFill,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          child: Row(children: [
            IconTile(icon: icon, color: colors.mintOnLight, size: 38),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(label, style: TextStyle(color: colors.textMuted, fontSize: 12)),
                Text(value, style: Theme.of(context).textTheme.titleSmall),
              ]),
            ),
            Icon(Icons.chevron_right_rounded, color: colors.textMuted, textDirection: Directionality.of(context)),
          ]),
        ),
      ),
    );
  }
}

// A small label above a group of fields inside a form.
class FieldLabel extends StatelessWidget {
  const FieldLabel(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsetsDirectional.only(start: 4, bottom: 8, top: 20),
        child: Text(text, style: Theme.of(context).textTheme.labelLarge?.copyWith(color: context.mizan.textMuted)),
      );
}
