# ميزان — Domain Discovery
## الخطوة 5: ERD و Architecture وخطة الكود

---

## القرارات التقنية

| القرار | الاختيار | السبب |
|---|---|---|
| Backend | **ASP.NET Core على .NET 10** (LTS) | مثبت عندك، وتبني على خبرة Order Management |
| قاعدة البيانات | **PostgreSQL** عبر Docker | مجانية، خفيفة، ممتازة مع EF Core |
| ORM | **EF Core** + Npgsql | المعيار في .NET |
| الواجهة | **Flutter** (جوال)، **بعد** الـ API | قرارك. نبني API أولًا ونجربه من Scalar |
| المصادقة | **JWT** (Access Token قصير + Refresh Token) | تطبيقات الجوال ما تعتمد على الكوكيز. هنا يرجع UC "Refresh Session" لكن كتفصيل تقني |
| كلمات المرور | `PasswordHasher<T>` من ASP.NET | بدون ASP.NET Identity الكامل، لأنه يربط الـ Domain بالإطار، وتتعلم الآلية بنفسك |
| المعرّفات | `Guid.CreateVersion7()` | فريد عالميًا ومرتّب زمنيًا، فأداء الفهارس أفضل من Guid العادي |
| الوقت | `TimeProvider` (مدمج في .NET) | عشان نختبر "اليوم" و"نهاية الفترة" بدون ما ننتظر الوقت الحقيقي |
| Domain Events | Dispatcher بسيط نكتبه بأيدينا | تتعلم الآلية بدل ما تخفيها مكتبة. MediatR صار برخصة تجارية |

---

## ERD

```
users ─────────┬──────< categories
               ├──────< budgets ──────< budget_allocations >────── categories
               ├──────< transactions >────── categories
               │              └──────○ goals   (اختياري)
               ├──────< goals
               ├──────< notifications
               └──────< refresh_tokens
```

### users
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| name | varchar(100) | NOT NULL |
| email | varchar(256) | NOT NULL، **UNIQUE** (BR-U1) |
| password_hash | text | NOT NULL |
| currency | char(3) | NOT NULL |
| monthly_income | numeric(18,2) | NOT NULL، CHECK > 0 |
| period_start_day | smallint | NOT NULL، CHECK بين 1 و 31 |
| created_at | timestamptz | NOT NULL |

### categories
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users (حذف متتالي) |
| name | varchar(50) | NOT NULL |
| kind | varchar(20) | Regular / Other / Savings |
| | | **UNIQUE (user_id, lower(name))** (BR-B1) |

### budgets
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users |
| period_start | date | NOT NULL |
| period_end | date | NOT NULL، CHECK ≥ period_start |
| planned_income | numeric(18,2) | NOT NULL |
| currency | char(3) | NOT NULL |
| | | **UNIQUE (user_id, period_start)** (BR-P5) |

### budget_allocations
| العمود | النوع | قيود |
|---|---|---|
| budget_id | uuid | PK مركّب، FK → budgets |
| category_id | uuid | PK مركّب، FK → categories |
| limit_amount | numeric(18,2) | NOT NULL، CHECK ≥ 0 |
| highest_threshold_sent | smallint | NULL أو 80 / 90 / 100 |

> **ليش `highest_threshold_sent` بدل قائمة العتبات؟** العتبات تصاعدية. إذا انرسلت 90، فـ 80 أكيد ما تنرسل مرة ثانية (BR-A2)، والـ 100 تنرسل فقط إذا انقطعت. رقم واحد يكفي بدل قائمة. **دائمًا دوّر على أبسط تمثيل يحقق القاعدة.**

### transactions
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users |
| category_id | uuid | FK → categories |
| goal_id | uuid | NULL، FK → goals |
| amount | numeric(18,2) | NOT NULL، CHECK > 0 |
| currency | char(3) | NOT NULL |
| date | date | NOT NULL |
| merchant | varchar(100) | NULL |
| created_at / updated_at | timestamptz | |
| | | **INDEX (user_id, date)**، **INDEX (category_id, date)** |

> الفهارس مبنية على أكثر سؤال راح نسأله: "مجموع مصروفات فئة X بين تاريخين".

