from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.section import WD_SECTION_START
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.style import WD_STYLE_TYPE
from docx.enum.text import WD_BREAK
from pathlib import Path
import re

OUTPUT = Path('Mizan Product Requirements Document.docx')

SOURCE = '''Product Requirements Document — Personal Finance Platform

Version: 0.1
Status: Draft / MVP Planning
Product Type: Personal Finance Management SaaS
Primary Market: Saudi Arabia
Primary Currency: SAR
Target Platform: Web Application + Responsive Mobile Web initially
Future: Mobile Apps / PWA

1. Product Vision
1.1 Vision

بناء منصة تساعد المستخدم على فهم وإدارة أمواله بطريقة ذكية وبسيطة، من خلال:

تقسيم الدخل الشهري.
تحديد Budget لكل نوع من المصروفات.
تتبع المصروفات تلقائيًا.
تحليل نمط الإنفاق.
التنبيه قبل تجاوز الميزانية.
إنشاء Financial Goals.
تقديم Financial Intelligence شخصية.
استخدام AI للمساعدة في التخطيط والتحليل.
لاحقًا ربط الحسابات البنكية من خلال Open Banking.
2. المشكلة

الكثير من المستخدمين يعرفون:

كم راتبي؟

لكن لا يعرفون:

أين يذهب راتبي؟

المشكلة ليست فقط في تسجيل المصروفات.

المشكلة في أن المستخدم غالبًا:

لا يضع Budget واضحة.
لا يعرف كم يستطيع إنفاقه يوميًا.
يكتشف تجاوز الميزانية بعد فوات الأوان.
لا يعرف سبب زيادة مصروفاته.
لا يملك خطة واضحة لأهدافه المالية.
يعتمد على الذاكرة أو Excel أو تطبيقات تسجيل المصروفات اليدوية.
لا يحصل على تحليل شخصي فعلي لسلوكه المالي.
3. الحل

المنصة تحول الراتب إلى خطة مالية قابلة للمتابعة والتحليل.

مثال:

المستخدم:

Salary
10,000 SAR

النظام يساعده على تقسيمها:

Housing        2,000
Food           1,000
Fuel             500
Shopping         800
Entertainment    500
Savings        2,000
Charity          500
Other          2,700

ثم تبدأ المنصة في مقارنة:

Planned Spending
       VS
Actual Spending

وبمرور الوقت تصبح قادرة على إعطاء المستخدم:

أنت أنفقت 27% أكثر على المطاعم مقارنة بالميزانية.

أو:

بناءً على معدل إنفاقك الحالي، ستتجاوز Budget الوقود خلال يومين.

4. Target Users
Primary User

المستخدم الفرد الذي لديه دخل شهري ويريد إدارة مصروفاته.

خصوصًا:

الموظفين.
الشباب.
حديثي التخرج.
الأشخاص الذين بدأوا الاستقلال المالي.
الأشخاص الذين يريدون الادخار لهدف معين.
5. Product Principles

هذه المبادئ ستكون مهمة جدًا أثناء التطوير:

1. Simplicity

المستخدم لا يريد أن يصبح محاسبًا لكي يستخدم التطبيق.

2. Privacy First

البيانات المالية شديدة الحساسية.

3. User Control

المستخدم هو صاحب القرار.

الـ AI يقدم:

Recommendation

وليس:

Financial decision.

4. Intelligence over Data Entry

لا نريد أن يكون التطبيق مجرد:

أدخل مصروفك.

نريد:

التطبيق يفهم نمط إنفاقك ويساعدك.

5. Progressive Complexity

نبدأ بسيطًا، ثم نضيف Open Banking وAI والميزات المتقدمة تدريجيًا.

6. Core Product Concept

لدينا خمسة أجزاء أساسية:

Income
   ↓
Budget
   ↓
Transactions
   ↓
Financial Intelligence
   ↓
Financial Goals

وكلها مرتبطة ببعضها.

7. MVP 0 — Manual Finance Tracker

هذه أول نسخة نبنيها.

لا يوجد Open Banking.

لا يوجد AI معقد.

الهدف:

إثبات أن الـ Core Product نفسه مفيد.

7.1 Authentication

المستخدم يستطيع:

Register
Login
Logout
Refresh Session

لاحقًا:

Google Sign-In
Apple Sign-In
MFA

لكن ليست ضمن MVP الأول.

8. User Profile

المستخدم لديه:

Name
Email
Currency
Monthly Income

Currency في النسخة الأولى:

SAR

لكن الـ Domain يصمم بحيث يمكن دعم عملات أخرى مستقبلًا.

9. Income

المستخدم يدخل دخله الشهري.

مثال:

Monthly Income
10,000 SAR

في MVP الأول:

Manual Input

لاحقًا:

Open Banking
     ↓
Income Detection
     ↓
Suggested Monthly Income
10. Budget

المستخدم يستطيع إنشاء Budget.

مثلاً:

Food             1,000
Fuel               500
Shopping           800
Entertainment      500
Bills            1,500
Savings          2,000
Charity            500

كل Budget Category تحتوي على:

Category
Monthly Limit
Current Spending
Remaining
Percentage Used
11. Transaction

المستخدم يستطيع إضافة Transaction يدويًا.

مثال:

Amount: 50 SAR

Merchant:
XYZ Fuel Station

Category:
Fuel

Date:
05/10/2026

بعد إضافتها:

Fuel Budget

500
 ↓
-50
 ↓
450 remaining
12. Budget Alerts

النظام يراقب استهلاك Budget.

مثلاً:

Budget = 500
Spent = 400

النظام:

⚠️ لقد استخدمت 80% من ميزانية الوقود.

ثم:

Spent = 450

⚠️ تبقى 50 ريال فقط من ميزانية الوقود.

Thresholds

نبدأ مثلًا بـ:

80%
90%
100%

والـ thresholds تكون قابلة للتغيير مستقبلًا.

13. Financial Goals

هذه ستكون من Core Features وليست Feature جانبية.

المستخدم ينشئ هدفًا:

مثال
Goal:
Travel

Target:
10,000 SAR

Current:
3,500 SAR

Target Date:
December 2027

النظام يحسب:

Remaining:
6,500 SAR

والمبلغ المطلوب ادخاره شهريًا.

14. Goal Intelligence

النظام لا يكتفي بعرض Progress.

مثلاً:

تحتاج إلى ادخار 541 ريال شهريًا للوصول إلى هدفك في الموعد المحدد.

ولو المستخدم غير الـ Target Date:

النظام يعيد الحساب.

15. Financial Intelligence

وهذه ستكون أحد أهم أسباب استخدام المنتج.

النظام يحلل:

Spending vs Budget
Food
Budget: 1,000
Actual: 1,270

يعرض:

أنفقت 27% أكثر من ميزانيتك على الطعام هذا الشهر.

16. Spending Forecast

النظام يحلل معدل الإنفاق.

مثال:

Fuel Budget = 500

Spent = 450

Days elapsed = 20

Daily average = 22.5

النظام يستطيع تقدير:

بناءً على معدل إنفاقك الحالي، من المتوقع أن تتجاوز ميزانية الوقود قبل نهاية الشهر.

هذه ستكون بداية الـ Financial Intelligence Engine.

17. Financial Insights

أمثلة:

مصروف المطاعم ارتفع 32% مقارنة بالشهر الماضي.

الوقود يمثل 7% من مصروفاتك هذا الشهر.

أنت تنفق أكثر في نهاية الأسبوع.

لديك 3 Categories تجاوزت Budget هذا الشهر.

إذا استمريت بنفس معدل الإنفاق، ستحتاج إلى تقليل المصروفات بمقدار X للوصول إلى هدفك.

18. AI Assistant

في البداية لا نريد AI يتحكم في النظام.

نستخدمه كمساعد.

Example

المستخدم:

راتبي 10,000 ريال، ساعدني أقسمه.

AI يقترح:

Needs
5,000

Savings
2,000

Lifestyle
2,000

Charity
500

Other
500

ثم المستخدم يستطيع تعديل الأرقام.

19. AI Financial Recommendations

مثال:

أريد شراء سيارة بعد سنة وأحتاج 20,000 ريال.

AI يستطيع اقتراح خطة ادخار بناءً على بيانات المستخدم.

لكن:

Important

الـ AI لا ينفذ تحويلات مالية.

ولا يتخذ قرارات مالية نيابة عن المستخدم.

20. Charity

ميزة اختيارية.

أثناء إنشاء Budget:

هل ترغب في تخصيص جزء من دخلك للصدقة؟

مثلاً:

1%
2.5%
5%
Custom
Skip

إذا اختار 5%:

Salary
10,000

Charity
500

نقدم للمستخدم روابط للجهات الرسمية فقط.

في البداية:

لا يوجد تنفيذ للتبرع داخل التطبيق.

21. MVP 1 — Open Banking

بعد إثبات أن الـ Core Product مفيد، ندخل Open Banking.

الهدف:

إزالة الحاجة إلى إدخال Transactions يدويًا.

Architecture:

User
  ↓
Our Application
  ↓
Licensed Open Banking Provider
  ↓
Bank

مع User Consent.

22. Open Banking Provider

الـ Provider المستهدف مبدئيًا:

Lean Technologies

وسنصمم الـ Integration خلف abstraction بحيث لا يصبح النظام مربوطًا بمزود واحد.

مثلاً conceptually:

IBankingTransactionProvider

ثم:

LeanBankingProvider

وفي المستقبل:

OtherBankingProvider
23. Bank Transactions

بعد الربط:

Transaction
    ↓
Amount
Merchant
Date
Account
Status

ويمكن الاستفادة من بيانات التصنيف التي يقدمها مزود Open Banking.

24. Transaction Categorization

النظام يحاول معرفة:

Merchant
     ↓
Category

مثال:

ALBAIK
↓
Food
↓
Fast Food

أو:

Fuel Station
↓
Transportation
↓
Fuel
25. Categorization Strategy

لن نعتمد على AI فقط.

نستخدم:

Provider Category
        ↓
Our Merchant Rules
        ↓
Known Merchant Database
        ↓
AI Fallback
        ↓
User Confirmation

وبالتالي يصبح النظام أفضل مع الوقت.

26. MVP 2 — Advanced Financial Intelligence

بعد توفر Transaction Data حقيقية، نوسع التحليل.

Monthly Comparison
September
Food: 1,200

October
Food: 1,600

Increase: 33%
Forecasting

متوقع تجاوز Budget بمقدار 220 ريال.

Anomaly Detection

مثلاً:

هذه العملية أعلى بكثير من متوسط إنفاقك المعتاد في هذه الفئة.

لكن يجب أن تكون هذه تنبيهات وليست اتهامات.

27. Financial Health

في مرحلة متقدمة نقدر نحسب مؤشرًا داخليًا:

Financial Health Score

مبني على:

Budget adherence
Savings rate
Goal progress
Spending consistency
Debt/obligation ratio إذا أضفناها مستقبلًا.

لكن هذا ليس Financial Credit Score.

28. Notifications

أنواع Notifications:

Budget Alert

استخدمت 80% من ميزانية المطاعم.

Budget Exceeded

تجاوزت ميزانية المطاعم.

Goal Reminder

تحتاج إلى ادخار 500 ريال هذا الشهر للوصول إلى هدف السفر.

Forecast Alert

بناءً على إنفاقك الحالي، قد تتجاوز ميزانية الوقود خلال 3 أيام.

Monthly Summary

ملخصك المالي لشهر أكتوبر جاهز.

29. Dashboard

الـ Dashboard الرئيسية يجب أن تكون بسيطة.

مثلاً:

┌───────────────────────────┐
│ Monthly Income            │
│ 10,000 SAR                │
└───────────────────────────┘

┌───────────────────────────┐
│ Available                 │
│ 4,230 SAR                 │
└───────────────────────────┘

Budget Usage
██████████░░ 72%

Top Spending
🍔 Food          1,240
⛽ Fuel            420
🛍 Shopping        800

Goals
✈️ Travel         45%

ثم:

Financial Insight

⚠️ Food spending is 24% higher than your monthly budget.

30. Architecture Direction

المشروع سيكون Full Stack:

Frontend
   ↓
ASP.NET Core API
   ↓
Application
   ↓
Domain
   ↓
Infrastructure
   ↓
Oracle

ومكونات مستقبلية:

                    ┌── Oracle
                    │
API → Application ──┼── Redis
                    │
                    ├── Open Banking
                    │
                    ├── AI Provider
                    │
                    └── Notifications
31. Domain Areas

مبدئيًا نتوقع Domains مثل:

User
Budget
BudgetCategory
Transaction
FinancialGoal
Income
Notification
FinancialInsight

لكن لا نقرر Aggregates الآن.

هذه ستكون مرحلة Domain Discovery بعد الـ PRD.

32. Security Requirements

لأننا نتعامل مع Financial Data:

HTTPS
Authentication
Authorization
Secure token handling
Encryption where appropriate
Sensitive data protection
Audit logging
Input validation
Rate limiting
Secure secrets management
No financial credentials stored by our system.

وبالنسبة لـ Open Banking:

نحن لا نخزن Bank Passwords.

المستخدم يمر بعملية authentication/consent التي يوفرها البنك أو مزود Open Banking.

33. Privacy

المستخدم يجب أن يعرف:

ما البيانات التي نجمعها.
لماذا نحتاجها.
كيف نستخدمها.
كيف يستطيع إلغاء الربط.
كيف يستطيع حذف حسابه.
كيف يتعامل النظام مع البيانات المالية.

وهذا سيكون جزءًا أساسيًا من Production وليس مجرد صفحة Terms.

34. MVP Scope
✅ MVP 0

ننفذ فقط:

Authentication
User Profile

Monthly Income

Budget
Budget Categories

Manual Transactions

Budget Tracking

Budget Alerts

Financial Goals

Basic Financial Intelligence

Basic Dashboard

Basic Notifications
❌ Not in MVP 0

لا نضيف:

Open Banking
AI
Mobile Native App
WhatsApp
Payments
Bank Transfers
ZATCA
Debt Management
Investments
Credit Score

هذه كلها Future Scope.

35. MVP 1

بعد نجاح MVP 0:

Open Banking
        ↓
Lean Integration

Accounts
Balances
Transactions
Income Insights
Spending Insights

ثم:

Automatic Transaction Import
Automatic Categorization
36. MVP 2
Advanced Financial Intelligence

Forecasting
Anomaly Detection
Monthly Comparisons
Spending Trends
Goal Recommendations
AI Assistant
37. MVP 3
Advanced Product

Recurring Expenses
Subscriptions
Debt Tracking
Family Budget
Shared Goals
Advanced Reports
Mobile App / PWA
38. Production Phase

إذا أثبت الـ MVP نجاحه:

Cloud Deployment
CI/CD
Monitoring
Logging
Tracing
Backups
Disaster Recovery
Security Hardening
Performance Optimization
Scaling

ثم نبدأ دراسة المتطلبات القانونية والتنظيمية للإطلاق التجاري في السعودية بشكل رسمي.

39. Business Model

لن نقرر الـ pricing الآن.

لكن لدينا خيارات مستقبلية:

Freemium
Free
Basic Budget
Manual Transactions
Basic Goals

Premium
Open Banking
Advanced Intelligence
AI
Advanced Reports

أو:

Subscription

مثلاً:

Monthly
Annual

أو:

Hybrid

Free + Premium.

لن نختار النموذج قبل اختبار MVP.

40. Success Metrics

هذه مهمة جدًا.

نجاح MVP ليس:

"اشتغل بدون bugs."

نجاحه هو أن الناس يستمرون في استخدامه.

نقيس:

Activation

كم مستخدم:

سجل → وضع راتبه → أنشأ Budget؟

Engagement

كم مرة يدخل المستخدم؟

Budget Usage

كم مستخدم يسجل Transactions؟

Goal Usage

كم مستخدم أنشأ Financial Goal؟

Retention

هل رجع بعد أسبوع؟

هل رجع بعد شهر؟

Intelligence Value

هل المستخدم يرى Insights مفيدة؟

Open Banking

كم شخص مستعد يربط حسابه البنكي؟

وهذه الأخيرة ستكون مؤشر ثقة مهم جدًا.

41. MVP User Journey

المستخدم الجديد:

Register
   ↓
Welcome
   ↓
Monthly Income
   ↓
Financial Goals
   ↓
Budget Setup
   ↓
Dashboard
   ↓
Add Transaction
   ↓
Budget Updated
   ↓
Insight
   ↓
Notification

لاحقًا:

Connect Bank
   ↓
Consent
   ↓
Import Transactions
   ↓
Categorization
   ↓
Budget Matching
   ↓
Financial Intelligence
42. مثال كامل

راتب:

10,000 SAR

Budget:

Food       1,000
Fuel         500
Shopping     800
Bills      1,500
Savings    2,000
Charity      500

المستخدم يدفع:

Fuel Station
50 SAR

النظام:

Fuel
500
 ↓
450

بعد عدة عمليات:

Fuel
450 → 50

Notification:

⚠️ بقي 50 ريال من ميزانية الوقود.

ثم Financial Intelligence:

معدل إنفاقك اليومي على الوقود 30 ريال، وبناءً على ذلك من المتوقع تجاوز ميزانيتك خلال يومين.

ثم في نهاية الشهر:

أنفقت 580 ريال على الوقود، متجاوزًا ميزانيتك بـ16%.

ثم النظام يلاحظ:

متوسط إنفاقك على الوقود خلال آخر 3 أشهر هو 570 ريال.

ويستطيع اقتراح:

قد تكون ميزانية 600 ريال أكثر واقعية لك.

وهنا تبدأ قيمة المنتج الحقيقية.

43. ما لن نفعله

أريد أن تكون هذه قاعدة المشروع:

لن نبني Feature لمجرد أنها "تقنية حلوة".

مثلاً:

"خلنا نضيف Microservices."

لا.

"خلنا نضيف Kafka."

لا.

"خلنا نضيف Kubernetes."

ليس الآن.

"خلنا نستخدم CQRS لأنه تعلمناه."

لا.

كل Technology يجب أن تجيب عن:

ما المشكلة التي تحلها؟

إذا احتجناها، نستخدمها.

إذا لا، لا نستخدمها.

44. أهم قرار معماري من البداية

نريد أن يكون النظام:

Core Domain
     ↑
Independent

ولا نريد:

Domain
 ↓
Lean API

أو:

Domain
 ↓
Oracle

أو:

Domain
 ↓
AI Provider

الـ Domain يجب أن يبقى مستقلًا.

مثلاً:

Transaction
Budget
Goal

لا تعرف أي شيء عن:

Lean
OpenAI
Oracle
ASP.NET
HTTP

وهذا سيكون اختبارًا ممتازًا لما تعلمته في مشروع Order Management.

45. أول Milestone لنا

لا نبدأ Coding بعد.

المرحلة التالية ستكون:

Domain Discovery

سنأخذ الـ PRD هذا ونحول الـ Business إلى:

Actors
Use Cases
Entities
Value Objects
Aggregates
Domain Events
Business Rules
Bounded Contexts

ثم نرسم:

Context Map

وبعدها:

ERD

وبعدها:

Architecture

وبعدها فقط:

dotnet new

😂

46. Definition of Done للـ MVP 0

قبل أن نقول:

MVP جاهز

يجب أن يستطيع المستخدم:

إنشاء حساب.
تسجيل الدخول.
إدخال راتبه.
إنشاء Budget.
تحديد Categories.
إنشاء Financial Goal.
إضافة Transaction.
رؤية الرصيد المتبقي لكل Budget.
استقبال Budget Alert.
رؤية Dashboard.
رؤية مقارنة Planned vs Actual.
رؤية Financial Insights الأساسية.
رؤية تقدم أهدافه.

بدون Open Banking.

إذا حققنا هذا بشكل نظيف ومختبر، عندها ننتقل إلى:

Open Banking Integration with Lean.'''

