# هللاتي — Domain Discovery (MVP 1: Open Banking)
## الخطوة 4: Aggregates و Domain Events

نفس قواعد MVP 0 (صغيرة، تشير بالـ Id، عملية واحدة تعدّل Aggregate واحد). هنا الجديد.

---

## الـ Aggregates

| Aggregate Root | يحتوي | الـ Invariants |
|---|---|---|
| **BankConnection** ✨ | BankAccount | المزامنة فقط وهو نشط (BR-C3)، حد 4 ساعات بين طلبات التحديث (BR-S2)، كل تذكير ينرسل مرة (BR-C5)، "انتهت الموافقة" تتبلّغ مرة (BR-C6)، الحساب بغير الريال ما يدخل الميزانية (BR-X3) |
| **Transaction** (موسّع) | — | البنكية: المبلغ والتاريخ ما يتعدلون وما تنحذف (BR-X4، BR-X5)، اختيارات المستخدم تبقى بعد تحديث البنك (BR-X7)، `BudgetEffect` صحيح لكل حالة (OB-10) |
| **MerchantRule** ✨ | — | التاجر صالح (MerchantKey)، والفئة موجودة |
| **Budget** (موسّع) | BudgetAllocation | يستلم **الأثر** (`BudgetEffect`) بدل المبلغ الخام، ومصروف الفئة ما ينزل تحت صفر (BR-F4) |

### قواعد تحتاج أكثر من Aggregate

| القاعدة | من يضمنها |
|---|---|
| عملية بنكية واحدة لكل معرّف مزود (BR-S5) | **Unique Index** على (ConnectionId، ProviderTransactionId) + Upsert |
| نفس البنك ما يُربط مرتين وهو نشط (BR-C2) | فحص في الـ Application + Unique Index جزئي (UserId، BankIdentifier) للنشط |
| قاعدة تاجر واحدة لكل تاجر (BR-K2) | Unique Index (UserId، Merchant) |
| مزامنتين لنفس الربط بنفس اللحظة | **قفل لكل ربط** (Postgres Advisory Lock). الجدولة، فتح التطبيق، والـ Webhook ممكن يجون مع بعض |

---

## Domain Services (منطق ما ينتمي لـ Entity واحد)

دوال **نقية** (Pure): تاخذ بيانات وترجع قرار، بدون قاعدة بيانات ولا شبكة. فيها أغلب قواعد MVP 1، وكلها تنكتب بـ TDD.

| Service | يقرر | القواعد |
|---|---|---|
| **TransactionClassifier** | الـ Treatment (صرف، استرجاع، تحويل داخلي، داخل) وهل تحتاج مراجعة | BR-X1، X10، X12، R1، R2، R5، F1–F3، F7 |
| **CategoryResolver** | الفئة ومصدرها | BR-K1، K4، K5، K6 |
| **DuplicateMatcher** | أي مصروف يدوي يشبه هذي العملية البنكية | BR-D1، D2، D6، D7 |
| **TransferPairMatcher** | لما ما فيه IBAN: أي عمليتين بين حساباتي هم نفس التحويل | BR-R1 (البديل) |
| **SalaryDetector** *(لاحقًا)* | هل هذا إيداع راتب | BR-F6 |

> **ليش Services ومو داخل Transaction؟** التصنيف يحتاج يشوف **عمليات وقواعد ثانية** (قواعد التاجر، حسابات المستخدم، المصروفات اليدوية). الـ Entity يعرف نفسه بس.

---

## Domain Events

### جديدة

| Event | يصير لما | من يسمعه ووش يسوي |
|---|---|---|
| **BankConnectionActivated** | وصل `entity.created` وفعّلنا الربط | يجدول **أول استيراد** (3 أشهر، BR-H1) |
| **BankTransactionsImported** | انتهى استيراد دفعة (أول ربط أو تحديث) | ① يقيّم عتبات **كل فئة تأثرت مرة وحدة** (BR-H3) ② ينشئ إشعار "عمليات تحتاج مراجعة" إذا فيه جديد |
| **ConsentExpiringSoon** | الفحص اليومي لقى موافقة باقي لها 14 أو 3 أيام | إشعار ConsentExpiring |
| **BankConnectionExpired** | المزامنة رجعت `CONSENT_EXPIRED`، أو Webhook `entity.updated` | إشعار ConsentExpired (مرة وحدة) |
| **BankConnectionRevoked** | المستخدم ألغى الربط | إذا اختار الحذف: تنحذف عملياته وينعاد التقييم (BR-C9) |

### موجودة، بسلوك جديد

| Event | التغيير |
|---|---|
| **TransactionRecorded** | يبقى لليدوي فقط. ومعه يشتغل **DuplicateMatcher** بالعكس: يدوي جديد يشبه عملية بنكية موجودة؟ |
| **TransactionUpdated** | يُطلق أيضًا لما: تتغير الفئة، الاستبعاد، حل المراجعة، أو تحديث من البنك غيّر المبلغ |
| **TransactionDeleted** | يُطلق أيضًا للمصروف اليدوي اللي اندمج (BR-D5) |
| **AccountDeleted** *(جديد)* | حذف الحساب اليوم دالة مباشرة (`ProfileService.DeleteAccountAsync`) بدون Event. **نضيف** هذا الحدث، ومعه يُسجَّل في الـ Outbox طلب إلغاء الموافقات وحذف الـ Customer عند Lean (BR-C10) |

