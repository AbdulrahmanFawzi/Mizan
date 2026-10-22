from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.style import WD_STYLE_TYPE
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

OUT = 'Mizan MVP 0 Delivery Roadmap.docx'
NAVY, PALE, BORDER, BODY = '1F4E79', 'F3F7FA', 'D9D9D9', 'Arial'

def tc_shade(cell, fill):
    props = cell._tc.get_or_add_tcPr()
    node = OxmlElement('w:shd'); node.set(qn('w:fill'), fill); props.append(node)

def tc_border(cell):
    props = cell._tc.get_or_add_tcPr()
    borders = props.first_child_found_in('w:tcBorders')
    if borders is None:
        borders = OxmlElement('w:tcBorders'); props.append(borders)
    for side in ('top', 'left', 'bottom', 'right'):
        node = borders.find(qn('w:' + side))
        if node is None:
            node = OxmlElement('w:' + side); borders.append(node)
        node.set(qn('w:val'), 'single'); node.set(qn('w:sz'), '6'); node.set(qn('w:color'), BORDER)

def keep_next(p):
    node = OxmlElement('w:keepNext')
    p._p.get_or_add_pPr().append(node)

def repeat_header(row):
    node = OxmlElement('w:tblHeader'); node.set(qn('w:val'), 'true')
    row._tr.get_or_add_trPr().append(node)

def cell_text(cell, text, bold=False, white=False, size=9.2, align=None):
    p = cell.paragraphs[0]; p.paragraph_format.space_after = Pt(0)
    if align is not None: p.alignment = align
    r = p.add_run(text); r.font.name = BODY; r.font.size = Pt(size); r.font.bold = bold
    r.font.color.rgb = RGBColor(255,255,255) if white else RGBColor(0,0,0)
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    tc_border(cell)

def title(doc, text, level=1):
    p = doc.add_paragraph(style='Heading ' + str(level)); p.add_run(text); keep_next(p); return p

def para(doc, text, lead=None):
    p = doc.add_paragraph(style='Body')
    if lead and text.startswith(lead):
        p.add_run(lead).bold = True; p.add_run(text[len(lead):])
    else: p.add_run(text)
    return p

def bullet(doc, text):
    p = doc.add_paragraph(style='Bullet'); p.add_run(text); return p

def simple_table(doc, headers, rows, widths):
    table = doc.add_table(rows=1, cols=len(headers)); table.alignment = WD_TABLE_ALIGNMENT.CENTER; table.autofit = False
    for c, h, w in zip(table.rows[0].cells, headers, widths):
        c.width = Inches(w); tc_shade(c, NAVY); cell_text(c, h, True, True, 9, WD_ALIGN_PARAGRAPH.CENTER)
    repeat_header(table.rows[0])
    for i, row in enumerate(rows):
        cells = table.add_row().cells
        for c, value, w in zip(cells, row, widths):
            c.width = Inches(w); tc_shade(c, 'FFFFFF' if i % 2 == 0 else PALE); cell_text(c, value, False, False, 8.7)
    return table

def milestone(doc, code, name, outcome, scope, deps, criteria, exit_note):
    title(doc, f'{code}  {name}', 2)
    para(doc, outcome, 'Outcome: ')
    simple_table(doc, ['Planning element', 'Detail'], [('Scope', scope), ('Technical dependencies', deps)], [1.48, 5.75])
    para(doc, 'Acceptance criteria:', 'Acceptance criteria:')
    for item in criteria: bullet(doc, item)
    para(doc, 'Milestone exit:', 'Milestone exit:')
    bullet(doc, exit_note)

doc = Document()
sec = doc.sections[0]
sec.page_width, sec.page_height = Inches(8.5), Inches(11)
sec.top_margin, sec.bottom_margin = Inches(0.70), Inches(0.68)
sec.left_margin, sec.right_margin = Inches(0.72), Inches(0.72)