MONO = 'Menlo'
BODY = 'Arial'

def set_cell_shading(cell, fill):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:fill'), fill)
    tcPr.append(shd)

def set_cell_border(cell, color='D9D9D9'):
    tc = cell._tc
    tcPr = tc.get_or_add_tcPr()
    tcBorders = tcPr.first_child_found_in('w:tcBorders')
    if tcBorders is None:
        tcBorders = OxmlElement('w:tcBorders')
        tcPr.append(tcBorders)
    for edge in ('top', 'left', 'bottom', 'right'):
        tag = 'w:' + edge
        element = tcBorders.find(qn(tag))
        if element is None:
            element = OxmlElement(tag)
            tcBorders.append(element)
        element.set(qn('w:val'), 'single')
        element.set(qn('w:sz'), '6')
        element.set(qn('w:color'), color)

def set_rtl(paragraph, rtl=True):
    pPr = paragraph._p.get_or_add_pPr()
    bidi = OxmlElement('w:bidi')
    bidi.set(qn('w:val'), '1' if rtl else '0')
    pPr.append(bidi)
    for run in paragraph.runs:
        rPr = run._r.get_or_add_rPr()
        rt = OxmlElement('w:rtl')
        rt.set(qn('w:val'), '1' if rtl else '0')
        rPr.append(rt)

