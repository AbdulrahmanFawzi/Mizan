# هللاتي — Domain Discovery (MVP 1: Open Banking)
## الخطوة 3: Entities و Value Objects

نفس تعريفات MVP 0 (الخطوة 3 هناك). هنا **الجديد** و **التعديل على الموجود** فقط.

---

## أهم قرار تصميمي: العملية البنكية **نفس** Entity المصروف اليدوي

| | الخيار أ: `Transaction` واحد بمصدرين ✅ | الخيار ب: `BankTransaction` منفصل |
|---|---|---|
| الفكرة | المصروف اليدوي والبنكي **نفس الـ Entity**، ويختلفون بالمصدر | جدولين، والميزانية تجمع من الاثنين |
| الميزانية، التنبيهات، "تقدر تصرف اليوم" | **ما تتغير**: تجمع العمليات المحسوبة كما هي الآن | كل استعلام لازم يجمع من مصدرين |
| الدمج (OB-1) | عملية تبقى وثانية تنحذف، داخل نفس النوع | نقل بيانات بين نوعين |
| العيب | `Transaction` يكبر بحقول البنك | تكرار منطق كثير |

**اخترت (أ)** لأن كل شيء بنيناه في MVP 0 يحسب من `Transaction`، فنحافظ عليه ونضيف فقط **"هل هذه العملية محسوبة؟"**.
وبيانات البنك الخام (كل حقول Lean) **ما تدخل الـ Domain**: تُحوَّل في طبقة Infrastructure للحقول اللي نحتاجها فقط.

---

## Value Objects الجديدة

| Value Object | المحتوى | القواعد اللي يحميها | سلوك |
|---|---|---|---|
| **Consent** | GrantedAt، ExpiresAt | BR-C4 (12 شهر) | `IsExpired(now)`، `DaysUntilExpiry(today)`، `ShouldRemind(today)` (BR-C5: 14 و 3 أيام) |
| **Iban** | Value | صيغة سعودية: `SA` + 22 رقم، يُخزَّن بدون مسافات وبأحرف كبيرة | `Masked` للعرض: `SA•• •••• 1234` |
| **MerchantKey** | Value | BR-K3: أحرف صغيرة، مسافات مفردة، بدون أرقام الفروع | `From(merchantName)` يرجع null إذا الاسم فاضي |
| **BankReference** | ConnectionId، AccountId، ProviderTransactionId | العملية البنكية تتعرّف بمعرّف المزود (BR-S5) | |
| **ProviderCategory** | Code (مثل `GROCERIES`)، Confidence (0–1) | BR-K4: تحت 0.5 = غير موثوق | `IsReliable` |
| **SyncWindow** | From، To | BR-S6: من آخر مزامنة ناقص 7 أيام. أول ربط: 3 أشهر (BR-H1) | `ForFirstLink(today)`، `After(lastSync)` |

### Enums (قيم ثابتة)

| Enum | القيم | ملاحظة |
|---|---|---|
| **ConnectionStatus** | Pending، Active، Expired، Revoked | BR-C3 |
| **AccountKind** | Current، Savings، CreditCard، ChargeCard، Prepaid، EMoney، Other | من نتيجة V5. `CountsTowardBudgetByDefault`: كلها **ما عدا** Savings و Other (BR-X10) |
| **TransactionSource** | Manual، Bank | |
| **TransactionDirection** | Debit، Credit | BR-X1. اليدوي دائمًا Debit |
| **BankStatus** | Pending، Booked | BR-X6. لليدوي: لا شيء |
| **Treatment** (كيف تأثر على الميزانية) | **Spending** (صرف)، **Refund** (ينقص الصرف، BR-F2)، **OwnTransfer** (ما يُحسب، BR-R)، **Incoming** (داخل وما يأثر، BR-F7)، **Excluded** (استبعده المستخدم، BR-X5) | **هذا قلب MVP 1:** الميزانية تسأل العملية "وش معاملتك؟" بدل ما تجمع كل شيء |
| **CategorySource** | User، MerchantRule، Provider، Fallback | BR-K1 و BR-K6: نعرف هل التصنيف مؤكد أو تلقائي |
| **ReviewReason** | PossibleDuplicate، PossibleTransfer | OB-1 و OB-3 |

---

## Entities الجديدة

### 1. BankConnection (ربط بنك) ← Aggregate Root

| الحقل | النوع | ملاحظة |
|---|---|---|
| Id | معرّف | |
| UserId | معرّف | |
| ProviderEntityId | نص | `entity_id` عند Lean (BR-C11) |
| BankIdentifier، BankName | نص | للعرض واكتشاف التكرار (BR-C2) |
| Status | ConnectionStatus | |
| Consent | Consent | |
| LastSyncedAt | تاريخ ووقت؟ | لـ SyncWindow و "آخر تحديث" (BR-S9) |
| LastRefreshRequestedAt | تاريخ ووقت؟ | BR-S2: 4 ساعات |
| RemindersSent | قائمة أيام (14، 3) | ما يتكرر التذكير |
| ImportedHistory | نعم/لا | أول استيراد خلص؟ (BR-H3) |
| **Accounts** | قائمة BankAccount | ↓ |

**سلوك:** `Activate(entityId, consent)`، `CanRequestRefresh(now)`، `RecordSync(at)`، `Expire()`، `Revoke()`، `Reconnect(consent)`، `IncludeAccount(id, bool)`.

### 2. BankAccount (حساب داخل الربط) ← Entity داخل BankConnection