styles = doc.styles
styles['Normal'].font.name = BODY; styles['Normal']._element.rPr.rFonts.set(qn('w:eastAsia'), BODY); styles['Normal'].font.size = Pt(10.5)
for style_name, size, before, after in [('Title', 27, 0, 14), ('Heading 1', 16, 18, 8), ('Heading 2', 12.5, 14, 6)]:
    style = styles[style_name]; style.font.name = BODY; style._element.rPr.rFonts.set(qn('w:eastAsia'), BODY)
    style.font.size = Pt(size); style.font.bold = True; style.font.color.rgb = RGBColor(0,0,0)
    style.paragraph_format.space_before = Pt(before); style.paragraph_format.space_after = Pt(after)
body_style = styles.add_style('Body', WD_STYLE_TYPE.PARAGRAPH); body_style.base_style = styles['Normal']; body_style.font.name = BODY; body_style.font.size = Pt(10.5)
body_style.paragraph_format.space_after = Pt(6); body_style.paragraph_format.line_spacing = 1.08
bullet_style = styles.add_style('Bullet', WD_STYLE_TYPE.PARAGRAPH); bullet_style.base_style = body_style
bullet_style.paragraph_format.left_indent = Inches(0.22); bullet_style.paragraph_format.first_line_indent = Inches(-0.16); bullet_style.paragraph_format.space_after = Pt(3)

p = doc.add_paragraph(style='Title'); p.alignment = WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Mizan MVP 0 Delivery Roadmap')
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = p.add_run('Personal Finance Platform'); r.font.name = BODY; r.font.size = Pt(13); r.font.bold = True
p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = p.add_run('Prioritized milestones acceptance criteria technical dependencies'); r.font.name = BODY; r.font.size = Pt(10.5); r.font.color.rgb = RGBColor(89,89,89)
doc.add_paragraph('')
cover_rows = [
    ('Scope', 'MVP 0 only'),
    ('Target', 'Prove the manual personal finance core product is useful'),
    ('Excluded', 'Open Banking, AI, native mobile app, payments, transfers, ZATCA, debt management, investments, and credit score'),
    ('Architecture direction', 'Web application and ASP.NET Core API with Application, Domain, Infrastructure, and Oracle')
]
simple_table(doc, ['Planning element', 'Detail'], cover_rows, [1.7, 5.5])
doc.add_page_break()

title(doc, 'Roadmap outcome and sequencing')
para(doc, 'This roadmap prioritizes the smallest end-to-end flow that proves value: a user creates an account, sets monthly income and budgets, records manual spending, sees the budget change, receives an alert, tracks a goal, and sees a basic financial insight. Each milestone is complete only when its acceptance criteria pass in a working environment.')
para(doc, 'Architectural guardrail: The milestone order protects the core domain from infrastructure details. The Domain does not depend on Oracle, ASP.NET, Lean, OpenAI, or HTTP. Oracle and the API are implementation dependencies behind the application boundary.', 'Architectural guardrail: ')

title(doc, 'Priority map')
simple_table(doc, ['Milestone', 'Priority', 'User or product outcome', 'Depends on'], [
    ('M0', 'P0', 'A runnable secure foundation and a protected domain boundary', 'None'),
    ('M1', 'P0', 'A user can register, sign in, maintain a profile, and enter monthly income', 'M0'),
    ('M2', 'P0', 'A user can create budget categories and monthly limits', 'M1'),
    ('M3', 'P0', 'A manual transaction changes budget tracking and triggers threshold alerts', 'M2'),
    ('M4', 'P0', 'A user can track a goal and receive basic planned versus actual insight', 'M3'),
    ('M5', 'P0', 'A usable dashboard, notifications, verified user journey, and release readiness', 'M3 and M4')
], [1.0, 0.7, 3.75, 1.65])

title(doc, 'Milestones')
milestone(doc, 'M0', 'Foundation and vertical slice',
    'Outcome: Establish a runnable solution with the intended boundaries before building business features.',
    'Solution structure for Web/API, Application, Domain, Infrastructure, and Oracle; configuration and secret handling; database migration workflow; authentication plumbing; health check; automated build and test baseline.',
    'Oracle access, a chosen authentication and token implementation, configuration and secret storage, and a repeatable local database environment.',
    ['The solution builds and its automated test suite runs from a clean checkout.', 'A local environment can start the API, apply Oracle migrations, and reach a protected test endpoint.', 'Domain tests reference neither Oracle nor ASP.NET packages.', 'Secrets and connection strings are not committed to source control, and configuration supports separate environments.', 'Authentication, authorization, input validation, and structured error handling have one consistent API path.'],
    'A thin authenticated API request reaches the Application layer and persists through Infrastructure without violating the Domain boundary.')