def set_keep_with_next(paragraph):
    pPr = paragraph._p.get_or_add_pPr()
    keep = OxmlElement('w:keepNext')
    pPr.append(keep)

def is_arabic(text):
    return bool(re.search(r'[\u0600-\u06FF]', text))

def is_heading(line):
    return bool(re.match(r'^\d+\.\s', line)) and not re.match(r'^\d+\.\d+\s', line)

def is_subheading(line):
    return bool(re.match(r'^\d+\.\d+\s', line))

def is_diagram_line(line):
    return any(ch in line for ch in '↓↑┌┐└┘│├┤─→') or line.startswith('████')

def is_label(line):
    labels = {'Primary User', 'Thresholds', 'Example', 'Important', 'Architecture:', 'Forecasting', 'Anomaly Detection', 'Advanced Financial Intelligence', 'Advanced Product', 'Freemium', 'Premium', 'Subscription', 'Hybrid', 'Activation', 'Engagement', 'Budget Usage', 'Goal Usage', 'Retention', 'Intelligence Value', 'Open Banking', 'Monthly Comparison', 'Financial Health Score', 'Budget Alert', 'Budget Exceeded', 'Goal Reminder', 'Forecast Alert', 'Monthly Summary', 'Top Spending', 'Goals', 'Financial Insight'}
    return line in labels or line.endswith(':') or line in {'مثال', 'أو', 'ثم', 'لاحقًا', 'في البداية', 'النظام', 'المستخدم', 'يعرض', 'نقيس'}

