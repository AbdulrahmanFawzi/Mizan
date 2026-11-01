# هللاتي — Domain Discovery (MVP 1: Open Banking)
## الخطوة 5: قاعدة البيانات والمعمارية وخطة الكود

---

## القرارات التقنية الجديدة

| القرار | الاختيار | السبب |
|---|---|---|
| مزود Open Banking | **Lean** خلف واجهة `IBankingProvider` | مرخّص من SAMA، والـ Sandbox شغال. الواجهة تخلينا نبدّل لـ Tarabut بدون لمس الـ Domain |
| الاتصال بـ Lean | `HttpClient` مكتوب بأيدينا + `Microsoft.Extensions.Http.Resilience` (إعادة المحاولة) | Lean ما عنده SDK رسمي لـ .NET، و API بسيط |
| الأعمال الخلفية | `BackgroundService` مدمج في ASP.NET (بدون Hangfire) | نفس فلسفة MVP 0: تتعلم الآلية بدل ما تخفيها مكتبة. سيرفر واحد بالبداية يكفي |
| الاعتمادية مع الشبكة | **Inbox** للـ Webhooks و **Outbox** للطلبات الخارجية | الخطوة 4 |
| قفل المزامنة | Postgres **Advisory Lock** لكل ربط | بدون جدول إضافي، ويتحرر تلقائيًا لو طاح الاتصال |
| أسرار Lean | **User Secrets** في التطوير، ومتغيرات بيئة في الإنتاج | ما تدخل git أبدًا (BR-C12) |
| الـ IBAN | يُخزن **مشفّر** وأيضًا **بصمة** (Hash) للبحث | بيانات مالية حساسة (PDPL). البصمة تكفي لمطابقة التحويل الداخلي (BR-R1) بدون فك التشفير |
| Webhooks أثناء التطوير | **Polling** احتياطي + نفق (Cloudflare Tunnel) اختياري | جهازك ما يستقبل طلبات من الإنترنت. نخلي المزامنة تشتغل حتى بدون Webhook |
| Flutter | الحزمة الرسمية `lean_sdk_flutter` + App Scheme `halalati://` | الخطوة 0 (Spike): الربط يحتاج عنوان رجوع مسجّل |

---

## تغييرات قاعدة البيانات

### جداول جديدة

**bank_customers**
| العمود | النوع | قيود |
|---|---|---|
| user_id | uuid | PK، FK → users (حذف متتالي) |
| provider | varchar(20) | "lean" |
| provider_customer_id | varchar(100) | UNIQUE |
| created_at | timestamptz | |

**bank_connections**
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users |
| provider_entity_id | varchar(100) | NULL حتى التفعيل، UNIQUE |
| bank_identifier / bank_name | varchar | |
| status | varchar(20) | Pending / Active / Expired / Revoked |
| consent_granted_at / consent_expires_at | timestamptz | |
| last_synced_at / last_refresh_requested_at | timestamptz | NULL |
| reminders_sent | smallint[] | {14, 3} |
| history_imported | boolean | |
| created_at | timestamptz | |
| | | **UNIQUE (user_id, bank_identifier) WHERE status = 'Active'** (BR-C2) |

**bank_accounts**
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| connection_id | uuid | FK → bank_connections (حذف متتالي) |
| provider_account_id | varchar(100) | UNIQUE (connection_id, provider_account_id) |
| kind | varchar(20) | |
| currency | char(3) | |
| iban_encrypted | text | NULL |
| iban_hash | char(64) | NULL، INDEX (BR-R1) |
| nickname | varchar(100) | |
| include_in_budget | boolean | |
| balance / balance_currency / balance_as_of | numeric / char(3) / timestamptz | NULL |

**merchant_rules**
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users |
| merchant_key | varchar(100) | **UNIQUE (user_id, merchant_key)** |
| category_id | uuid | FK → categories (حذف متتالي) |
| created_at | timestamptz | |

