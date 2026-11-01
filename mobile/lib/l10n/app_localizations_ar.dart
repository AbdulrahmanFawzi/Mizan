// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appName => 'ميزان';

  @override
  String get tagline => 'اعرف وين يروح راتبك';

  @override
  String get login => 'تسجيل الدخول';

  @override
  String get register => 'إنشاء حساب';

  @override
  String get logout => 'تسجيل الخروج';

  @override
  String get name => 'الاسم';

  @override
  String get email => 'البريد الإلكتروني';

  @override
  String get password => 'كلمة المرور';

  @override
  String get monthlyIncome => 'الدخل الشهري (ريال)';

  @override
  String get periodStartDay => 'يوم نزول الراتب';

  @override
  String get periodStartDayHint => 'فترة ميزانيتك تبدأ في هذا اليوم من كل شهر';

  @override
  String get noAccount => 'ما عندك حساب؟ أنشئ حساب';

  @override
  String get haveAccount => 'عندك حساب؟ سجّل دخول';

  @override
  String get fieldRequired => 'هذا الحقل مطلوب';

  @override
  String get invalidEmail => 'أدخل بريد إلكتروني صحيح';

  @override
  String passwordTooShort(int min) {
    return 'على الأقل $min أحرف';
  }

  @override
  String get invalidAmount =>
      'أدخل مبلغ أكبر من صفر (بحد أقصى منزلتين عشريتين)';

  @override
  String get networkError => 'ما قدرنا نوصل للخادم. تأكد من اتصالك.';

  @override
  String get unexpectedError => 'صار خطأ. حاول مرة ثانية.';

  @override
  String get sessionExpired => 'انتهت جلستك. سجّل دخول مرة ثانية.';

  @override
  String welcome(String name) {
    return 'أهلًا، $name';
  }

  @override
  String get language => 'English';

  @override
  String get retry => 'إعادة المحاولة';

  @override
  String periodRange(String start, String end) {
    return '$start – $end';
  }

  @override
  String daysLeft(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'باقي $count يوم',
      few: 'باقي $count أيام',
      two: 'باقي يومين',
      one: 'باقي يوم واحد',
      zero: 'آخر يوم في الفترة',
    );
    return '$_temp0';
  }

  @override
  String get spent => 'المصروف';

  @override
  String get available => 'المتاح';

  @override
  String budgetUsed(String percent) {
    return 'استخدمت $percent% من ميزانيتك';
  }

  @override
  String get noLimitsYet => 'حدد ميزانية لفئاتك عشان تتابع صرفك';

  @override
  String get topSpending => 'أعلى المصروفات';

  @override
  String get noSpendingYet => 'ما فيه مصروفات في هذه الفترة';

  @override
  String get goals => 'الأهداف';

  @override
  String get noGoals => 'ما عندك أهداف للحين';

  @override
  String saveEachPeriod(String amount) {
    return 'ادخر $amount شهريًا';
  }

  @override
  String goalBy(String date) {
    return 'قبل $date';
  }

  @override
  String get goalOverdue => 'فات التاريخ المستهدف';

  @override
  String get insights => 'ملاحظات ذكية';

  @override
  String get allOnTrack => 'أمورك تمام. ما فيه تنبيهات في هذه الفترة.';

  @override
  String get notifications => 'الإشعارات';

  @override
  String get income => 'الدخل';

  @override
  String get tabHome => 'الرئيسية';

  @override
  String get tabExpenses => 'المصروفات';

  @override
  String get addExpense => 'إضافة مصروف';

  @override
  String get editExpense => 'تعديل المصروف';

  @override
  String get amount => 'المبلغ (ريال)';

  @override
  String get category => 'الفئة';

  @override
  String get date => 'التاريخ';

  @override
  String get merchantOptional => 'التاجر (اختياري)';

  @override
  String get save => 'حفظ';

  @override
  String get delete => 'حذف';

  @override
  String get cancel => 'إلغاء';

  @override
  String get deleteExpenseQuestion => 'تحذف هذا المصروف؟';

  @override
  String get expenseSaved => 'تم حفظ المصروف';

  @override
  String get expenseDeleted => 'تم حذف المصروف';

  @override
  String get noExpenses => 'ما سجلت أي مصروف للحين';

  @override
  String get today => 'اليوم';

  @override
  String get yesterday => 'أمس';

  @override
  String get categoryOther => 'أخرى';

  @override
  String get categorySavings => 'الادخار';

  @override
  String get goalContribution => 'ادخار لهدف';

  @override
  String get goalCategoryLocked => 'مبالغ الادخار للأهداف تبقى في فئة الادخار';

  @override
  String get tabBudget => 'الميزانية';

  @override
  String get invalidLimit => 'أدخل صفر أو أكثر (بحد أقصى منزلتين عشريتين)';

  @override
  String get planned => 'المخطط';

  @override
  String get unallocated => 'غير مخطط له';

  @override
  String overAllocated(String amount) {
    return 'خططت أكثر من دخلك بـ $amount';
  }

  @override
  String get addCategory => 'إضافة فئة';

  @override
  String get editCategory => 'تعديل الفئة';

  @override
  String get categoryName => 'اسم الفئة';

  @override
  String get monthlyLimit => 'الحد الشهري (ريال)';

  @override
  String get monthlyLimitHint => 'صفر يعني ما تخطط تصرف عليها';

  @override
  String get savingsTarget => 'هدف الادخار الشهري (ريال)';

  @override
  String spentOfLimit(String spent, String limit) {
    return '$spent من $limit';
  }

  @override
  String remainingAmount(String amount) {
    return 'باقي $amount';
  }

  @override
  String overBy(String amount) {
    return 'متجاوز بـ $amount';
  }

  @override
  String savedOfTarget(String saved, String target) {
    return 'ادخرت $saved من $target';
  }

  @override
  String get noLimitSet => 'بدون حد';

  @override
  String deleteCategoryQuestion(String name) {
    return 'تحذف فئة \"$name\"؟';
  }

  @override
  String get deleteCategoryExplain => 'مصروفاتها راح تنتقل لفئة \"أخرى\".';

  @override
  String get categorySaved => 'تم حفظ الفئة';

  @override
  String get categoryDeleted => 'تم حذف الفئة';

  @override
  String get systemCategoryNote => 'فئة أساسية: تقدر تعدّل حدها فقط.';

  @override
  String get tabGoals => 'الأهداف';

  @override
  String get addGoal => 'هدف جديد';

  @override
  String get editGoal => 'تعديل الهدف';

  @override
  String get goalName => 'اسم الهدف';

  @override
  String get goalNameHint => 'مثلًا: السفر، سيارة، صندوق طوارئ';

  @override
  String get targetAmount => 'المبلغ المستهدف (ريال)';

  @override
  String get initialAmount => 'مدّخر حاليًا (ريال)';

  @override
  String get targetDate => 'التاريخ المستهدف';

  @override
  String get goalSaved => 'تم حفظ الهدف';

  @override
  String get goalArchived => 'تمت أرشفة الهدف';

  @override
  String get archiveGoal => 'أرشفة';

  @override
  String archiveGoalQuestion(String name) {
    return 'تأرشف \"$name\"؟';
  }

  @override
  String get archiveGoalExplain =>
      'راح يختفي من أهدافك. المبالغ اللي ادخرتها ما تتأثر.';

  @override
  String get addSaving => 'إضافة ادخار';

  @override
  String get addSavingExplain => 'هذا المبلغ يُحسب مصروفًا من فئة الادخار.';

  @override
  String get savingAdded => 'تمت إضافة الادخار';

  @override
  String get goalReached => '🎉 مبروك! وصلت لهدفك';

  @override
  String get statusCompleted => 'مكتمل';

  @override
  String get statusOverdue => 'فات التاريخ المستهدف';

  @override
  String get saved => 'المدّخر';

  @override
  String get remainingLabel => 'المتبقي';

  @override
  String get perMonth => 'المطلوب شهريًا';

  @override
  String monthsLeft(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'باقي $count شهر',
      few: 'باقي $count أشهر',
      two: 'باقي شهرين',
      one: 'باقي شهر واحد',
      zero: 'ما بقي أشهر',
    );
    return '$_temp0';
  }

  @override
  String get extendDateHint => 'مدّد التاريخ المستهدف عشان تكمل';

  @override
  String get noNotifications => 'ما عندك إشعارات للحين';

  @override
  String get notifBudgetUsedTitle => 'تنبيه الميزانية';

  @override
  String notifBudgetUsed(int percent, String category) {
    return 'استخدمت $percent% من ميزانية $category.';
  }

  @override
  String get notifBudgetExceededTitle => 'تجاوز الميزانية';

  @override
  String notifBudgetExceeded(String category) {
    return 'تجاوزت ميزانية $category.';
  }

  @override
  String get notifGoalCompletedTitle => 'هدف مكتمل';

  @override
  String notifGoalCompleted(String goal) {
    return '🎉 مبروك! وصلت لهدفك: $goal.';
  }

  @override
  String get notifOther => 'إشعار';

  @override
  String get unread => 'غير مقروء';

  @override
  String get settings => 'الإعدادات';

  @override
  String get profile => 'الملف الشخصي';

  @override
  String get editProfile => 'تعديل الملف الشخصي';

  @override
  String get profileSaved => 'تم حفظ الملف الشخصي';

  @override
  String get incomeChangeHint =>
      'يحدّث دخل ميزانيتك الحالية. حدود الفئات تبقى كما هي.';

  @override
  String get salaryDayChangeHint =>
      'يوم الراتب الجديد يبدأ من فترة ميزانيتك القادمة.';

  @override
  String get languageSetting => 'اللغة';

  @override
  String get languageSystem => 'لغة الجهاز';

  @override
  String get account => 'الحساب';

  @override
  String get deleteAccount => 'حذف الحساب';

  @override
  String get deleteAccountQuestion => 'تحذف حسابك؟';

  @override
  String get deleteAccountExplain =>
      'هذا يحذف حسابك وكل بياناتك نهائيًا: الدخل والميزانيات والمصروفات والأهداف والإشعارات. ما تقدر تتراجع عنه.';

  @override
  String get deleteAccountConfirm => 'احذف نهائيًا';

  @override
  String get goodMorning => 'صباح الخير';

  @override
  String get goodEvening => 'مساء الخير';

  @override
  String get safeToSpendToday => 'تقدر تصرف اليوم';

  @override
  String get safeToSpendNone => 'ما بقي شيء للصرف في هذه الفترة';

  @override
  String availableOf(String amount) {
    return 'المتاح $amount';
  }

  @override
  String get budgetShort => 'من الميزانية';

  @override
  String get whereMoneyWent => 'وين راح صرفك';

  @override
  String get seeAll => 'عرض الكل';

  @override
  String get plannedSavingNote => 'الادخار المخطط محجوز أولًا';

  @override
  String get spentThisPeriod => 'صرفك في هذه الفترة';

  @override
  String get savedForGoals => 'مدّخر لأهدافك';

  @override
  String ofAmount(String amount) {
    return 'من $amount';
  }

  @override
  String get notifNew => 'جديدة';

  @override
  String get notifEarlier => 'سابقة';

  @override
  String get goalDetails => 'التفاصيل';

  @override
  String get appearance => 'المظهر';

  @override
  String get themeSystem => 'تلقائي';

  @override
  String get themeLight => 'فاتح';

  @override
  String get themeDark => 'داكن';

  @override
  String get banks => 'الحسابات البنكية';

  @override
  String get linkBank => 'اربط بنكك';

  @override
  String get linkBankTitle => 'مصروفاتك تدخل تلقائيًا';

  @override
  String get linkBankExplain =>
      'اربط بنكك مرة وحدة، ومشترياتك تنضاف لميزانيتك بنفسها. نقدر نقرأ الحسابات والأرصدة والعمليات فقط، وما نقدر نحرّك فلوسك أبدًا.';

  @override
  String get linkBankSecure =>
      'عن طريق Lean المرخّصة من البنك المركزي السعودي (ساما).';

  @override
  String get linking => 'نجيب عملياتك…';

  @override
  String get linkFailed => 'ما تم الربط. تقدر تحاول مرة ثانية.';

  @override
  String get bankLinked => 'تم ربط البنك';

  @override
  String lastSynced(String time) {
    return 'آخر تحديث $time';
  }

  @override
  String get notSyncedYet => 'لم يُحدّث بعد';

  @override
  String get syncNow => 'تحديث';

  @override
  String consentDaysLeft(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'الربط ينتهي بعد $count يوم',
      one: 'الربط ينتهي بكرة',
    );
    return '$_temp0';
  }

  @override
  String get consentEnded => 'انتهى الربط، أعد الربط عشان يستمر التحديث';

  @override
  String get relink => 'أعد الربط';

  @override
  String get removeBank => 'إلغاء الربط';

  @override
  String removeBankQuestion(String bank) {
    return 'إلغاء ربط $bank؟';
  }

  @override
  String get removeBankExplain =>
      'بنوقف قراءة هذا البنك. وش تبغى نسوي بالعمليات اللي أضافها؟';

  @override
  String get keepTransactions => 'احتفظ بها';

  @override
  String get deleteTransactions => 'احذفها';

  @override
  String get bankRemoved => 'تم إلغاء الربط';

  @override
  String get countsInBudget => 'يدخل الميزانية';

  @override
  String get accountCurrent => 'حساب جاري';

  @override
  String get accountSavings => 'حساب ادخار';

  @override
  String get accountCreditCard => 'بطاقة ائتمانية';

  @override
  String get accountChargeCard => 'بطاقة مسبقة السداد';

  @override
  String get accountPrepaid => 'بطاقة مسبقة الدفع';

  @override
  String get accountEMoney => 'محفظة';

  @override
  String get accountOther => 'حساب آخر';

  @override
  String get otherCurrencyNote => 'العملات الأخرى ما تدخل الميزانية';

  @override
  String get reviewTitle => 'تحتاج قرارك';

  @override
  String reviewBanner(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count عمليات بنكية تحتاج قرارك',
      two: 'عمليتين بنكيتين تحتاج قرارك',
      one: 'عملية بنكية تحتاج قرارك',
    );
    return '$_temp0';
  }

  @override
  String get reviewEmpty => 'كل شيء واضح، ما فيه شيء للمراجعة';

  @override
  String get duplicateQuestion => 'هذي نفس العملية اللي سجلتها بنفسك؟';

  @override
  String get duplicateExplain => 'لين تجاوب، تنحسب مرة وحدة.';

  @override
  String get transferQuestion => 'هذا تحويل بين حساباتك؟';

  @override
  String get transferExplain =>
      'التحويلات وسداد البطاقة مو صرف، فما تنحسب لين تقول.';

  @override
  String rememberTransfer(String account) {
    return 'تذكّر جوابي لكل التحويلات إلى «$account»';
  }

  @override
  String get sameAsManual => 'نفس العملية';

  @override
  String get notDuplicate => 'عملية مختلفة';

  @override
  String get ownTransfer => 'نعم، تحويل';

  @override
  String get itsSpending => 'لا، صرف';

  @override
  String get fromBank => 'من البنك';

  @override
  String get youRecorded => 'سجلته أنت';

  @override
  String get pendingLabel => 'معلّقة';

  @override
  String get refundLabel => 'مسترجع';

  @override
  String get incomingLabel => 'مبلغ داخل';

  @override
  String get transferLabel => 'تحويل';

  @override
  String get excludedLabel => 'غير محسوبة';

  @override
  String get outsideBudgetLabel => 'خارج الميزانية';

  @override
  String get cashWithdrawal => 'سحب نقدي';

  @override
  String get bankTransaction => 'عملية بنكية';

  @override
  String get bankTransactionNote =>
      'المبلغ والتاريخ من بنكك. تقدر تغيّر الفئة أو تستبعدها من ميزانيتك.';

  @override
  String rememberMerchant(String merchant) {
    return 'دائمًا صنّف $merchant هنا';
  }

  @override
  String get countInBudget => 'احسبها في ميزانيتي';

  @override
  String get noteLabel => 'ملاحظة';

  @override
  String bankFee(String amount) {
    return 'تشمل رسوم بنكية $amount';
  }

  @override
  String get autoCategorized => 'الفئة مختارة تلقائيًا';

  @override
  String get notifConsentExpiringTitle => 'ربط البنك ينتهي قريبًا';

  @override
  String notifConsentExpiring(String bank, String days) {
    return 'ربط $bank ينتهي بعد $days يوم. جدّده عشان تستمر مصروفاتك تدخل.';
  }

  @override
  String get notifConsentExpiredTitle => 'انتهى ربط البنك';

  @override
  String notifConsentExpired(String bank) {
    return 'أعد ربط $bank عشان ترجع المصروفات تدخل بنفسها.';
  }

  @override
  String get notifReviewTitle => 'عمليات تحتاج مراجعتك';

  @override
  String notifReview(String count) {
    return 'عندك $count عمليات من البنك تحتاج قرارك.';
  }

  @override
  String get bankRefreshing =>
      'بنكك يتحدّث الحين، والعمليات الجديدة توصل بعد شوي.';

  @override
  String outsideBudgetExplain(String account) {
    return 'هذي العملية من «$account» وهو خارج ميزانيتك، فما تنحسب مهما كانت فئتها.';
  }

  @override
  String get includeAccount => 'أدخل هذا الحساب في ميزانيتي';

  @override
  String get accountIncluded => 'صار الحساب يدخل ميزانيتك';

  @override
  String get confirm => 'تأكيد';

  @override
  String get code => 'الرمز';

  @override
  String get invalidCode => 'اكتب الرمز المكوّن من 6 أرقام';

  @override
  String get verifyEmail => 'تأكيد الإيميل';

  @override
  String verifyEmailExplain(String email) {
    return 'أرسلنا رمز من 6 أرقام إلى $email. اكتبه هنا. لو ما وصل، شيّك على البريد المزعج أو اطلب رمز جديد.';
  }

  @override
  String get sendCode => 'أرسل الرمز';

  @override
  String get resendCode => 'أرسل رمز جديد';

  @override
  String get codeSent => 'أرسلنا رمز جديد';

  @override
  String get forgotPassword => 'نسيت كلمة المرور؟';

  @override
  String get resetPassword => 'تغيير كلمة المرور';

  @override
  String get resetCodeSent =>
      'إذا هذا الإيميل له حساب، بيوصلك رمز من 6 أرقام. يشتغل لمدة 15 دقيقة.';

  @override
  String get newPassword => 'كلمة المرور الجديدة';

  @override
  String get backToLogin => 'رجوع لتسجيل الدخول';

  @override
  String get verifyBeforeLink =>
      'أكّد إيميلك أول. هذا يحمي حسابك قبل ربط البنك.';

  @override
  String get exportData => 'تصدير بياناتي';
}