def add_para(doc, text, style='Body'):
    p = doc.add_paragraph(style=style)
    p.add_run(text)
    if style == 'Mono':
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        set_rtl(p, False)
    else:
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        set_rtl(p)
    return p

doc = Document()
section = doc.sections[0]
section.page_width = Inches(8.5)
section.page_height = Inches(11)
section.top_margin = Inches(0.75)
section.bottom_margin = Inches(0.70)
section.left_margin = Inches(0.82)
section.right_margin = Inches(0.82)

styles = doc.styles
normal = styles['Normal']
normal.font.name = BODY
normal._element.rPr.rFonts.set(qn('w:eastAsia'), BODY)
normal.font.size = Pt(11.5)
normal.font.color.rgb = RGBColor(0, 0, 0)
normal.paragraph_format.space_after = Pt(7)

for name, size, bold, space_before, space_after in [
    ('Title', 30, True, 0, 18),
    ('Heading 1', 17, True, 20, 9),
    ('Heading 2', 13.5, True, 14, 7),
]:
    s = styles[name]
    s.font.name = BODY
    s._element.rPr.rFonts.set(qn('w:eastAsia'), BODY)
    s.font.size = Pt(size)
    s.font.bold = bold
    s.font.color.rgb = RGBColor(0, 0, 0)
    s.paragraph_format.space_before = Pt(space_before)
    s.paragraph_format.space_after = Pt(space_after)