**rejected_matches**
| العمود | النوع | قيود |
|---|---|---|
| bank_transaction_id | uuid | PK مركّب، FK → transactions (حذف متتالي) |
| manual_transaction_id | uuid | PK مركّب، FK → transactions (حذف متتالي) |

**webhook_inbox** (تقني)
| العمود | النوع | قيود |
|---|---|---|
| event_id | varchar(100) | PK (BR-S8: نفس الحدث مرتين = مرة) |
| type | varchar(100) | |
| payload | jsonb | |
| received_at / processed_at | timestamptz | processed_at NULL = لسه |
| attempts / last_error | int / text | |

**outbox_messages** (تقني)
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| type | varchar(100) | مثل `RevokeConsent`، `DeleteBankingCustomer`، `RequestRefresh` |
| payload | jsonb | |
| created_at / next_attempt_at / processed_at | timestamptz | |
| attempts / last_error | int / text | |

### تعديل جدول transactions

| العمود الجديد | النوع | ملاحظة |
|---|---|---|
| source | varchar(10) | Manual / Bank. الموجود كله Manual |
| direction | varchar(10) | Debit / Credit |
| treatment | varchar(20) | Spending / Refund / OwnTransfer / Incoming / Excluded |
| category_source | varchar(20) | User / MerchantRule / Provider / Fallback |
| bank_account_id | uuid | NULL، FK → bank_accounts (**SET NULL**: لما يحتفظ المستخدم بالعمليات بعد إلغاء الربط، BR-C9) |
| bank_connection_id | uuid | NULL |
| provider_transaction_id | varchar(100) | NULL، **UNIQUE (bank_connection_id, provider_transaction_id)** (BR-S5) |
| bank_status | varchar(10) | NULL / Pending / Booked |
| merchant_key | varchar(100) | NULL، INDEX (user_id, merchant_key) لـ BR-K2 و BR-F3 |
| counterparty_iban_hash | char(64) | NULL (BR-R1) |
| fee / fee_currency | numeric / char(3) | NULL (BR-X11) |
| note | varchar(200) | NULL |
| review_reason | varchar(20) | NULL |
| review_candidate_id | uuid | NULL |
| possible_duplicate_of | uuid | NULL (على اليدوي، OB-10) |

**ملاحظة على الأداء:** الميزانية تجمع `BudgetEffect`. نحسبه في SQL بنفس منطق الـ Domain (CASE على treatment و review_reason) عبر **عمود محسوب** `budget_amount`، عشان استعلام "مجموع الفئة في الفترة" يبقى سريع ويستخدم نفس الفهرس الموجود `(category_id, date)`. اختبار Integration يتأكد إن العمود والـ Domain يعطون نفس الرقم.

---

## المعمارية: وين يروح كل شيء

```
Mizan.Domain
├── Transactions/      Transaction (موسّع)، Treatment، CategorySource، BankReference، ReviewReason
├── Banking/           BankConnection، BankAccount، Consent، Iban، AccountKind، SyncWindow، أحداثها
├── Categorization/    MerchantRule، MerchantKey، ProviderCategory
└── Importing/         TransactionClassifier، CategoryResolver، DuplicateMatcher، TransferPairMatcher

Mizan.Application
├── Abstractions/      IBankingProvider ✨، IBankConnectionRepository، IMerchantRuleRepository،
│                      IOutbox، ISyncLock
├── Banking/           BankLinkService (Token الربط، الإلغاء)، BankImportService (الاستيراد)،
│                      WebhookProcessor، BankingModels (BankTransactionData...)
├── Transactions/      ReviewService (حل المراجعة، الدمج، الاستبعاد، "دائمًا هذا التاجر")
└── EventHandlers/     BankTransactionsImported، Consent*، AccountDeleted

Mizan.Infrastructure
├── Banking/Lean/      LeanBankingProvider (يطبّق IBankingProvider)، LeanTokenCache،
│                      LeanSignatureVerifier (HMAC-SHA512)، LeanOptions، تحويل JSON
├── Background/        SyncScheduler (يومي 6 صباحًا الرياض + فحص الموافقات)،
│                      InboxProcessor، OutboxProcessor
├── Security/          IbanProtector (تشفير + بصمة)
└── Persistence/       الجداول الجديدة، Migration، PostgresSyncLock

Mizan.Api
└── Endpoints/         BankConnectionEndpoints، WebhookEndpoints (بدون JWT، بالتوقيع)،
                       ReviewEndpoints
```

