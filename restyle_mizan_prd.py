import ast
import re
from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.style import WD_STYLE_TYPE
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

OUTPUT = 'Mizan Product Requirements Document.docx'
SOURCE_FILE = Path('build_mizan_prd.py')
NAVY, PALE, GRID, BODY, MONO = '1F4E79', 'F3F7FA', 'D9D9D9', 'Arial', 'Menlo'

tree = ast.parse(SOURCE_FILE.read_text(encoding='utf-8'))
SOURCE = next(
    node.value.value for node in tree.body
    if isinstance(node, ast.Assign)
    and any(isinstance(target, ast.Name) and target.id == 'SOURCE' for target in node.targets)
)

def shade(cell, fill):
    props = cell._tc.get_or_add_tcPr()
    node = OxmlElement('w:shd')
    node.set(qn('w:fill'), fill)
    props.append(node)

def border(cell):
    props = cell._tc.get_or_add_tcPr()
    borders = props.first_child_found_in('w:tcBorders')
    if borders is None:
        borders = OxmlElement('w:tcBorders')
        props.append(borders)
    for side in ('top', 'left', 'bottom', 'right'):
        node = borders.find(qn('w:' + side))
        if node is None:
            node = OxmlElement('w:' + side)
            borders.append(node)
        node.set(qn('w:val'), 'single')
        node.set(qn('w:sz'), '6')
        node.set(qn('w:color'), GRID)

def rtl(paragraph, value=True):
    props = paragraph._p.get_or_add_pPr()
    node = OxmlElement('w:bidi')
    node.set(qn('w:val'), '1' if value else '0')
    props.append(node)
    for run in paragraph.runs:
        run_props = run._r.get_or_add_rPr()
        run_node = OxmlElement('w:rtl')
        run_node.set(qn('w:val'), '1' if value else '0')
        run_props.append(run_node)

def keep_next(paragraph):
    node = OxmlElement('w:keepNext')
    paragraph._p.get_or_add_pPr().append(node)

def repeat_header(row):
    node = OxmlElement('w:tblHeader')
    node.set(qn('w:val'), 'true')
    row._tr.get_or_add_trPr().append(node)

def is_arabic(text):
    return bool(re.search(r'[\u0600-\u06FF]', text))

def is_heading(text):
    return bool(re.match(r'^\d+\.\s', text)) and not bool(re.match(r'^\d+\.\d+\s', text))

def is_subheading(text):
    return bool(re.match(r'^\d+\.\d+\s', text))

def is_arrow(text):
    return bool(re.fullmatch(r'[\s↓↑→←│├┤┌┐└┘─]+', text))

def is_flow(lines):
    return any(is_arrow(line) or '→' in line for line in lines)

def is_money_list(lines):
    return len(lines) >= 3 and all(re.match(r'^.+?\s{2,}[\d,]+(?:\s+SAR)?$', line) for line in lines)

def is_key_value(lines):
    return len(lines) >= 2 and all(':' in line for line in lines)

def is_metric_box(lines):
    return lines and lines[0].startswith('┌') and lines[-1].startswith('└')

def put_cell(cell, text, bold=False, white=False, size=9.5, align=WD_ALIGN_PARAGRAPH.RIGHT, bidi=True):
    paragraph = cell.paragraphs[0]
    paragraph.alignment = align
    paragraph.paragraph_format.space_after = Pt(0)
    run = paragraph.add_run(text)
    run.font.name = BODY
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.color.rgb = RGBColor(255,255,255) if white else RGBColor(0,0,0)
    if bidi:
        rtl(paragraph)
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    border(cell)

def table(doc, headers, rows, widths, rtl_columns=None):
    output = doc.add_table(rows=1, cols=len(headers))
    output.alignment = WD_TABLE_ALIGNMENT.CENTER
    output.autofit = False
    for cell, text, width in zip(output.rows[0].cells, headers, widths):
        cell.width = Inches(width)
        shade(cell, NAVY)
        put_cell(cell, text, True, True, 9.2)
    repeat_header(output.rows[0])
    for index, row in enumerate(rows):
        cells = output.add_row().cells
        for idx, (cell, text, width) in enumerate(zip(cells, row, widths)):
            cell.width = Inches(width)
            shade(cell, 'FFFFFF' if index % 2 == 0 else PALE)
            put_cell(cell, text, False, False, 9.2, bidi=(rtl_columns is None or idx in rtl_columns))
    doc.add_paragraph('').paragraph_format.space_after = Pt(2)
    return output

