import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/storage/preferences.dart';

// true between signing up and finishing (or skipping) the guided setup. Saved, so closing
// the app halfway brings the user back to it.
class OnboardingController extends Notifier<bool> {
  static const _key = 'onboardingPending';

  @override
  bool build() => ref.read(sharedPreferencesProvider).getBool(_key) ?? false;

  Future<void> start() async {
    await ref.read(sharedPreferencesProvider).setBool(_key, true);
    state = true;
  }

  Future<void> finish() async {
    await ref.read(sharedPreferencesProvider).remove(_key);
    state = false;
  }
}

final onboardingProvider = NotifierProvider<OnboardingController, bool>(OnboardingController.new);