body = styles.add_style('Body', WD_STYLE_TYPE.PARAGRAPH)
body.base_style = normal
body.font.name = BODY
body.font.size = Pt(11.5)
body.paragraph_format.space_after = Pt(7)
body.paragraph_format.line_spacing = 1.18

label = styles.add_style('Label', WD_STYLE_TYPE.PARAGRAPH)
label.base_style = body
label.font.name = BODY
label.font.size = Pt(11.5)
label.font.bold = True
label.paragraph_format.space_before = Pt(8)
label.paragraph_format.space_after = Pt(5)

mono = styles.add_style('Mono', WD_STYLE_TYPE.PARAGRAPH)
mono.base_style = body
mono.font.name = MONO
mono._element.rPr.rFonts.set(qn('w:eastAsia'), MONO)
mono.font.size = Pt(9.2)
mono.paragraph_format.space_after = Pt(2)
mono.paragraph_format.line_spacing = 1.0

# Cover page
p = doc.add_paragraph(style='Title')
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.add_run('Mizan')
p = doc.add_paragraph()
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = p.add_run('Product Requirements Document')
r.font.name = BODY; r.font.size = Pt(16); r.font.bold = True; r.font.color.rgb = RGBColor(0, 0, 0)
p = doc.add_paragraph()
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = p.add_run('Personal Finance Platform')
r.font.name = BODY; r.font.size = Pt(12); r.font.color.rgb = RGBColor(0, 0, 0)