### الواجهة اللي تفصلنا عن Lean

```csharp
public interface IBankingProvider
{
    Task<string> CreateCustomerAsync(Guid userId, CancellationToken ct);
    Task<LinkToken> CreateLinkTokenAsync(string customerId, CancellationToken ct);
    Task<IReadOnlyList<BankAccountData>> GetAccountsAsync(string entityId, CancellationToken ct);
    IAsyncEnumerable<BankTransactionData> GetTransactionsAsync(string entityId, string accountId, SyncWindow window, CancellationToken ct);
    Task<RefreshResult> RequestRefreshAsync(string entityId, CancellationToken ct);   // CONSENT_EXPIRED → Expired
    Task RevokeConsentAsync(string entityId, CancellationToken ct);
    Task DeleteCustomerAsync(string customerId, CancellationToken ct);
}
```

`BankTransactionData` **بشكلنا**، مو شكل Lean: المعرّف، المبلغ، الاتجاه، الحالة، التاريخ، التاجر، رمز النشاط، تصنيف المزود ودرجة ثقته، النوع الفرعي (استرجاع؟)، IBAN الطرف الآخر، الرسوم.

### الـ API الجديد

| Method | Path | الوظيفة | UC |
|---|---|---|---|
| POST | `/api/bank-connections/link` | ينشئ Customer لو ما فيه + Token مؤقت للـ SDK | UC-70 |
| GET | `/api/bank-connections` | الروابط والحسابات وحالتها | UC-72 |
| POST | `/api/bank-connections/{id}/sync` | تحديث الآن (يحترم 4 ساعات) | UC-82 |
| POST | `/api/bank-connections/{id}/reconnect` | Token لإعادة الربط | UC-73 |
| DELETE | `/api/bank-connections/{id}?keepTransactions=true` | إلغاء الربط | UC-74 |
| PATCH | `/api/bank-accounts/{id}` | يدخل الميزانية أو لا | BR-X10 |
| POST | `/api/webhooks/lean` | **بدون تسجيل دخول**، يتحقق من `lean-signature` | UC-71، 75، 80 |
| GET | `/api/transactions/review` | العمليات اللي تحتاج قرار | UC-92، 93 |
| POST | `/api/transactions/{id}/review` | دمج / مو مكرر / تحويل / صرف | UC-92، 93 |
| PUT | `/api/transactions/{id}/category` | تغيير التصنيف + `rememberMerchant` | UC-91 |
| POST | `/api/transactions/{id}/exclude` و `/include` | الاستبعاد | UC-92 |

**ملاحظة:** `PUT` و `DELETE` الموجودة على `/api/transactions/{id}` ترفض العملية البنكية بخطأ واضح (BR-X4، BR-X5).

---

## خطة الكود