def heading(doc, text, level=1):
    paragraph = doc.add_paragraph(style='Heading ' + str(level))
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    paragraph.add_run(text)
    rtl(paragraph)
    keep_next(paragraph)

def body(doc, text, style='Body'):
    paragraph = doc.add_paragraph(style=style)
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    paragraph.add_run(text)
    rtl(paragraph)
    return paragraph

def bullet(doc, text):
    paragraph = doc.add_paragraph(style='Bullet')
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    paragraph.add_run(text)
    rtl(paragraph)

def diagram(doc, lines):
    t = doc.add_table(rows=1, cols=1)
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = False
    c = t.cell(0, 0)
    c.width = Inches(4.9)
    shade(c, NAVY)
    put_cell(c, 'Flow', True, True, 9.2, WD_ALIGN_PARAGRAPH.CENTER, False)
    repeat_header(t.rows[0])
    for line in lines:
        c = t.add_row().cells[0]
        c.width = Inches(4.9)
        if is_arrow(line):
            shade(c, 'FFFFFF')
            put_cell(c, line.strip(), True, False, 10, WD_ALIGN_PARAGRAPH.CENTER, False)
        else:
            shade(c, PALE)
            put_cell(c, line.strip(), True, False, 10, WD_ALIGN_PARAGRAPH.CENTER, is_arabic(line))
    doc.add_paragraph('').paragraph_format.space_after = Pt(2)

def metric_card(doc, lines):
    content = [line.strip('│ ').strip() for line in lines if line.startswith('│')]
    if len(content) >= 2:
        table(doc, ['Metric', 'Value'], [(content[0], content[1])], [2.0, 1.65])
    else:
        for line in lines:
            body(doc, line)

def label(doc, text):
    paragraph = doc.add_paragraph(style='Label')
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    paragraph.add_run(text)
    rtl(paragraph)

def content_block(doc, lines):
    lines = [line.rstrip() for line in lines if line.strip()]
    if not lines:
        return
    if is_metric_box(lines):
        metric_card(doc, lines)
        return
    if is_flow(lines):
        diagram(doc, lines)
        return
    if is_money_list(lines):
        rows = []
        for line in lines:
            match = re.match(r'^(.*?)(\s{2,})([\d,]+(?:\s+SAR)?)$', line)
            rows.append((match.group(1).strip(), match.group(3).strip()))
        table(doc, ['Category', 'Amount'], rows, [4.25, 2.3])
        return
    if is_key_value(lines):
        rows = [tuple(part.strip() for part in line.split(':', 1)) for line in lines]
        table(doc, ['Field', 'Value'], rows, [2.05, 4.5])
        return
    if len(lines) == 3 and lines[1].strip().upper() == 'VS':
        table(doc, [lines[0], lines[2]], [('', '')], [3.25, 3.25])
        return
    if len(lines) >= 3:
        for line in lines:
            bullet(doc, line)
        return
    if len(lines) == 2 and (lines[0].endswith(':') or re.fullmatch(r'[A-Za-z][A-Za-z /-]*', lines[0])):
        table(doc, ['Field', 'Value'], [(lines[0].rstrip(':'), lines[1])], [2.05, 4.5])
        return
    if len(lines) == 1:
        known_labels = {'Primary User', 'Thresholds', 'Example', 'Important', 'Architecture:', 'Forecasting', 'Anomaly Detection', 'Advanced Financial Intelligence', 'Advanced Product', 'Freemium', 'Premium', 'Subscription', 'Hybrid', 'Activation', 'Engagement', 'Budget Usage', 'Goal Usage', 'Retention', 'Intelligence Value', 'Open Banking', 'Monthly Comparison', 'Financial Health Score', 'Budget Alert', 'Budget Exceeded', 'Goal Reminder', 'Forecast Alert', 'Monthly Summary', 'Top Spending', 'Goals', 'Financial Insight'}
        if lines[0] in known_labels or lines[0].endswith(':'):
            label(doc, lines[0])
        else:
            body(doc, lines[0])
        return
    for line in lines:
        body(doc, line)

doc = Document()
sec = doc.sections[0]
sec.page_width, sec.page_height = Inches(8.5), Inches(11)
sec.top_margin, sec.bottom_margin = Inches(0.70), Inches(0.68)
sec.left_margin, sec.right_margin = Inches(0.78), Inches(0.78)