milestone(doc, 'M1', 'Identity profile and monthly income',
    'Outcome: A user has a private finance workspace with the profile and monthly income required for all later calculations.',
    'Register, login, logout, refresh session; name, email, currency, and monthly income. Currency is SAR in MVP 0 while the model remains extendable for future currencies.',
    'M0 authentication flow, user persistence, password and token security policy, and money/date abstractions.',
    ['A new user can register and sign in with valid credentials; invalid credentials do not create a session.', 'A signed-in user can log out and refresh an active session according to the selected token policy.', 'A user can store and later retrieve name, email, currency, and monthly income; MVP input uses SAR.', 'One user cannot retrieve or update another user’s profile or income.', 'Monthly income is represented as a money value and is available to the current monthly planning flow.'],
    'A user can sign in again and see the same profile and monthly income without access to any other user’s data.')

milestone(doc, 'M2', 'Budget setup and categories',
    'Outcome: A user turns monthly income into a plan of categories and monthly limits.',
    'Create and manage budget categories with category name and monthly limit. Track current spending, remaining amount, and percentage used. Charity is supported as a normal category if the user chooses it.',
    'M1 user identity and income context; money precision rules; active-month calendar rules; persistence for budgets and categories.',
    ['A signed-in user can create a budget category with a valid name and monthly limit in SAR.', 'A newly created category reports zero current spending, remaining equal to its monthly limit, and zero percent used.', 'The user can view their categories and monthly limits for the active month.', 'Category and budget data is isolated by user.', 'The budget model exposes the values needed to compare planned spending against actual spending.'],
    'At least one category exists for the current user and its calculated budget fields are correct before any transaction is added.')

milestone(doc, 'M3', 'Manual transactions budget tracking and alerts',
    'Outcome: Manual spending immediately becomes observable and actionable against a budget.',
    'Manual transaction capture with amount, merchant, category, and date; budget tracking; thresholds at 80%, 90%, and 100%; basic in-app notification records for alerts.',
    'M2 categories and monthly budget rules; transaction persistence; a consistent rule for mapping transaction date to a budget month; notification persistence.',
    ['A user can add a transaction with amount, merchant, category, and date to one of their own budget categories.', 'Adding a 50 SAR Fuel transaction to a 500 SAR Fuel budget reports 50 SAR current spending and 450 SAR remaining for the applicable month.', 'Budget percentage used and remaining amount are calculated from planned limit and actual categorized spending.', 'Crossing the 80%, 90%, or 100% threshold creates the corresponding budget alert for the user.', 'A transaction cannot be assigned to another user’s category, and invalid amounts or dates are rejected.'],
    'The Fuel example from the PRD works end-to-end, including a visible update to the remaining budget and an alert when a threshold is crossed.')

milestone(doc, 'M4', 'Financial goals and basic intelligence',
    'Outcome: The user can see progress toward a goal and receive a clear deterministic explanation of spending versus plan.',
    'Financial goals with name, target amount, current amount, and target date; remaining amount and required monthly saving calculation; basic planned-versus-actual insights and forecast language based on current rate.',
    'M3 reliable dated transactions and budget calculations; active-month and month-end date rules; goal calculation rules; insight projection inputs.',
    ['A user can create a goal with target amount, current amount, and target date, then view remaining amount and required monthly saving.', 'Changing a goal target date recalculates the required monthly saving amount.', 'The system can report that Food actual spending of 1,270 SAR is 27% above a 1,000 SAR budget.', 'A forecast identifies a likely budget overrun when the current spend rate would exceed the monthly limit before month end.', 'Insights are advisory only; no feature transfers money or makes a financial decision for the user.'],
    'A user can see goal progress and at least one basic planned-versus-actual insight based on their own persisted data.')

