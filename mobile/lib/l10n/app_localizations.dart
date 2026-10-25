import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en'),
  ];

  /// No description provided for @appName.
  ///
  /// In en, this message translates to:
  /// **'Mizan'**
  String get appName;

  /// No description provided for @tagline.
  ///
  /// In en, this message translates to:
  /// **'Know where your salary goes'**
  String get tagline;

  /// No description provided for @login.
  ///
  /// In en, this message translates to:
  /// **'Log in'**
  String get login;

  /// No description provided for @register.
  ///
  /// In en, this message translates to:
  /// **'Create account'**
  String get register;

  /// No description provided for @logout.
  ///
  /// In en, this message translates to:
  /// **'Log out'**
  String get logout;

  /// No description provided for @name.
  ///
  /// In en, this message translates to:
  /// **'Name'**
  String get name;

  /// No description provided for @email.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get email;

  /// No description provided for @password.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get password;

  /// No description provided for @monthlyIncome.
  ///
  /// In en, this message translates to:
  /// **'Monthly income (SAR)'**
  String get monthlyIncome;

  /// No description provided for @periodStartDay.
  ///
  /// In en, this message translates to:
  /// **'Salary day'**
  String get periodStartDay;

  /// No description provided for @periodStartDayHint.
  ///
  /// In en, this message translates to:
  /// **'Your budget period starts on this day each month'**
  String get periodStartDayHint;

  /// No description provided for @noAccount.
  ///
  /// In en, this message translates to:
  /// **'Don\'t have an account? Create one'**
  String get noAccount;

  /// No description provided for @haveAccount.
  ///
  /// In en, this message translates to:
  /// **'Already have an account? Log in'**
  String get haveAccount;

  /// No description provided for @fieldRequired.
  ///
  /// In en, this message translates to:
  /// **'This field is required'**
  String get fieldRequired;

  /// No description provided for @invalidEmail.
  ///
  /// In en, this message translates to:
  /// **'Enter a valid email'**
  String get invalidEmail;

  /// No description provided for @passwordTooShort.
  ///
  /// In en, this message translates to:
  /// **'At least {min} characters'**
  String passwordTooShort(int min);

  /// No description provided for @invalidAmount.
  ///
  /// In en, this message translates to:
  /// **'Enter an amount greater than zero (up to 2 decimals)'**
  String get invalidAmount;

  /// No description provided for @networkError.
  ///
  /// In en, this message translates to:
  /// **'Can\'t reach the server. Check your connection.'**
  String get networkError;

  /// No description provided for @unexpectedError.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get unexpectedError;

  /// No description provided for @sessionExpired.
  ///
  /// In en, this message translates to:
  /// **'Your session has ended. Please log in again.'**
  String get sessionExpired;

  /// No description provided for @welcome.
  ///
  /// In en, this message translates to:
  /// **'Welcome, {name}'**
  String welcome(String name);

  /// No description provided for @language.
  ///
  /// In en, this message translates to:
  /// **'العربية'**
  String get language;

  /// No description provided for @retry.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get retry;

  /// No description provided for @periodRange.
  ///
  /// In en, this message translates to:
  /// **'{start} – {end}'**
  String periodRange(String start, String end);

  /// No description provided for @daysLeft.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =0{Last day of the period} =1{1 day left} other{{count} days left}}'**
  String daysLeft(int count);

  /// No description provided for @spent.
  ///
  /// In en, this message translates to:
  /// **'Spent'**
  String get spent;

  /// No description provided for @available.
  ///
  /// In en, this message translates to:
  /// **'Available'**
  String get available;

  /// No description provided for @budgetUsed.
  ///
  /// In en, this message translates to:
  /// **'{percent}% of your budget used'**
  String budgetUsed(String percent);

  /// No description provided for @noLimitsYet.
  ///
  /// In en, this message translates to:
  /// **'Set limits for your categories to track your budget'**
  String get noLimitsYet;

  /// No description provided for @topSpending.
  ///
  /// In en, this message translates to:
  /// **'Top spending'**
  String get topSpending;

  /// No description provided for @noSpendingYet.
  ///
  /// In en, this message translates to:
  /// **'No spending yet this period'**
  String get noSpendingYet;

  /// No description provided for @goals.
  ///
  /// In en, this message translates to:
  /// **'Goals'**
  String get goals;

  /// No description provided for @noGoals.
  ///
  /// In en, this message translates to:
  /// **'No goals yet'**
  String get noGoals;

  /// No description provided for @saveEachPeriod.
  ///
  /// In en, this message translates to:
  /// **'Save {amount} per month'**
  String saveEachPeriod(String amount);

  /// No description provided for @goalBy.
  ///
  /// In en, this message translates to:
  /// **'by {date}'**
  String goalBy(String date);

  /// No description provided for @goalOverdue.
  ///
  /// In en, this message translates to:
  /// **'Target date passed'**
  String get goalOverdue;

  /// No description provided for @insights.
  ///
  /// In en, this message translates to:
  /// **'Insights'**
  String get insights;

  /// No description provided for @allOnTrack.
  ///
  /// In en, this message translates to:
  /// **'You\'re on track. No warnings this period.'**
  String get allOnTrack;

  /// No description provided for @notifications.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get notifications;

  /// No description provided for @income.
  ///
  /// In en, this message translates to:
  /// **'Income'**
  String get income;

  /// No description provided for @tabHome.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get tabHome;

  /// No description provided for @tabExpenses.
  ///
  /// In en, this message translates to:
  /// **'Expenses'**
  String get tabExpenses;

  /// No description provided for @addExpense.
  ///
  /// In en, this message translates to:
  /// **'Add expense'**
  String get addExpense;

  /// No description provided for @editExpense.
  ///
  /// In en, this message translates to:
  /// **'Edit expense'**
  String get editExpense;

  /// No description provided for @amount.
  ///
  /// In en, this message translates to:
  /// **'Amount (SAR)'**
  String get amount;

  /// No description provided for @category.
  ///
  /// In en, this message translates to:
  /// **'Category'**
  String get category;

  /// No description provided for @date.
  ///
  /// In en, this message translates to:
  /// **'Date'**
  String get date;

  /// No description provided for @merchantOptional.
  ///
  /// In en, this message translates to:
  /// **'Merchant (optional)'**
  String get merchantOptional;

  /// No description provided for @save.
  ///
  /// In en, this message translates to:
  /// **'Save'**
  String get save;

  /// No description provided for @delete.
  ///
  /// In en, this message translates to:
  /// **'Delete'**
  String get delete;

  /// No description provided for @cancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get cancel;

  /// No description provided for @deleteExpenseQuestion.
  ///
  /// In en, this message translates to:
  /// **'Delete this expense?'**
  String get deleteExpenseQuestion;

  /// No description provided for @expenseSaved.
  ///
  /// In en, this message translates to:
  /// **'Expense saved'**
  String get expenseSaved;

  /// No description provided for @expenseDeleted.
  ///
  /// In en, this message translates to:
  /// **'Expense deleted'**
  String get expenseDeleted;

  /// No description provided for @noExpenses.
  ///
  /// In en, this message translates to:
  /// **'You haven\'t recorded any expenses yet'**
  String get noExpenses;

  /// No description provided for @today.
  ///
  /// In en, this message translates to:
  /// **'Today'**
  String get today;

  /// No description provided for @yesterday.
  ///
  /// In en, this message translates to:
  /// **'Yesterday'**
  String get yesterday;

  /// No description provided for @categoryOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get categoryOther;

  /// No description provided for @categorySavings.
  ///
  /// In en, this message translates to:
  /// **'Savings'**
  String get categorySavings;

  /// No description provided for @goalContribution.
  ///
  /// In en, this message translates to:
  /// **'Goal saving'**
  String get goalContribution;

  /// No description provided for @goalCategoryLocked.
  ///
  /// In en, this message translates to:
  /// **'Money saved toward a goal stays in Savings'**
  String get goalCategoryLocked;

  /// No description provided for @tabBudget.
  ///
  /// In en, this message translates to:
  /// **'Budget'**
  String get tabBudget;

  /// No description provided for @invalidLimit.
  ///
  /// In en, this message translates to:
  /// **'Enter zero or more (up to 2 decimals)'**
  String get invalidLimit;

  /// No description provided for @planned.
  ///
  /// In en, this message translates to:
  /// **'Planned'**
  String get planned;

  /// No description provided for @unallocated.
  ///
  /// In en, this message translates to:
  /// **'Not planned yet'**
  String get unallocated;

  /// No description provided for @overAllocated.
  ///
  /// In en, this message translates to:
  /// **'You planned {amount} more than your income'**
  String overAllocated(String amount);

  /// No description provided for @addCategory.
  ///
  /// In en, this message translates to:
  /// **'Add category'**
  String get addCategory;

  /// No description provided for @editCategory.
  ///
  /// In en, this message translates to:
  /// **'Edit category'**
  String get editCategory;

  /// No description provided for @categoryName.
  ///
  /// In en, this message translates to:
  /// **'Category name'**
  String get categoryName;

  /// No description provided for @monthlyLimit.
  ///
  /// In en, this message translates to:
  /// **'Monthly limit (SAR)'**
  String get monthlyLimit;

  /// No description provided for @monthlyLimitHint.
  ///
  /// In en, this message translates to:
  /// **'0 means you don\'t plan to spend on it'**
  String get monthlyLimitHint;

  /// No description provided for @savingsTarget.
  ///
  /// In en, this message translates to:
  /// **'Monthly saving target (SAR)'**
  String get savingsTarget;

  /// No description provided for @spentOfLimit.
  ///
  /// In en, this message translates to:
  /// **'{spent} of {limit}'**
  String spentOfLimit(String spent, String limit);

  /// No description provided for @remainingAmount.
  ///
  /// In en, this message translates to:
  /// **'{amount} left'**
  String remainingAmount(String amount);

  /// No description provided for @overBy.
  ///
  /// In en, this message translates to:
  /// **'Over by {amount}'**
  String overBy(String amount);

  /// No description provided for @savedOfTarget.
  ///
  /// In en, this message translates to:
  /// **'Saved {saved} of {target}'**
  String savedOfTarget(String saved, String target);

  /// No description provided for @noLimitSet.
  ///
  /// In en, this message translates to:
  /// **'No limit set'**
  String get noLimitSet;

  /// No description provided for @deleteCategoryQuestion.
  ///
  /// In en, this message translates to:
  /// **'Delete \"{name}\"?'**
  String deleteCategoryQuestion(String name);

  /// No description provided for @deleteCategoryExplain.
  ///
  /// In en, this message translates to:
  /// **'Its expenses will move to \"Other\".'**
  String get deleteCategoryExplain;

  /// No description provided for @categorySaved.
  ///
  /// In en, this message translates to:
  /// **'Category saved'**
  String get categorySaved;

  /// No description provided for @categoryDeleted.
  ///
  /// In en, this message translates to:
  /// **'Category deleted'**
  String get categoryDeleted;

  /// No description provided for @systemCategoryNote.
  ///
  /// In en, this message translates to:
  /// **'A built-in category: you can change its limit only.'**
  String get systemCategoryNote;

  /// No description provided for @tabGoals.
  ///
  /// In en, this message translates to:
  /// **'Goals'**
  String get tabGoals;

  /// No description provided for @addGoal.
  ///
  /// In en, this message translates to:
  /// **'New goal'**
  String get addGoal;

  /// No description provided for @editGoal.
  ///
  /// In en, this message translates to:
  /// **'Edit goal'**
  String get editGoal;

  /// No description provided for @goalName.
  ///
  /// In en, this message translates to:
  /// **'Goal name'**
  String get goalName;

  /// No description provided for @goalNameHint.
  ///
  /// In en, this message translates to:
  /// **'e.g. Travel, Car, Emergency fund'**
  String get goalNameHint;

  /// No description provided for @targetAmount.
  ///
  /// In en, this message translates to:
  /// **'Target amount (SAR)'**
  String get targetAmount;

  /// No description provided for @initialAmount.
  ///
  /// In en, this message translates to:
  /// **'Already saved (SAR)'**
  String get initialAmount;

  /// No description provided for @targetDate.
  ///
  /// In en, this message translates to:
  /// **'Target date'**
  String get targetDate;

  /// No description provided for @goalSaved.
  ///
  /// In en, this message translates to:
  /// **'Goal saved'**
  String get goalSaved;

  /// No description provided for @goalArchived.
  ///
  /// In en, this message translates to:
  /// **'Goal archived'**
  String get goalArchived;

  /// No description provided for @archiveGoal.
  ///
  /// In en, this message translates to:
  /// **'Archive'**
  String get archiveGoal;

  /// No description provided for @archiveGoalQuestion.
  ///
  /// In en, this message translates to:
  /// **'Archive \"{name}\"?'**
  String archiveGoalQuestion(String name);

  /// No description provided for @archiveGoalExplain.
  ///
  /// In en, this message translates to:
  /// **'It will be hidden from your goals. The money you saved is not affected.'**
  String get archiveGoalExplain;

  /// No description provided for @addSaving.
  ///
  /// In en, this message translates to:
  /// **'Add saving'**
  String get addSaving;

  /// No description provided for @addSavingExplain.
  ///
  /// In en, this message translates to:
  /// **'This amount counts as spending in your Savings category.'**
  String get addSavingExplain;

  /// No description provided for @savingAdded.
  ///
  /// In en, this message translates to:
  /// **'Saving added'**
  String get savingAdded;

  /// No description provided for @goalReached.
  ///
  /// In en, this message translates to:
  /// **'🎉 You reached your goal!'**
  String get goalReached;

  /// No description provided for @statusCompleted.
  ///
  /// In en, this message translates to:
  /// **'Completed'**
  String get statusCompleted;

  /// No description provided for @statusOverdue.
  ///
  /// In en, this message translates to:
  /// **'Target date passed'**
  String get statusOverdue;

  /// No description provided for @saved.
  ///
  /// In en, this message translates to:
  /// **'Saved'**
  String get saved;

  /// No description provided for @remainingLabel.
  ///
  /// In en, this message translates to:
  /// **'Remaining'**
  String get remainingLabel;

  /// No description provided for @perMonth.
  ///
  /// In en, this message translates to:
  /// **'Needed per month'**
  String get perMonth;

  /// No description provided for @monthsLeft.
  ///
  /// In en, this message translates to:
  /// **'{count, plural, =0{No months left} =1{1 month left} other{{count} months left}}'**
  String monthsLeft(int count);

  /// No description provided for @extendDateHint.
  ///
  /// In en, this message translates to:
  /// **'Move the target date to keep going'**
  String get extendDateHint;

  /// No description provided for @noNotifications.
  ///
  /// In en, this message translates to:
  /// **'No notifications yet'**
  String get noNotifications;

  /// No description provided for @notifBudgetUsedTitle.
  ///
  /// In en, this message translates to:
  /// **'Budget alert'**
  String get notifBudgetUsedTitle;

  /// No description provided for @notifBudgetUsed.
  ///
  /// In en, this message translates to:
  /// **'You\'ve used {percent}% of your {category} budget.'**
  String notifBudgetUsed(int percent, String category);

  /// No description provided for @notifBudgetExceededTitle.
  ///
  /// In en, this message translates to:
  /// **'Budget exceeded'**
  String get notifBudgetExceededTitle;

  /// No description provided for @notifBudgetExceeded.
  ///
  /// In en, this message translates to:
  /// **'You\'ve gone over your {category} budget.'**
  String notifBudgetExceeded(String category);

  /// No description provided for @notifGoalCompletedTitle.
  ///
  /// In en, this message translates to:
  /// **'Goal reached'**
  String get notifGoalCompletedTitle;

  /// No description provided for @notifGoalCompleted.
  ///
  /// In en, this message translates to:
  /// **'🎉 Congratulations! You reached your goal: {goal}.'**
  String notifGoalCompleted(String goal);

  /// No description provided for @notifOther.
  ///
  /// In en, this message translates to:
  /// **'Notification'**
  String get notifOther;

  /// No description provided for @unread.
  ///
  /// In en, this message translates to:
  /// **'Unread'**
  String get unread;

  /// No description provided for @settings.
  ///
  /// In en, this message translates to:
  /// **'Settings'**
  String get settings;

  /// No description provided for @profile.
  ///
  /// In en, this message translates to:
  /// **'Profile'**
  String get profile;

  /// No description provided for @editProfile.
  ///
  /// In en, this message translates to:
  /// **'Edit profile'**
  String get editProfile;

  /// No description provided for @profileSaved.
  ///
  /// In en, this message translates to:
  /// **'Profile saved'**
  String get profileSaved;

  /// No description provided for @incomeChangeHint.
  ///
  /// In en, this message translates to:
  /// **'Updates your current budget\'s income. Category limits stay the same.'**
  String get incomeChangeHint;

  /// No description provided for @salaryDayChangeHint.
  ///
  /// In en, this message translates to:
  /// **'A new salary day starts with your next budget period.'**
  String get salaryDayChangeHint;

  /// No description provided for @languageSetting.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get languageSetting;

  /// No description provided for @languageSystem.
  ///
  /// In en, this message translates to:
  /// **'Phone'**
  String get languageSystem;

  /// No description provided for @account.
  ///
  /// In en, this message translates to:
  /// **'Account'**
  String get account;

  /// No description provided for @deleteAccount.
  ///
  /// In en, this message translates to:
  /// **'Delete account'**
  String get deleteAccount;

  /// No description provided for @deleteAccountQuestion.
  ///
  /// In en, this message translates to:
  /// **'Delete your account?'**
  String get deleteAccountQuestion;

  /// No description provided for @deleteAccountExplain.
  ///
  /// In en, this message translates to:
  /// **'This permanently deletes your account and all your data: income, budgets, expenses, goals, and notifications. It can\'t be undone.'**
  String get deleteAccountExplain;

  /// No description provided for @deleteAccountConfirm.
  ///
  /// In en, this message translates to:
  /// **'Delete permanently'**
  String get deleteAccountConfirm;

  /// No description provided for @goodMorning.
  ///
  /// In en, this message translates to:
  /// **'Good morning'**
  String get goodMorning;

  /// No description provided for @goodEvening.
  ///
  /// In en, this message translates to:
  /// **'Good evening'**
  String get goodEvening;

  /// No description provided for @safeToSpendToday.
  ///
  /// In en, this message translates to:
  /// **'You can spend today'**
  String get safeToSpendToday;

  /// No description provided for @safeToSpendNone.
  ///
  /// In en, this message translates to:
  /// **'Nothing left to spend this period'**
  String get safeToSpendNone;

  /// No description provided for @availableOf.
  ///
  /// In en, this message translates to:
  /// **'{amount} available'**
  String availableOf(String amount);

  /// No description provided for @budgetShort.
  ///
  /// In en, this message translates to:
  /// **'of budget'**
  String get budgetShort;

  /// No description provided for @whereMoneyWent.
  ///
  /// In en, this message translates to:
  /// **'Where your money went'**
  String get whereMoneyWent;

  /// No description provided for @seeAll.
  ///
  /// In en, this message translates to:
  /// **'See all'**
  String get seeAll;

  /// No description provided for @plannedSavingNote.
  ///
  /// In en, this message translates to:
  /// **'Planned savings are set aside first'**
  String get plannedSavingNote;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
