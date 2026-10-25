import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/settings/locale_controller.dart';
import '../../l10n/app_localizations.dart';

class LanguageButton extends ConsumerWidget {
  const LanguageButton({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => TextButton(
        onPressed: () => ref.read(localeProvider.notifier).toggle(Localizations.localeOf(context)),
        child: Text(AppLocalizations.of(context).language),
      );
}