milestone(doc, 'M5', 'Dashboard notifications and MVP release readiness',
    'Outcome: The complete MVP 0 journey is coherent, testable, secure, and ready for controlled user validation.',
    'Dashboard for monthly income, available amount, budget usage, top spending, goals, and one financial insight; notification view; end-to-end testing; security and operational checks needed for an MVP release.',
    'M3 transaction, budget, and alerts; M4 goals and insights; dashboard read models; notification view; deployment, monitoring, logging, and backup decisions.',
    ['The dashboard presents monthly income, budget usage, top spending, goal progress, and a basic insight from the user’s current data.', 'Budget alerts and goal reminders are visible to the intended user in the notification experience.', 'The full path passes: register, enter income, create budget categories, add a transaction, observe budget update and alert, create a goal, and view dashboard insight.', 'Authorization, input validation, rate limiting, audit logging, and sensitive-data handling are verified for MVP 0 public API paths.', 'Automated tests cover core calculations and the primary end-to-end user journey; production configuration has monitoring, logging, backups, and recovery ownership defined for the MVP environment.'],
    'Every item in the PRD Definition of Done for MVP 0 is demonstrated without Open Banking.')

title(doc, 'Technical dependency decisions')
para(doc, 'These decisions are prerequisites for predictable delivery. They do not add customer-facing scope; they define how MVP 0 behavior remains consistent and testable.')
simple_table(doc, ['Dependency', 'Decision required', 'Used by'], [
    ('Money and currency', 'Use a decimal money representation with SAR in MVP 0; keep currency attached to monetary values so multi-currency support remains possible later.', 'M1 to M5'),
    ('Monthly calendar', 'Define the active budget month and how a transaction date maps to it. Use the same rule for tracking, alerts, forecasts, dashboard, and goals.', 'M1 to M5'),
    ('Identity and sessions', 'Choose password storage, access-token, refresh-token, session-expiry, and logout invalidation policies. Enforce user ownership on every finance query and command.', 'M0 to M5'),
    ('Oracle persistence', 'Adopt migration, indexing, connection, backup, and environment policies behind Infrastructure. Domain code must not reference Oracle.', 'M0 to M5'),
    ('Notification delivery', 'Persist and display in-app notifications for MVP 0. Do not require email, WhatsApp, or push delivery to satisfy the scope.', 'M3 to M5'),
    ('Calculations and time', 'Provide one testable date and time source. Keep percentage, remaining amount, threshold, and forecast calculations deterministic and unit tested.', 'M2 to M5'),
    ('Observability and security', 'Define audit events, structured logs, rate limits, sensitive-field handling, monitoring, and recovery ownership before controlled release.', 'M0 and M5')
], [1.55, 4.55, 1.15])

title(doc, 'Definition of done checklist')
para(doc, 'MVP 0 is ready for controlled validation only when the following user-visible outcomes are verified in the deployed environment:')
for item in ['Account creation and sign-in work securely.', 'The user can enter monthly income and create budget categories.', 'The user can create a financial goal and add a manual transaction.', 'Remaining budget, percentage used, and planned-versus-actual information are correct.', 'A budget alert is received after a threshold is crossed.', 'Dashboard, basic financial insights, and goal progress are visible.', 'No Open Banking integration is required for this release.']:
    bullet(doc, item)

title(doc, 'Release gates')
for item in ['Product gate: the end-to-end scenario is demonstrated against realistic test data and accepted by the product owner.', 'Quality gate: core calculation tests, authorization tests, and primary journey tests pass in CI.', 'Security gate: no financial credentials are stored; secrets are managed outside source control; API validation and rate limiting are enabled.', 'Operations gate: logs, monitoring, backups, and an owner for recovery are in place for the MVP environment.', 'Scope gate: no Open Banking, AI, payments, transfers, debt management, investments, or credit score has entered the MVP 0 release.']:
    bullet(doc, item)

for section in doc.sections:
    p = section.footer.paragraphs[0]; p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run('Mizan  |  MVP 0 Delivery Roadmap  |  '); r.font.name = BODY; r.font.size = Pt(8); r.font.color.rgb = RGBColor(89,89,89)
    field = OxmlElement('w:fldSimple'); field.set(qn('w:instr'), 'PAGE'); p._p.append(field)
doc.core_properties.title = 'Mizan MVP 0 Delivery Roadmap'
doc.core_properties.subject = 'Prioritized milestones acceptance criteria and technical dependencies'
doc.core_properties.author = 'Mizan'
doc.save(OUT)
print(OUT)