| المرحلة | وش نبني | ليش بهذا الترتيب |
|---|---|---|
| **1. Domain** | Value Objects (Consent، Iban، MerchantKey، SyncWindow) ← توسيع Transaction و `BudgetEffect` ← BankConnection ← الـ Domain Services الأربعة. **كلها TDD** | أغلب قواعد MVP 1 هنا، وتنختبر بدون قاعدة بيانات ولا Lean |
| **2. Application** | `IBankingProvider` + **مزود وهمي للاختبارات** ← BankImportService ← ReviewService ← Handlers | نختبر الاستيراد كامل بمزود وهمي نتحكم فيه (تكرار، تحويل، استرجاع، معلّقة تختفي) |
| **3. Infrastructure** | Migration ← LeanBankingProvider ← Inbox/Outbox ← المجدول | الآن نعرف بالضبط وش نحفظ ووش نطلب |
| **4. Api** | Endpoints + Webhook | |
| **5. اختبار مع Sandbox** | ربط حقيقي بالـ Mockbank من الـ API واستيراد | يتأكد إن التحويل من JSON Lean صحيح |
| **6. Flutter** | شاشة "اربط بنكك" (SDK)، الحسابات، شاشة المراجعة، شارة "من البنك"، تغيير التصنيف مع "دائمًا" | |

**قرار مفتوح قبل المرحلة 1:** BR-X12 (سحب الصراف). المقترح: صرف تحت "أخرى".

---

## فروقات التنفيذ عن هذا التصميم (2026-10-25)

المرجع النهائي هو الكود والـ Migration `20261025175314_OpenBanking`.

| الشيء | في التصميم | في التنفيذ | السبب |
|---|---|---|---|
| حالة الربط `Pending` | موجودة | **غير موجودة** | الربط ينشأ لما يأكده Lean (`entity.created` أو الاكتشاف)، فما فيه حالة "قيد الإنشاء" نحتاج نخزنها |
| `Treatment.Excluded` | قيمة من Treatment | **علم منفصل `IsExcluded`** + قيمة جديدة `OutsideBudget` | الاستبعاد اختيار المستخدم ويرجع منه، فما يصير يمسح التصنيف الأصلي. و `OutsideBudget` للحسابات اللي ما تدخل الميزانية والعملات الأخرى |
| `counterparty_iban_hash` و `iban_hash` | أعمدة | **بدلها `counterparty_account_id` و `bank_sub_type`** (Migration `SavingsTransfers`) | IBAN الطرف الآخر ما يُحفظ أبدًا (خصوصية). نحفظ فقط **أي حساب من حسابات المستخدم** كان الطرف الثاني، عشان نقدر نعيد التصنيف (BR-X13) |
| حدث `AccountDeleted` | Event | **مباشر في `ProfileService`** | حذف الحساب يضيف رسالة Outbox في نفس الـ Transaction، وهذا يكفي بدون Event |
| إلغاء الربط عند Lean | `RevokeConsent(entityId)` | `RevokeConsent(customerId, entityId)` | API حق Lean يحتاج رقم الموافقة، ونجيبه من الـ Entity |
| حذف العميل عند Lean | `DeleteCustomer` | **حذف كل Entity** للعميل | Lean ما عنده endpoint لحذف العميل |
| `POST /{id}/reconnect` | Endpoint | **نفس `/link`** | إعادة الربط بنفس البنك = ربط جديد، والنظام يكمل على نفس الربط القديم (BR-C7) |
| ترتيب المزامنة | تحديث ثم استيراد | **استيراد ثم طلب تحديث** + حالة "يتحدّث" 15 دقيقة | في الـ Sandbox: بعد طلب التحديث، Lean يرجع 504 لدقائق. الآن "تحديث الآن" يرد خلال 0.1 ثانية بدل 5 دقائق |
| مزامنة أولى ناقصة | — | `RecordSync(complete: false)` | لو فشل حساب أو رجع الاستيراد الأول فاضي، ما نتقدم بالنافذة عشان ما يضيع سجل 3 أشهر |
| مدة الموافقة | 12 شهر | 12 شهر من `transaction_to_date_time` | ⚠️ **سؤال مفتوح:** Lean رجّع `expiration_date_time` بعد **24 ساعة** فقط، بينما شاشة الربط قالت للمستخدم سنة. نتحقق بعد 2026-10-26 17:14 UTC: إذا التحديث رجع `CONSENT_EXPIRED`، فالموافقة فعلًا يوم واحد في الـ Sandbox |
