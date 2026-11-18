# ميزان (Mizan)

تطبيق ميزانية شخصية للسوق السعودي، بالعربي من أول يوم. يربط حسابك البنكي عبر المصرفية المفتوحة، فكل عملية تنخصم من ميزانيتك وتتصنّف تلقائيًا، وتعرف كم تقدر تصرف اليوم.

> **الحالة:** MVP 1 (المصرفية المفتوحة) والمرحلة 2 (ميزات الإنتاج) مكتملة على بيئة Lean التجريبية. النشر ينتظر السجل التجاري وعقد Lean. التفاصيل في [خطة الإنتاج](docs/production/00-roadmap.md).

## الميزات

- **ربط البنك عبر Lean** (مرخّصة من ساما): استيراد الحسابات والأرصدة والعمليات، وتحديث يومي وعند فتح التطبيق.
- **تصنيف تلقائي:** كل عملية تتصنّف تحت فئتها، ومع «تذكّر هذا التاجر» ينطبق التصنيف على العمليات الجاية.
- **مراجعة ذكية:** يكتشف العمليات المكررة (يدوي وبنكي)، والتحويلات بين حساباتك فما تنحسب صرف. وجواب واحد يكفي لكل التحويلات لنفس الحساب.
- **الميزانية:** حد لكل فئة، و«كم تقدر تصرف اليوم»، وملاحظات ذكية وتنبيهات عند 80% و90% و100%.
- **أهداف ادخار** بخطة شهرية، والتحويل لحساب الادخار يتحسب ادخار تلقائيًا.
- **إدخال يدوي** للصرف النقدي.
- **الحساب:** تأكيد الإيميل واسترجاع كلمة المرور برمز من 6 أرقام، وإعداد موجّه بعد التسجيل، وتصدير كل البيانات (نظام حماية البيانات الشخصية)، وحذف الحساب.
- **عربي وإنجليزي، فاتح وداكن.** نصوص الخادم (الملاحظات والإشعارات والأخطاء) تتبع لغة التطبيق.

## التقنيات

| الجزء | التقنية |
|---|---|
| الخادم | ASP.NET Core على .NET 10 · EF Core 10 · PostgreSQL 17 · JWT |
| التطبيق | Flutter · Riverpod · go_router |
| البنوك | Lean (SAMA Open Banking) |
| الاختبارات | xUnit · Testcontainers · flutter_test · integration_test |
| التشغيل الآلي | GitHub Actions (`.github/workflows/ci.yml`) |

البنية: Clean Architecture مع DDD (Domain / Application / Infrastructure / Api)، وأحداث نطاق، وInbox/Outbox للـ Webhooks والأعمال الخارجية.

## التشغيل

المتطلبات: .NET 10 SDK، وDocker Desktop، وFlutter (ومعه Xcode لمحاكي iPhone).

**1. قاعدة البيانات والخادم:**

```bash
docker compose up -d
```

```bash
dotnet tool restore
```

```bash
dotnet run --project src/Mizan.Api --launch-profile http
```

- الخادم على `http://localhost:5009`. في التطوير، الـ Migrations تنطبق تلقائيًا.
- التوثيق التفاعلي (Scalar): `http://localhost:5009/scalar/v1`. أمثلة جاهزة في `src/Mizan.Api/Mizan.Api.http`.
- **الإيميلات** (رموز التأكيد واسترجاع كلمة المرور) تنكتب في سجل الخادم للحين، لين نختار مزوّد إيميل في المرحلة 3.

**2. التطبيق:**

```bash
cd mobile && flutter run
```

التطبيق يتصل بـ `http://localhost:5009` افتراضيًا. لخادم ثاني:

```bash
flutter run --dart-define=API_BASE_URL=https://api.example.com
```

## ربط البنوك (Lean)

بدون مفاتيح Lean، التطبيق يشتغل طبيعي، وربط البنك بس يرجع رسالة «غير مُعد».
لتفعيله في التطوير، حط مفاتيح الـ Sandbox (من Lean Dashboard ← Development ← Integration settings) في **User Secrets**، مو في الملفات:

```bash
dotnet user-secrets set "Lean:AppToken" "<app token>" --project src/Mizan.Api
```

```bash
dotnet user-secrets set "Lean:ClientSecret" "<client secret>" --project src/Mizan.Api
```

- ربط البنك يحتاج **إيميل مؤكد**.
- عناوين الرجوع (`Lean:SuccessRedirectUrl` و`Lean:FailRedirectUrl`) لازم تكون مسجلة في Lean Dashboard ← Redirection URLs، وإلا الربط يفشل.
- الـ Webhooks (`POST /api/webhooks/lean`) تحتاج `Lean:WebhookSecret` وعنوان يوصل له الإنترنت. محليًا ما يلزم: التطبيق يكتشف الربط الجديد بنفسه ويحدّث عند الفتح.
- الأعمال الخلفية (التحديث اليومي 6 صباحًا، ومعالجة الـ Webhooks، والـ Outbox ومنها الإيميلات) تشتغل داخل الخادم. `BackgroundJobs:Enabled=false` يوقفها.
- **موافقة الربط في بيئة Lean التجريبية تنتهي بعد 24 ساعة**، فبعدها يطلب التطبيق إعادة الربط.
- تجربة الـ Sandbox وما تعلمناه منها: `docs/domain-discovery/mvp1/00-sandbox-findings.md` و`spikes/lean/`.

## الاختبارات

```bash
dotnet test
```

```bash
cd mobile && flutter analyze && flutter test
```

- `Mizan.Domain.Tests`: قواعد العمل، سريعة وبدون قاعدة بيانات.
- `Mizan.Api.Tests`: الرحلة كاملة عبر HTTP على PostgreSQL حقيقي في Docker (يحتاج Docker شغّال). البنك يُختبر ببنك وهمي (`FakeBankingProvider`)، والإيميل بمرسل وهمي.
- `mobile/test`: اختبارات الشاشات. و`mobile/integration_test`: رحلة كاملة على المحاكي ضد خادم محلي شغّال:

```bash
cd mobile && flutter test integration_test
```

GitHub Actions يشغّل اختبارات الخادم والتطبيق مع كل رفع.

## الهيكل

```
src/
  Mizan.Domain/          القواعد (ما يعتمد على أي شيء)
  Mizan.Application/     الـ Use Cases وواجهات المستودعات ومعالجات الأحداث
  Mizan.Infrastructure/  EF Core وPostgreSQL والتشفير وJWT وLean والأعمال الخلفية
  Mizan.Api/             الـ Endpoints والمصادقة ومعالجة الأخطاء والترجمة
tests/
  Mizan.Domain.Tests/
  Mizan.Api.Tests/
mobile/                  تطبيق Flutter (iOS وAndroid)
docs/
  domain-discovery/      التحليل: Use Cases وBusiness Rules وEntities وAggregates وERD (MVP 0، و mvp1/ للمصرفية المفتوحة)
  production/            خطة الإنتاج وقرارات المرحلة 2
spikes/lean/             تجربة Lean الأولى (المفاتيح في .env غير مرفوع)
```

## إضافة Migration

```bash
dotnet ef migrations add <Name> --project src/Mizan.Infrastructure --startup-project src/Mizan.Api --output-dir Persistence/Migrations
```

## قبل أي نشر حقيقي

- `Jwt:SigningKey` و`Banking:EncryptionKey` (32 بايت عشوائية بصيغة base64، لتشفير الـ IBAN) ومفاتيح `Lean:*` تجي من متغيرات بيئة أو Secret Manager، ولا تنحط في الملفات. المفاتيح الموجودة في `appsettings.Development.json` للتطوير فقط.
- مزوّد إيميل حقيقي بدل `LogEmailSender`.
- الـ Migrations تنطبق كخطوة نشر منفصلة، مو عند تشغيل الخادم.
- مراجعة متطلبات نظام حماية البيانات الشخصية (PDPL) وأنظمة ساما قبل أول مستخدم حقيقي.