styles = doc.styles
styles['Normal'].font.name = BODY
styles['Normal']._element.rPr.rFonts.set(qn('w:eastAsia'), BODY)
styles['Normal'].font.size = Pt(11)
for name, size, before, after in [('Title', 29, 0, 18), ('Heading 1', 16.5, 20, 9), ('Heading 2', 13.5, 14, 7)]:
    style = styles[name]
    style.font.name = BODY
    style._element.rPr.rFonts.set(qn('w:eastAsia'), BODY)
    style.font.size = Pt(size)
    style.font.bold = True
    style.font.color.rgb = RGBColor(0,0,0)
    style.paragraph_format.space_before = Pt(before)
    style.paragraph_format.space_after = Pt(after)
body_style = styles.add_style('Body', WD_STYLE_TYPE.PARAGRAPH)
body_style.base_style = styles['Normal']
body_style.font.name = BODY
body_style.font.size = Pt(11)
body_style.paragraph_format.line_spacing = 1.18
body_style.paragraph_format.space_after = Pt(7)
label_style = styles.add_style('Label', WD_STYLE_TYPE.PARAGRAPH)
label_style.base_style = body_style
label_style.font.name = BODY
label_style.font.size = Pt(11)
label_style.font.bold = True
label_style.paragraph_format.space_before = Pt(8)
label_style.paragraph_format.space_after = Pt(5)
bullet_style = styles.add_style('Bullet', WD_STYLE_TYPE.PARAGRAPH)
bullet_style.base_style = body_style
bullet_style.font.name = BODY
bullet_style.paragraph_format.right_indent = Inches(0.22)
bullet_style.paragraph_format.first_line_indent = Inches(-0.16)
bullet_style.paragraph_format.space_after = Pt(3)
arrow_style = styles.add_style('Arrow', WD_STYLE_TYPE.PARAGRAPH)
arrow_style.base_style = body_style
arrow_style.font.name = MONO
arrow_style.font.size = Pt(11)
arrow_style.font.bold = True
arrow_style.paragraph_format.space_after = Pt(1)
arrow_style.paragraph_format.space_before = Pt(1)

# Cover
p = doc.add_paragraph(style='Title')
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
p.add_run('Mizan')
p = doc.add_paragraph()
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = p.add_run('Product Requirements Document')
r.font.name = BODY; r.font.size = Pt(16); r.font.bold = True
p = doc.add_paragraph()
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
r = p.add_run('Personal Finance Platform')
r.font.name = BODY; r.font.size = Pt(12)
doc.add_paragraph('')
table(doc, ['Value', 'Field'], [
    ('0.1', 'Version'),
    ('Draft / MVP Planning', 'Status'),
    ('Personal Finance Management SaaS', 'Product Type'),
    ('Saudi Arabia', 'Primary Market'),
    ('SAR', 'Primary Currency'),
    ('Web Application + Responsive Mobile Web initially', 'Target Platform'),
    ('Mobile Apps / PWA', 'Future')
], [4.95, 1.45])
doc.add_page_break()

blocks = SOURCE.split('\n\n')
first_title = True
for raw_block in blocks:
    source_lines = [line.rstrip() for line in raw_block.splitlines() if line.strip()]
    if not source_lines:
        continue
    if first_title and source_lines[0] == 'Product Requirements Document — Personal Finance Platform':
        p = doc.add_paragraph(style='Title')
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        p.add_run(source_lines[0])
        rtl(p)
        source_lines = source_lines[1:]
        first_title = False
    buffer = []
    for line in source_lines:
        metadata = re.match(r'^(Version|Status|Product Type|Primary Market|Primary Currency|Target Platform|Future):', line)
        if metadata:
            continue
        section_kind = 1 if is_heading(line) else 2 if is_subheading(line) or line.startswith(('✅ ', '❌ ')) else None
        if section_kind:
            content_block(doc, buffer)
            buffer = []
            heading(doc, line, section_kind)
            continue
        buffer.append(line)
    content_block(doc, buffer)

for section in doc.sections:
    p = section.footer.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run('Mizan  |  ')
    r.font.name = BODY; r.font.size = Pt(8); r.font.color.rgb = RGBColor(89,89,89)
    fld = OxmlElement('w:fldSimple')
    fld.set(qn('w:instr'), 'PAGE')
    p._p.append(fld)

doc.core_properties.title = 'Mizan Product Requirements Document'
doc.core_properties.subject = 'Personal Finance Platform'
doc.core_properties.author = 'Mizan'
doc.save(OUTPUT)
print(OUTPUT)