> **ليش ما نستخدم TransactionRecorded لكل عملية مستوردة؟** لأن أول ربط فيه ~500 عملية. حدث لكل واحدة = 500 تقييم عتبات و **سيل تنبيهات**. حدث واحد للدفعة كلها يطبق BR-H3 بشكل طبيعي.

---

## التواصل مع Lean: ثلاث طرق، لكل وحدة سبب

| الطريقة | متى | ليش |
|---|---|---|
| **مباشر** (داخل الطلب) | إنشاء Token الربط، إلغاء الربط بطلب المستخدم | المستخدم ينتظر النتيجة، و BR-C8 يقول ما نأكد الإلغاء إلا بعد Lean |
| **Webhook** (Lean يكلمنا) | `entity.created`، `entity.data.refresh.updated` (FINISHED)، `entity.updated` | Lean يبلّغنا. نحفظ الحدث في **Inbox** ونرد 200 فورًا (Lean يعطي 10 ثواني فقط)، ونعالجه بالخلفية |
| **Outbox** (نحن نكلم Lean بالخلفية) | حذف الحساب، طلب التحديث اليومي | ما نسوي اتصال خارجي داخل Transaction قاعدة البيانات. نسجل "مطلوب" في نفس الـ Transaction، وعامل خلفي ينفذه ويعيد المحاولة |

> **Inbox و Outbox** نمطين مشهورين لما نظامين يتكلمون عبر الشبكة: الشبكة ممكن تفشل في أي لحظة، فنحفظ النية أولًا في قاعدتنا (اللي نثق فيها) ثم ننفذها.

---

## مثال كامل: المستخدم يربط بنكه أول مرة

```
التطبيق ── POST /bank-connections/link
              │  الـ Backend: ينشئ BankingCustomer (لو ما فيه) ثم Token مؤقت (BR-C12)
              ▼
        Link SDK ── المستخدم يختار البنك ويوافق
              │
              ▼  Lean ── Webhook: entity.created
        Inbox ── يُحفظ ويُرد 200
              │
              ▼  عامل خلفي
        BankConnection.Activate(entityId, consent)  ──► BankConnectionActivated
              │
              ▼  (Lean يجمع البيانات)   Webhook: entity.data.refresh.updated = FINISHED
        ImportService (بقفل الربط):
          1. يجلب الحسابات ← يحدّث BankAccount (النوع، IBAN، الرصيد)
          2. يجلب عمليات SyncWindow (أول مرة 3 أشهر) لكل حساب، ويعيد المحاولة لكل حساب لحاله
          3. لكل عملية:
               TransactionClassifier  ← صرف؟ تحويل؟ استرجاع؟
               CategoryResolver       ← قاعدة التاجر؟ Lean؟ أخرى؟
               DuplicateMatcher       ← تشبه مصروف يدوي؟ ← Review + PossibleDuplicateOf
               Upsert بالمعرّف (BR-S5)
          4. BankConnection.RecordSync(now)
              │  ──► BankTransactionsImported(isInitial: true, فئات تأثرت)
              ▼
        Handler: يقيّم كل فئة مرة وحدة ← أعلى عتبة فقط ← Notification
        Handler: 7 عمليات تحتاج مراجعة ← Notification "راجع عملياتك"
```

---

## خريطة العلاقات المحدّثة

```
User ─┬─< Category ──< MerchantRule
      ├─< Budget ──< BudgetAllocation
      ├── BankingCustomer
      ├─< BankConnection ──< BankAccount
      ├─< Transaction ──○ BankAccount (بالـ Id، للبنكية فقط)
      │        ├──○ Goal
      │        └──○ Transaction (PossibleDuplicateOf)
      ├─< Goal
      └─< Notification
```

---

## Bounded Contexts

في MVP 0 قلنا إن Open Banking "غالبًا يصير له Context". القرار الآن:

| Context | فيه | يعرف عن Lean؟ |
|---|---|---|
| **الإدارة المالية** (الموجود) | Budget، Transaction، Category، Goal، MerchantRule، Domain Services | ❌ أبدًا |
| **الربط البنكي** (جديد) | BankConnection، BankingCustomer، المزامنة، Inbox/Outbox | من خلال **واجهة** فقط |
| **مزود Lean** (Infrastructure) | تحويل JSON حق Lean لبياناتنا، التوقيع، الـ Tokens | ✅ المكان الوحيد |

**الحد بينهم:** الربط البنكي يستلم من المزود بيانات **بشكلنا** (`BankTransactionData`)، ويحوّلها لـ `Transaction` عبر الـ Domain Services. لو بدّلنا Lean بـ Tarabut، يتغير الصندوق الأخير بس.
