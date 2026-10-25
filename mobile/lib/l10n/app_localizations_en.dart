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
}
