// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'Mizan';

  @override
  String get tagline => 'Know where your salary goes';

  @override
  String get login => 'Log in';

  @override
  String get register => 'Create account';

  @override
  String get logout => 'Log out';

  @override
  String get name => 'Name';

  @override
  String get email => 'Email';

  @override
  String get password => 'Password';

  @override
  String get monthlyIncome => 'Monthly income (SAR)';

  @override
  String get periodStartDay => 'Salary day';

  @override
  String get periodStartDayHint =>
      'Your budget period starts on this day each month';

  @override
  String get noAccount => 'Don\'t have an account? Create one';

  @override
  String get haveAccount => 'Already have an account? Log in';

  @override
  String get fieldRequired => 'This field is required';

  @override
  String get invalidEmail => 'Enter a valid email';

  @override
  String passwordTooShort(int min) {
    return 'At least $min characters';
  }

  @override
  String get invalidAmount =>
      'Enter an amount greater than zero (up to 2 decimals)';

  @override
  String get networkError => 'Can\'t reach the server. Check your connection.';

  @override
  String get unexpectedError => 'Something went wrong. Please try again.';

  @override
  String get sessionExpired => 'Your session has ended. Please log in again.';

  @override
  String welcome(String name) {
    return 'Welcome, $name';
  }

  @override
  String get language => 'العربية';

  @override
  String get retry => 'Retry';

  @override
  String periodRange(String start, String end) {
    return '$start – $end';
  }

  @override
  String daysLeft(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count days left',
      one: '1 day left',
      zero: 'Last day of the period',
    );
    return '$_temp0';
  }

  @override
  String get spent => 'Spent';

  @override
  String get available => 'Available';

  @override
  String budgetUsed(String percent) {
    return '$percent% of your budget used';
  }

  @override
  String get noLimitsYet =>
      'Set limits for your categories to track your budget';

  @override
  String get topSpending => 'Top spending';

  @override
  String get noSpendingYet => 'No spending yet this period';

  @override
  String get goals => 'Goals';

  @override
  String get noGoals => 'No goals yet';

  @override
  String saveEachPeriod(String amount) {
    return 'Save $amount per month';
  }

  @override
  String goalBy(String date) {
    return 'by $date';
  }

  @override
  String get goalOverdue => 'Target date passed';

  @override
  String get insights => 'Insights';

  @override
  String get allOnTrack => 'You\'re on track. No warnings this period.';

  @override
  String get notifications => 'Notifications';

  @override
  String get income => 'Income';

  @override
  String get tabHome => 'Home';

  @override
  String get tabExpenses => 'Expenses';

  @override
  String get addExpense => 'Add expense';

  @override
  String get editExpense => 'Edit expense';

  @override
  String get amount => 'Amount (SAR)';

  @override
  String get category => 'Category';

  @override
  String get date => 'Date';

  @override
  String get merchantOptional => 'Merchant (optional)';

  @override
  String get save => 'Save';

  @override
  String get delete => 'Delete';

  @override
  String get cancel => 'Cancel';

  @override
  String get deleteExpenseQuestion => 'Delete this expense?';

  @override
  String get expenseSaved => 'Expense saved';

  @override
  String get expenseDeleted => 'Expense deleted';

  @override
  String get noExpenses => 'You haven\'t recorded any expenses yet';

  @override
  String get today => 'Today';

  @override
  String get yesterday => 'Yesterday';

  @override
  String get categoryOther => 'Other';

  @override
  String get categorySavings => 'Savings';

  @override
  String get goalContribution => 'Goal saving';

  @override
  String get goalCategoryLocked => 'Money saved toward a goal stays in Savings';

  @override
  String get tabBudget => 'Budget';

  @override
  String get invalidLimit => 'Enter zero or more (up to 2 decimals)';

  @override
  String get planned => 'Planned';

  @override
  String get unallocated => 'Not planned yet';

  @override
  String overAllocated(String amount) {
    return 'You planned $amount more than your income';
  }

  @override
  String get addCategory => 'Add category';

  @override
  String get editCategory => 'Edit category';

  @override
  String get categoryName => 'Category name';

  @override
  String get monthlyLimit => 'Monthly limit (SAR)';

  @override
  String get monthlyLimitHint => '0 means you don\'t plan to spend on it';

  @override
  String get savingsTarget => 'Monthly saving target (SAR)';

  @override
  String spentOfLimit(String spent, String limit) {
    return '$spent of $limit';
  }

  @override
  String remainingAmount(String amount) {
    return '$amount left';
  }

  @override
  String overBy(String amount) {
    return 'Over by $amount';
  }

  @override
  String savedOfTarget(String saved, String target) {
    return 'Saved $saved of $target';
  }

  @override
  String get noLimitSet => 'No limit set';

  @override
  String deleteCategoryQuestion(String name) {
    return 'Delete \"$name\"?';
  }

  @override
  String get deleteCategoryExplain => 'Its expenses will move to \"Other\".';

  @override
  String get categorySaved => 'Category saved';

  @override
  String get categoryDeleted => 'Category deleted';

  @override
  String get systemCategoryNote =>
      'A built-in category: you can change its limit only.';

  @override
  String get tabGoals => 'Goals';

  @override
  String get addGoal => 'New goal';

  @override
  String get editGoal => 'Edit goal';

  @override
  String get goalName => 'Goal name';

  @override
  String get goalNameHint => 'e.g. Travel, Car, Emergency fund';

  @override
  String get targetAmount => 'Target amount (SAR)';

  @override
  String get initialAmount => 'Already saved (SAR)';

  @override
  String get targetDate => 'Target date';

  @override
  String get goalSaved => 'Goal saved';

  @override
  String get goalArchived => 'Goal archived';

  @override
  String get archiveGoal => 'Archive';

  @override
  String archiveGoalQuestion(String name) {
    return 'Archive \"$name\"?';
  }

  @override
  String get archiveGoalExplain =>
      'It will be hidden from your goals. The money you saved is not affected.';

  @override
  String get addSaving => 'Add saving';

  @override
  String get addSavingExplain =>
      'This amount counts as spending in your Savings category.';

  @override
  String get savingAdded => 'Saving added';

  @override
  String get goalReached => '🎉 You reached your goal!';

  @override
  String get statusCompleted => 'Completed';

  @override
  String get statusOverdue => 'Target date passed';

  @override
  String get saved => 'Saved';

  @override
  String get remainingLabel => 'Remaining';

  @override
  String get perMonth => 'Needed per month';

  @override
  String monthsLeft(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count months left',
      one: '1 month left',
      zero: 'No months left',
    );
    return '$_temp0';
  }

  @override
  String get extendDateHint => 'Move the target date to keep going';

  @override
  String get noNotifications => 'No notifications yet';

  @override
  String get notifBudgetUsedTitle => 'Budget alert';

  @override
  String notifBudgetUsed(int percent, String category) {
    return 'You\'ve used $percent% of your $category budget.';
  }

  @override
  String get notifBudgetExceededTitle => 'Budget exceeded';

  @override
  String notifBudgetExceeded(String category) {
    return 'You\'ve gone over your $category budget.';
  }

  @override
  String get notifGoalCompletedTitle => 'Goal reached';

  @override
  String notifGoalCompleted(String goal) {
    return '🎉 Congratulations! You reached your goal: $goal.';
  }

  @override
  String get notifOther => 'Notification';

  @override
  String get unread => 'Unread';

  @override
  String get settings => 'Settings';

  @override
  String get profile => 'Profile';

  @override
  String get editProfile => 'Edit profile';

  @override
  String get profileSaved => 'Profile saved';

  @override
  String get incomeChangeHint =>
      'Updates your current budget\'s income. Category limits stay the same.';

  @override
  String get salaryDayChangeHint =>
      'A new salary day starts with your next budget period.';

  @override
  String get languageSetting => 'Language';

  @override
  String get languageSystem => 'Phone';

  @override
  String get account => 'Account';

  @override
  String get deleteAccount => 'Delete account';

  @override
  String get deleteAccountQuestion => 'Delete your account?';

  @override
  String get deleteAccountExplain =>
      'This permanently deletes your account and all your data: income, budgets, expenses, goals, and notifications. It can\'t be undone.';

  @override
  String get deleteAccountConfirm => 'Delete permanently';

  @override
  String get goodMorning => 'Good morning';

  @override
  String get goodEvening => 'Good evening';

  @override
  String get safeToSpendToday => 'You can spend today';

  @override
  String get safeToSpendNone => 'Nothing left to spend this period';

  @override
  String availableOf(String amount) {
    return '$amount available';
  }

  @override
  String get budgetShort => 'of budget';

  @override
  String get whereMoneyWent => 'Where your money went';

  @override
  String get seeAll => 'See all';

  @override
  String get plannedSavingNote => 'Planned savings are set aside first';

  @override
  String get spentThisPeriod => 'Spent this period';

  @override
  String get savedForGoals => 'Saved toward your goals';

  @override
  String ofAmount(String amount) {
    return 'of $amount';
  }

  @override
  String get notifNew => 'New';

  @override
  String get notifEarlier => 'Earlier';

  @override
  String get goalDetails => 'Details';

  @override
  String get appearance => 'Appearance';

  @override
  String get themeSystem => 'Auto';

  @override
  String get themeLight => 'Light';

  @override
  String get themeDark => 'Dark';

  @override
  String get banks => 'Bank accounts';

  @override
  String get linkBank => 'Link a bank';

  @override
  String get linkBankTitle => 'Your expenses, automatically';

  @override
  String get linkBankExplain =>
      'Link your bank once and your purchases join your budget by themselves. We can only read accounts, balances and transactions — we can never move your money.';

  @override
  String get linkBankSecure =>
      'Through Lean, licensed by the Saudi Central Bank (SAMA).';

  @override
  String get linking => 'Bringing in your transactions…';

  @override
  String get linkFailed => 'The bank wasn\'t linked. You can try again.';

  @override
  String get bankLinked => 'Bank linked';

  @override
  String lastSynced(String time) {
    return 'Updated $time';
  }

  @override
  String get notSyncedYet => 'Not updated yet';

  @override
  String get syncNow => 'Refresh';

  @override
  String consentDaysLeft(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'Link ends in $count days',
      one: 'Link ends tomorrow',
    );
    return '$_temp0';
  }

  @override
  String get consentEnded => 'Link ended — link again to keep updating';

  @override
  String get relink => 'Link again';

  @override
  String get removeBank => 'Remove bank';

  @override
  String removeBankQuestion(String bank) {
    return 'Remove $bank?';
  }

  @override
  String get removeBankExplain =>
      'We\'ll stop reading this bank. What should happen to the transactions it added?';

  @override
  String get keepTransactions => 'Keep them';

  @override
  String get deleteTransactions => 'Delete them';

  @override
  String get bankRemoved => 'Bank removed';

  @override
  String get countsInBudget => 'Counts in budget';

  @override
  String get accountCurrent => 'Current account';

  @override
  String get accountSavings => 'Savings account';

  @override
  String get accountCreditCard => 'Credit card';

  @override
  String get accountChargeCard => 'Charge card';

  @override
  String get accountPrepaid => 'Prepaid card';

  @override
  String get accountEMoney => 'Wallet';

  @override
  String get accountOther => 'Other account';

  @override
  String get otherCurrencyNote => 'Other currencies don\'t count in the budget';

  @override
  String get reviewTitle => 'Needs your decision';

  @override
  String reviewBanner(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count bank transactions need your decision',
      one: '1 bank transaction needs your decision',
    );
    return '$_temp0';
  }

  @override
  String get reviewEmpty => 'All clear — nothing to review';

  @override
  String get duplicateQuestion => 'Is this the purchase you recorded yourself?';

  @override
  String get duplicateExplain => 'Until you answer, it\'s counted once.';

  @override
  String get transferQuestion =>
      'Is this a transfer between your own accounts?';

  @override
  String get transferExplain =>
      'Transfers and card payments aren\'t spending, so they don\'t count until you say so.';

  @override
  String rememberTransfer(String account) {
    return 'Remember my answer for every transfer to “$account”';
  }

  @override
  String get sameAsManual => 'Same purchase';

  @override
  String get notDuplicate => 'Different';

  @override
  String get ownTransfer => 'Yes, a transfer';

  @override
  String get itsSpending => 'No, it\'s spending';

  @override
  String get fromBank => 'From the bank';

  @override
  String get youRecorded => 'You recorded';

  @override
  String get pendingLabel => 'Pending';

  @override
  String get refundLabel => 'Refund';

  @override
  String get incomingLabel => 'Money in';

  @override
  String get transferLabel => 'Transfer';

  @override
  String get excludedLabel => 'Not counted';

  @override
  String get outsideBudgetLabel => 'Not in budget';

  @override
  String get cashWithdrawal => 'Cash withdrawal';

  @override
  String get bankTransaction => 'Bank transaction';

  @override
  String get bankTransactionNote =>
      'The amount and date come from your bank. You can change the category or leave it out of your budget.';

  @override
  String rememberMerchant(String merchant) {
    return 'Always put $merchant here';
  }

  @override
  String get countInBudget => 'Count in my budget';

  @override
  String get noteLabel => 'Note';

  @override
  String bankFee(String amount) {
    return 'Includes a $amount bank fee';
  }

  @override
  String get autoCategorized => 'Category picked automatically';

  @override
  String get notifConsentExpiringTitle => 'Bank link ends soon';

  @override
  String notifConsentExpiring(String bank, String days) {
    return 'Your $bank link ends in $days days. Renew it to keep expenses coming in.';
  }

  @override
  String get notifConsentExpiredTitle => 'Bank link ended';

  @override
  String notifConsentExpired(String bank) {
    return 'Link $bank again to keep expenses coming in by themselves.';
  }

  @override
  String get notifReviewTitle => 'Transactions to review';

  @override
  String notifReview(String count) {
    return '$count bank transactions need your decision.';
  }

  @override
  String get bankRefreshing =>
      'Your bank is updating. New transactions will arrive shortly.';

  @override
  String outsideBudgetExplain(String account) {
    return 'This came from “$account”, which isn\'t part of your budget, so it doesn\'t count whatever its category.';
  }

  @override
  String get includeAccount => 'Include this account in my budget';

  @override
  String get accountIncluded => 'The account now counts in your budget';

  @override
  String get confirm => 'Confirm';

  @override
  String get code => 'Code';

  @override
  String get invalidCode => 'Enter the 6-digit code';

  @override
  String get verifyEmail => 'Confirm your email';

  @override
  String verifyEmailExplain(String email) {
    return 'We sent a 6-digit code to $email. Enter it here. If it hasn\'t arrived, check spam or send a new one.';
  }

  @override
  String get sendCode => 'Send code';

  @override
  String get resendCode => 'Send a new code';

  @override
  String get codeSent => 'A new code is on its way';

  @override
  String get forgotPassword => 'Forgot your password?';

  @override
  String get resetPassword => 'Reset password';

  @override
  String get resetCodeSent =>
      'If this email has an account, a 6-digit code is on its way. It works for 15 minutes.';

  @override
  String get newPassword => 'New password';

  @override
  String get backToLogin => 'Back to login';

  @override
  String get verifyBeforeLink =>
      'Confirm your email first. It protects your account before linking a bank.';

  @override
  String get exportData => 'Export my data';
}