### goals
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users |
| name | varchar(100) | NOT NULL |
| target_amount | numeric(18,2) | CHECK > 0 |
| initial_amount | numeric(18,2) | CHECK ≥ 0 |
| currency | char(3) | |
| target_date | date | NOT NULL |
| is_archived | boolean | |

### notifications
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users |
| type | varchar(50) | |
| payload | jsonb | بيانات الإشعار |
| created_at | timestamptz | |
| read_at | timestamptz | NULL = غير مقروء |

### refresh_tokens (تقني، مو Domain)
| العمود | النوع | قيود |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users |
| token_hash | text | **نخزن الـ hash فقط**، مو التوكن نفسه |
| expires_at / revoked_at / created_at | timestamptz | |

---

## Architecture: Clean Architecture

```
┌───────────────────────────────────────────┐
│  Mizan.Api            (HTTP، JWT، Scalar)  │
├───────────────────────────────────────────┤
│  Mizan.Infrastructure (EF Core، Postgres)  │
├───────────────────────────────────────────┤
│  Mizan.Application    (Use Cases)          │
├───────────────────────────────────────────┤
│  Mizan.Domain         (القواعد — لا شيء)   │
└───────────────────────────────────────────┘
```

**قاعدة الاعتماد (Dependency Rule):** الأسهم تتجه للداخل فقط.

| المشروع | يعتمد على | ممنوع يعرف عن |
|---|---|---|
| **Domain** | لا شيء | EF Core، ASP.NET، Postgres، HTTP |
| **Application** | Domain | EF Core، Postgres، HTTP |
| **Infrastructure** | Application، Domain | ASP.NET |
| **Api** | الكل (للربط فقط) | — |

**كيف يحفظ الـ Application بدون ما يعرف Postgres؟** يعرّف **Interface** مثل `ITransactionRepository`، والـ Infrastructure يكتب التطبيق الفعلي. هذا اسمه **Dependency Inversion**، وهو اللي يخلي الـ Domain يبقى مستقل (قسم 44 في الـ PRD).

---

## هيكل المشروع

```
Mizan/
├── docs/                        ← هذي الملفات
├── src/
│   ├── Mizan.Domain/
│   ├── Mizan.Application/
│   ├── Mizan.Infrastructure/
│   └── Mizan.Api/
├── tests/
│   ├── Mizan.Domain.Tests/      ← Unit Tests للقواعد (سريعة، بدون DB)
│   ├── Mizan.Application.Tests/
│   └── Mizan.Api.Tests/         ← Integration Tests مع Postgres حقيقي
├── mobile/                      ← Flutter (لاحقًا)
├── docker-compose.yml           ← Postgres
└── Mizan.sln
```

---

## خطة الكود

| المرحلة | وش نبني | ليش بهذا الترتيب | يقابل في الـ Roadmap |
|---|---|---|---|
| **0. التجهيز** | git، الـ solution، المشاريع، المراجع، Postgres في Docker، أول test يشتغل | أساس نظيف قبل أي ميزة | M0 |
| **1. Domain** | `Money` ← `BudgetPeriod` ← باقي Value Objects ← الـ Aggregates ← حسابات الـ Insights. **كلها بـ TDD** | قلب المشروع، يُختبر بدون قاعدة بيانات، وهو أكثر شيء تتعلم منه | M1–M4 (القواعد) |
| **2. Application** | Use Cases، Repository Interfaces، Domain Event Dispatcher | ينسّق الـ Domain بدون تفاصيل تقنية | M1–M4 |
| **3. Infrastructure** | EF Core، Mappings، Migrations، Repositories | الآن نعرف بالضبط وش نحفظ | M0–M4 |
| **4. Api** | Endpoints، JWT، Validation، معالجة الأخطاء (ProblemDetails) | الطبقة الخارجية آخر شيء | M1–M5 |
| **5. Flutter** | التطبيق | الـ API جاهز ومختبر | M5 |

**ملاحظة:** الـ Roadmap الأصلي يبدأ بـ "Vertical Slice" (شريحة رفيعة من كل الطبقات). اخترت أبدأ بالـ Domain لأن هدفك تعلّم DDD، والـ Domain هو المكان اللي يصير فيه التعلم الحقيقي. المرحلة 0 تضمن إن الهيكل كله موجود من البداية.