doc.add_paragraph('')
meta = doc.add_table(rows=0, cols=2)
meta.alignment = WD_TABLE_ALIGNMENT.CENTER
meta.autofit = False
for key, value in [
    ('Version', '0.1'), ('Status', 'Draft / MVP Planning'),
    ('Product Type', 'Personal Finance Management SaaS'), ('Primary Market', 'Saudi Arabia'),
    ('Primary Currency', 'SAR'), ('Target Platform', 'Web Application + Responsive Mobile Web initially'),
    ('Future', 'Mobile Apps / PWA')
]:
    cells = meta.add_row().cells
    cells[0].width = Inches(4.7); cells[1].width = Inches(1.6)
    for cell in cells:
        set_cell_border(cell)
        cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    set_cell_shading(cells[1], '1F4E79')
    p0 = cells[0].paragraphs[0]; p0.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    rr = p0.add_run(value); rr.font.name = BODY; rr.font.size = Pt(10.5); rr.font.color.rgb = RGBColor(0,0,0)
    set_rtl(p0)
    p1 = cells[1].paragraphs[0]; p1.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    rr = p1.add_run(key); rr.font.name = BODY; rr.font.size = Pt(10); rr.font.bold = True; rr.font.color.rgb = RGBColor(255,255,255)
    set_rtl(p1)