| الحقل | النوع | ملاحظة |
|---|---|---|
| Id | معرّف | |
| ProviderAccountId | نص | `account_id` عند Lean |
| Kind | AccountKind | |
| Currency | Currency | غير الريال ما يدخل الميزانية (BR-X3) |
| Iban | Iban؟ | لاكتشاف التحويل الداخلي (BR-R1) |
| Nickname | نص | "الحساب الجاري" |
| IncludeInBudget | نعم/لا | افتراضيًا من `Kind`، والمستخدم يغيّره (BR-X10) |
| Balance، BalanceAsOf | Money؟، تاريخ | للعرض فقط (OB-11) |

> **ليش BankAccount داخل BankConnection ومو Aggregate لحاله؟** الحساب ما له حياة بدون الربط: إلغاء الربط يلغي حساباته، وانتهاء الموافقة يوقفها كلها. هذا بالضبط تعريف "جزء من Aggregate".

### 3. BankingCustomer (العميل عند المزود)

| الحقل | النوع | ملاحظة |
|---|---|---|
| UserId | معرّف | واحد لكل مستخدم |
| Provider | نص | "Lean" |
| ProviderCustomerId | نص | `customer_id` عند Lean |

صغير ومنفصل عن `User` عشان `User` ما يعرف شيء عن Lean. ينحذف مع الحساب (BR-C10).

### 4. MerchantRule (قاعدة تصنيف تاجر) ← Aggregate Root

| الحقل | النوع | ملاحظة |
|---|---|---|
| Id | معرّف | |
| UserId | معرّف | |
| Merchant | MerchantKey | **فريد لكل مستخدم** |
| CategoryId | معرّف | لو انحذفت الفئة، تنحذف القاعدة |
| CreatedAt | تاريخ ووقت | |

### 5. RejectedMatch (اقتراح رفضه المستخدم)

| الحقل | النوع | ملاحظة |
|---|---|---|
| UserId | معرّف | |
| BankTransactionId | معرّف | |
| ManualTransactionId | معرّف | |

BR-D6: ما نقترح نفس الزوج مرة ثانية. صغير ويُمسح لما تنحذف أي من العمليتين.

---

## تعديلات على Entities موجودة

### Transaction (المصروف) — يتوسّع

| الحقل الجديد | النوع | ملاحظة |
|---|---|---|
| Source | TransactionSource | Manual أو Bank |
| Direction | TransactionDirection | |
| Treatment | Treatment | يحدد تأثيره على الميزانية |
| CategorySource | CategorySource | |
| Bank | BankReference؟ | فاضي لليدوي |
| BankStatus | BankStatus؟ | فاضي لليدوي |
| Fee | Money؟ | BR-X11 |
| Note | نص؟ | الشيء الوحيد مع الفئة اللي يكتبه المستخدم على البنكية |
| Review | ReviewReason + معرّف العملية المرشحة؟ | فاضي إذا ما تحتاج مراجعة |
| PossibleDuplicateOf | معرّف؟ | **على اليدوي:** "معلّق لأنه يشبه عملية بنكية" (OB-10) |

**المبلغ اللي يحسبه الـ Budget** (خاصية محسوبة `BudgetEffect`):

| الحالة | الأثر |
|---|---|
| Spending | + (المبلغ + الرسوم) |
| Refund | − المبلغ (والفئة ما تنزل تحت صفر، BR-F4، يطبقها الـ Budget) |
| OwnTransfer، Incoming، Excluded | 0 |
| Review = PossibleTransfer | 0 (OB-10) |
| يدوي و PossibleDuplicateOf موجود | 0 (OB-10) |

**قواعد جديدة يحميها:**
- العملية البنكية **ما تنحذف** ولا يتغير مبلغها أو تاريخها (BR-X4، BR-X5): `Update` و `Delete` الموجودة **ترفض** إذا Source = Bank.
- دوال جديدة: `ImportFromBank(...)`، `ApplyBankUpdate(...)` (يحافظ على اختيارات المستخدم، BR-X7)، `Recategorize(category, source)`، `Exclude()` / `Include()`، `MarkForReview(reason, candidateId)`، `ResolveReview(...)`، `MergeManual(manual)`.
- `BR-T3` (ما يكون بالمستقبل) لليدوي فقط.

### Category

- ما يتغير شكله. لكن حذف فئة الآن **أيضًا** يحذف قواعد التجار المرتبطة فيها.

### Notification — أنواع جديدة

| النوع | متى | البيانات |
|---|---|---|
| ConsentExpiring | قبل 14 و 3 أيام (BR-C5) | اسم البنك، تاريخ الانتهاء |
| ConsentExpired | مرة وحدة (BR-C6) | اسم البنك |
| ReviewNeeded | لما تتجمع عمليات تحتاج مراجعة (تكرار أو تحويل) | العدد |
| SalaryDetected | BR-F6 | المبلغ، اليوم المقترح |

---

## خريطة سريعة

```
User ──1:1── BankingCustomer (Lean customer_id)
  │
  ├──1:N── BankConnection ──1:N── BankAccount
  │              (consent, status)      (kind, IBAN, include?)
  │
  ├──1:N── Transaction ──0:1── BankReference ──► BankAccount
  │          (source, treatment, review)
  │
  ├──1:N── MerchantRule ──► Category
  └──1:N── RejectedMatch
```

---

## قرار مفتوح من الـ Spike

| # | السؤال | المقترح |
|---|---|---|
| BR-X12 | **سحب الصراف (ATM):** صرف أو نقل للجيب؟ | يُحسب **صرف** ويُصنّف "أخرى" باسم "سحب نقدي". لو اخترت "نقل"، يصير Treatment = OwnTransfer والمستخدم يسجل مصروفات الكاش يدويًا |
