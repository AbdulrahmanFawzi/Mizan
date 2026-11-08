# ميزان (Mizan) — MVP 0

منصة إدارة مالية شخصية للسوق السعودي. هذا الـ Backend (API) لنسخة MVP 0: إدخال يدوي، بدون Open Banking وبدون AI.

## التقنيات

ASP.NET Core على .NET 10 · PostgreSQL 17 · EF Core 10 · JWT · xUnit · Testcontainers

## التشغيل

المتطلبات: .NET 10 SDK و Docker Desktop.

```bash
docker compose up -d
```

```bash
dotnet tool restore
```

```bash
dotnet run --project src/Mizan.Api --launch-profile http
```

- الـ API على `http://localhost:5009`. في بيئة التطوير، الـ Migrations تُطبَّق تلقائيًا عند التشغيل.
- التوثيق التفاعلي (Scalar): `http://localhost:5009/scalar/v1`. سجّل من `/api/auth/register`، انسخ `accessToken`، واستخدمه كـ Bearer.
- أمثلة جاهزة في `src/Mizan.Api/Mizan.Api.http`.

## ربط البنوك (MVP 1، عبر Lean)

بدون مفاتيح Lean، التطبيق يشتغل طبيعي وربط البنك فقط يرجع رسالة "غير مُعد".
لتفعيله في التطوير، حط مفاتيح الـ Sandbox (من Lean Dashboard → Development → Integration settings) في **User Secrets**، مو في الملفات:

```bash
dotnet user-secrets set "Lean:AppToken" "<app token>" --project src/Mizan.Api
```

```bash
dotnet user-secrets set "Lean:ClientSecret" "<client secret>" --project src/Mizan.Api
```

- عناوين الرجوع (`Lean:SuccessRedirectUrl` و `Lean:FailRedirectUrl`) لازم تكون مسجلة في Lean Dashboard → Redirection URLs، وإلا الربط يفشل.
- الـ Webhooks (`POST /api/webhooks/lean`) تحتاج `Lean:WebhookSecret` وعنوان يوصل له الإنترنت. محليًا ما يلزم: التطبيق يكتشف الربط الجديد بنفسه (`/api/bank-connections/discover`) ويحدّث عند الفتح.
- الأعمال الخلفية (التحديث اليومي 6 صباحًا، معالجة الـ Webhooks، الـ Outbox) تشتغل داخل الـ API. `BackgroundJobs:Enabled=false` يوقفها.
- تجربة الـ Sandbox الأولى وما تعلمناه منها: `docs/domain-discovery/mvp1/00-sandbox-findings.md` و `spikes/lean/`.

## الاختبارات

```bash
dotnet test
```

- `Mizan.Domain.Tests`: قواعد العمل، سريعة وبدون قاعدة بيانات.
- `Mizan.Api.Tests`: الرحلة كاملة عبر HTTP على PostgreSQL حقيقي في Docker (يحتاج Docker شغّال). ربط البنوك يُختبر ببنك وهمي (`FakeBankingProvider`) بدل Lean.

## الهيكل

```
src/
  Mizan.Domain/          القواعد (لا يعتمد على أي شيء)
  Mizan.Application/     الـ Use Cases وواجهات المستودعات ومعالجات الأحداث
  Mizan.Infrastructure/  EF Core و PostgreSQL والتشفير و JWT
  Mizan.Api/             الـ Endpoints والمصادقة ومعالجة الأخطاء
tests/
  Mizan.Domain.Tests/
  Mizan.Api.Tests/
docs/domain-discovery/   التحليل: Use Cases، Business Rules، Entities، Aggregates، ERD
```

## إضافة Migration

```bash
dotnet ef migrations add <Name> --project src/Mizan.Infrastructure --startup-project src/Mizan.Api --output-dir Persistence/Migrations
```

## قبل أي نشر حقيقي

- `Jwt:SigningKey` يُعطى من متغير بيئة (`Jwt__SigningKey`) أو Secret Manager، ولا يوضع في الملفات.
- `Banking:EncryptionKey` (32 بايت عشوائية بصيغة base64، لتشفير الـ IBAN) ومفاتيح `Lean:*` كذلك. المفتاح الموجود في `appsettings.Development.json` للتطوير فقط.
- الـ Migrations تُطبَّق كخطوة نشر منفصلة، مو عند تشغيل التطبيق.
- مراجعة متطلبات نظام حماية البيانات الشخصية (PDPL) قبل أول مستخدم حقيقي.