doc.add_page_break()

lines = SOURCE.splitlines()
metadata_done = False
for idx, raw in enumerate(lines):
    line = raw.rstrip()
    if not line:
        continue
    if idx == 0:
        p = doc.add_paragraph(style='Title')
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        p.add_run(line)
        set_rtl(p)
        continue
    if re.match(r'^(Version|Status|Product Type|Primary Market|Primary Currency|Target Platform|Future):', line):
        continue
    if is_heading(line):
        p = doc.add_paragraph(style='Heading 1')
        p.add_run(line)
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT; set_rtl(p)
        set_keep_with_next(p)
        continue
    if is_subheading(line):
        p = doc.add_paragraph(style='Heading 2')
        p.add_run(line)
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT; set_rtl(p)
        set_keep_with_next(p)
        continue
    if line.startswith(('✅ ', '❌ ')):
        p = doc.add_paragraph(style='Heading 2')
        p.add_run(line)
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT; set_rtl(p)
        set_keep_with_next(p)
        continue
    if is_diagram_line(line):
        add_para(doc, line, 'Mono')
    elif is_label(line):
        add_para(doc, line, 'Label')
    else:
        add_para(doc, line, 'Body')

# Footer page number field
for sec in doc.sections:
    footer = sec.footer
    p = footer.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run('Mizan  |  ')
    r.font.name = BODY; r.font.size = Pt(8); r.font.color.rgb = RGBColor(89, 89, 89)
    fld = OxmlElement('w:fldSimple')
    fld.set(qn('w:instr'), 'PAGE')
    p._p.append(fld)

doc.core_properties.title = 'Mizan Product Requirements Document'
doc.core_properties.subject = 'Personal Finance Platform'
doc.core_properties.author = 'Mizan'
doc.save(OUTPUT)
print(OUTPUT)
