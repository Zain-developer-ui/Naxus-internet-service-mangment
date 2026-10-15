"""
Renders the markdown guides to PDF with reportlab.

A browser-based converter was the first choice, but headless Edge declines to
write the PDF under this sandbox, so the layout is drawn directly. That means
the markup subset is handled explicitly rather than delegated to an HTML engine:
headings, paragraphs, tables, lists, fenced code, blockquotes, rules.

Run:  python .workbuddy-ai/tmp/make-pdfs.py
"""

import os
import re
import sys

from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.platypus import (
    BaseDocTemplate, Frame, PageTemplate, Paragraph, Spacer, Table, TableStyle,
    Preformatted, KeepTogether, PageBreak, HRFlowable
)

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "deliverables", "guides")

GUIDES = [
    dict(
        md="USER-MANUAL.md",
        pdf="NEXUS-User-Manual.pdf",
        title="NEXUS Service Marketing System",
        subtitle="User Manual",
        accent=colors.HexColor("#1e4d3f"),
        accent_light=colors.HexColor("#e8efec"),
    ),
    dict(
        md="DEVELOPER-GUIDE.md",
        pdf="NEXUS-Developer-Guide.pdf",
        title="NEXUS Service Marketing System",
        subtitle="Developer Guide",
        accent=colors.HexColor("#2c3e70"),
        accent_light=colors.HexColor("#e9ecf5"),
    ),
    dict(
        md="INSTALLATION-GUIDE.md",
        pdf="NEXUS-Installation-Guide.pdf",
        title="NEXUS Service Marketing System",
        subtitle="Installation & Deployment Guide",
        accent=colors.HexColor("#7a3b1e"),
        accent_light=colors.HexColor("#f5ede8"),
    ),
]


class Guide:
    """Holds the styles and the document template for one guide."""

    def __init__(self, spec):
        self.spec = spec
        self.accent = spec["accent"]
        self.accent_light = spec["accent_light"]
        self._build_styles()

    def _build_styles(self):
        a = self.accent

        self.body = ParagraphStyle(
            "body", fontName="Times-Roman", fontSize=9.8, leading=14.2,
            textColor=colors.HexColor("#1c1c1c"), spaceAfter=6, alignment=TA_LEFT)

        self.h1 = ParagraphStyle(
            "h1", fontName="Helvetica-Bold", fontSize=17, leading=21,
            textColor=colors.HexColor("#111111"), spaceBefore=2, spaceAfter=9)

        self.h2 = ParagraphStyle(
            "h2", fontName="Helvetica-Bold", fontSize=12.6, leading=16,
            textColor=a, spaceBefore=15, spaceAfter=6, keepWithNext=1)

        self.h3 = ParagraphStyle(
            "h3", fontName="Helvetica-Bold", fontSize=10.6, leading=14,
            textColor=colors.HexColor("#222222"), spaceBefore=11, spaceAfter=4,
            keepWithNext=1)

        self.h4 = ParagraphStyle(
            "h4", fontName="Helvetica-BoldOblique", fontSize=9.8, leading=13,
            textColor=colors.HexColor("#444444"), spaceBefore=9, spaceAfter=3,
            keepWithNext=1)

        self.code = ParagraphStyle(
            "code", fontName="Courier", fontSize=7.7, leading=10.4,
            textColor=colors.HexColor("#1a1a1a"))

        self.bullet = ParagraphStyle(
            "bullet", parent=self.body, leftIndent=13, bulletIndent=4,
            spaceAfter=2.5)

        self.quote = ParagraphStyle(
            "quote", parent=self.body, fontSize=9.2, leading=13,
            leftIndent=9, rightIndent=6, textColor=colors.HexColor("#3a3a3a"))

        self.cell = ParagraphStyle(
            "cell", fontName="Helvetica", fontSize=7.9, leading=10.6,
            textColor=colors.HexColor("#1c1c1c"))

        self.cell_head = ParagraphStyle(
            "cell_head", fontName="Helvetica-Bold", fontSize=7.9, leading=10.6,
            textColor=colors.white)

        self.cover_mark = ParagraphStyle(
            "cover_mark", fontName="Helvetica-Bold", fontSize=54, leading=56,
            textColor=a)

        self.cover_title = ParagraphStyle(
            "cover_title", fontName="Helvetica-Bold", fontSize=22, leading=27,
            textColor=colors.HexColor("#111111"), spaceAfter=6)

        self.cover_sub = ParagraphStyle(
            "cover_sub", fontName="Helvetica", fontSize=13.5, leading=18,
            textColor=colors.HexColor("#555555"), spaceAfter=26)

        self.cover_meta = ParagraphStyle(
            "cover_meta", fontName="Helvetica", fontSize=9, leading=15,
            textColor=colors.HexColor("#555555"))

        self.toc_entry = ParagraphStyle(
            "toc_entry", fontName="Helvetica", fontSize=9.6, leading=16,
            textColor=colors.HexColor("#1c1c1c"))

    # ---- page furniture ----

    def page(self, canvas, doc):
        canvas.saveState()

        # left accent rule down the whole page
        canvas.setFillColor(self.accent)
        canvas.rect(0, 0, 3.2 * mm, A4[1], stroke=0, fill=1)

        # footer: guide name on the left, page number on the right
        canvas.setFont("Helvetica", 7.4)
        canvas.setFillColor(colors.HexColor("#8a8a8a"))
        canvas.drawString(18 * mm, 12 * mm,
                          f"NEXUS — {self.spec['subtitle']}")
        canvas.drawRightString(A4[0] - 18 * mm, 12 * mm, str(doc.page))

        canvas.setStrokeColor(colors.HexColor("#dddddd"))
        canvas.setLineWidth(0.4)
        canvas.line(18 * mm, 15.5 * mm, A4[0] - 18 * mm, 15.5 * mm)

        canvas.restoreState()

    def cover_page(self, canvas, doc):
        canvas.saveState()

        canvas.setFillColor(self.accent)
        canvas.rect(0, 0, A4[0], A4[1], stroke=0, fill=1)

        # a lighter panel so the text sits on something other than flat colour
        canvas.setFillColor(colors.white)
        canvas.rect(0, 0, A4[0], A4[1] * 0.34, stroke=0, fill=1)

        canvas.setFillColor(self.accent)
        canvas.rect(0, A4[1] * 0.34 - 2.4, A4[0], 2.4, stroke=0, fill=1)

        canvas.align = "left"
        canvas.drawString(26 * mm, A4[1] - 96 * mm, "N")
        canvas.setFont("Helvetica-Bold", 54)
        canvas.drawString(26 * mm, A4[1] - 96 * mm, "N")

        canvas.setFont("Helvetica-Bold", 25)
        canvas.drawString(26 * mm, A4[1] - 118 * mm, "NEXUS")

        canvas.setFont("Helvetica", 11)
        canvas.drawString(26 * mm, A4[1] - 130 * mm,
                          "Service Marketing System")

        canvas.setStrokeColor(colors.white)
        canvas.setLineWidth(1.6)
        canvas.line(26 * mm, A4[1] - 140 * mm, 66 * mm, A4[1] - 140 * mm)

        canvas.setFont("Helvetica-Bold", 17)
        canvas.drawString(26 * mm, A4[1] - 158 * mm, self.spec["subtitle"])

        canvas.setFillColor(colors.HexColor("#555555"))
        canvas.setFont("Helvetica", 9.2)
        y = 68 * mm
        for label, value in (
            ("Version", "1.0"),
            ("Date", "October 2026"),
            ("Project", "Aptech eProject"),
            ("Platform", "ASP.NET Core 8 MVC"),
            ("Database", "Microsoft SQL Server"),
        ):
            canvas.setFont("Helvetica-Bold", 9.2)
            canvas.drawString(26 * mm, y, label)
            canvas.setFont("Helvetica", 9.2)
            canvas.drawString(58 * mm, y, value)
            y -= 7.4 * mm

        canvas.restoreState()

    def toc_page(self, canvas, doc):
        canvas.saveState()
        canvas.setFillColor(self.accent)
        canvas.rect(0, 0, 3.2 * mm, A4[1], stroke=0, fill=1)
        canvas.setFont("Helvetica", 7.4)
        canvas.setFillColor(colors.HexColor("#8a8a8a"))
        canvas.drawString(18 * mm, 12 * mm, f"NEXUS — {self.spec['subtitle']}")
        canvas.drawRightString(A4[0] - 18 * mm, 12 * mm, str(doc.page))
        canvas.restoreState()


# ---------------------------------------------------------------- markdown

def esc(text):
    """Escape for reportlab's mini-HTML, then apply inline markdown."""
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    text = re.sub(r"`([^`]+)`",
                  r'<font face="Courier" color="#9c2a2a">\1</font>', text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"(?<![\w*])\*([^*\n]+)\*(?![\w*])", r"<i>\1</i>", text)
    text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r'\1', text)
    return text


def split_row(line):
    return [c.strip() for c in line.strip().strip("|").split("|")]


def is_table_sep(line):
    return bool(re.fullmatch(r"\|[\s:|-]+\|", line.strip()))


def build_table(rows, guide):
    head = split_row(rows[0])
    body = [split_row(r) for r in rows[2:]]

    data = [[Paragraph(esc(c), guide.cell_head) for c in head]]
    for r in body:
        padded = (r + [""] * len(head))[:len(head)]
        data.append([Paragraph(esc(c), guide.cell) for c in padded])

    width = A4[0] - 36 * mm
    if len(head) == 2:
        widths = [width * 0.36, width * 0.64]
    elif len(head) == 3:
        widths = [width * 0.26, width * 0.37, width * 0.37]
    elif len(head) == 4:
        widths = [width / 4] * 4
    else:
        widths = [width / len(head)] * len(head)

    t = Table(data, colWidths=widths, repeatRows=1, hAlign="LEFT")

    style = [
        ("BACKGROUND", (0, 0), (-1, 0), guide.accent),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 3.4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3.4),
        ("LEFTPADDING", (0, 0), (-1, -1), 4.4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4.4),
        ("LINEBELOW", (0, 0), (-1, -2), 0.35, colors.HexColor("#e2e2e2")),
        ("BOX", (0, 0), (-1, -1), 0.4, colors.HexColor("#cccccc")),
    ]
    for i in range(1, len(data)):
        if i % 2 == 0:
            style.append(("BACKGROUND", (0, i), (-1, i),
                          colors.HexColor("#f7f7f5")))

    t.setStyle(TableStyle(style))
    return t


def code_block(text, guide):
    pre = Preformatted(text, guide.code, maxLineLength=95)
    t = Table([[pre]], colWidths=[A4[0] - 36 * mm], hAlign="LEFT")
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#f6f6f3")),
        ("BOX", (0, 0), (-1, -1), 0.4, colors.HexColor("#dddddd")),
        ("LINEBEFORE", (0, 0), (0, -1), 2.2, guide.accent),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    return t


def callout(inner_lines, guide):
    paras = []
    for chunk in inner_lines:
        if chunk.strip():
            paras.append(Paragraph(esc(chunk.strip()), guide.quote))

    if not paras:
        return Spacer(1, 1)

    t = Table([[paras]], colWidths=[A4[0] - 36 * mm], hAlign="LEFT")
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#f8f8f4")),
        ("LINEBEFORE", (0, 0), (0, -1), 2.2, colors.HexColor("#c9a227")),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
        ("TOPPADDING", (0, 0), (-1, -1), 6),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
    ]))
    return t


def parse(md_text, guide):
    """Markdown subset -> reportlab flowables."""
    lines = md_text.split("\n")
    flow = []
    i = 0

    while i < len(lines):
        line = lines[i]

        # fenced code
        if line.strip().startswith("```"):
            i += 1
            buf = []
            while i < len(lines) and not lines[i].strip().startswith("```"):
                buf.append(lines[i])
                i += 1
            i += 1
            flow.append(Spacer(1, 3))
            flow.append(code_block("\n".join(buf), guide))
            flow.append(Spacer(1, 7))
            continue

        # table
        if (line.strip().startswith("|") and i + 1 < len(lines)
                and is_table_sep(lines[i + 1])):
            rows = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                rows.append(lines[i])
                i += 1
            flow.append(Spacer(1, 3))
            flow.append(build_table(rows, guide))
            flow.append(Spacer(1, 8))
            continue

        # heading
        m = re.match(r"^(#{1,4})\s+(.*)$", line)
        if m:
            level = len(m.group(1))
            text = esc(m.group(2).strip())
            style = {1: guide.h1, 2: guide.h2, 3: guide.h3, 4: guide.h4}[level]

            if level == 2:
                flow.append(Spacer(1, 5))
                flow.append(Paragraph(text, style))
                flow.append(HRFlowable(width="100%", thickness=0.9,
                                       color=guide.accent,
                                       spaceBefore=2, spaceAfter=7))
            else:
                flow.append(Paragraph(text, style))
            i += 1
            continue

        # horizontal rule
        if re.fullmatch(r"-{3,}", line.strip()):
            flow.append(Spacer(1, 5))
            flow.append(HRFlowable(width="100%", thickness=0.4,
                                   color=colors.HexColor("#d5d5d5"),
                                   spaceBefore=3, spaceAfter=8))
            i += 1
            continue

        # blockquote
        if line.strip().startswith(">"):
            buf = []
            while i < len(lines) and lines[i].strip().startswith(">"):
                buf.append(lines[i].lstrip().lstrip(">").strip())
                i += 1
            flow.append(Spacer(1, 2))
            flow.append(callout(buf, guide))
            flow.append(Spacer(1, 8))
            continue

        # list
        if re.match(r"^\s*([-*]|\d+\.)\s+", line):
            ordered = bool(re.match(r"^\s*\d+\.", line))
            items = []
            while i < len(lines) and re.match(r"^\s*([-*]|\d+\.)\s+", lines[i]):
                items.append(re.sub(r"^\s*([-*]|\d+\.)\s+", "", lines[i]))
                i += 1

            for n, item in enumerate(items, 1):
                bullet = f"{n}." if ordered else "•"
                flow.append(Paragraph(esc(item), guide.bullet,
                                      bulletText=bullet))
            flow.append(Spacer(1, 4))
            continue

        # blank
        if not line.strip():
            i += 1
            continue

        # paragraph
        buf = []
        while (i < len(lines) and lines[i].strip()
               and not re.match(r"^(#{1,4}\s|```|\||>\s?|\s*([-*]|\d+\.)\s)", lines[i])
               and not re.fullmatch(r"-{3,}", lines[i].strip())):
            buf.append(lines[i].strip())
            i += 1

        text = " ".join(buf)
        if text:
            flow.append(Paragraph(esc(text), guide.body))

    return flow


def cover_flowables(guide):
    return [
        Paragraph(f'<font color="#ffffff">{guide.spec["title"]}</font>',
                  guide.cover_title),
        Spacer(1, 4),
        Paragraph(f'<font color="#ffffff">{guide.spec["subtitle"]}</font>',
                  guide.cover_sub),
    ]


# ---------------------------------------------------------------- main

def make_doc(spec):
    guide = Guide(spec)
    out_pdf = os.path.join(SRC, spec["pdf"])
    md_path = os.path.join(SRC, spec["md"])

    md_text = open(md_path, encoding="utf-8").read()

    # The title block at the top becomes the cover, so the body starts at the
    # first real section.
    start = md_text.find("\n## ")
    body_md = md_text[start:] if start > 0 else md_text
    body = parse(body_md, guide)

    doc = BaseDocTemplate(
        out_pdf, pagesize=A4,
        leftMargin=18 * mm, rightMargin=18 * mm,
        topMargin=18 * mm, bottomMargin=20 * mm,
        title=f"{spec['title']} — {spec['subtitle']}",
        author="NEXUS", subject=spec["subtitle"])

    frame = Frame(doc.leftMargin, doc.bottomMargin,
                  doc.width, doc.height, id="main")

    doc.addPageTemplates([
        PageTemplate(id="cover", frames=[frame], onPage=guide.cover_page),
        PageTemplate(id="toc", frames=[Frame(
            doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id="toc")],
            onPage=guide.toc_page),
        PageTemplate(id="body", frames=[frame], onPage=guide.page),
    ])

    # contents are generated from the h2 headings of the body
    heads = re.findall(r"^##\s+(.*)$", body_md, flags=re.M)

    toc = [Paragraph("Contents", guide.h2), Spacer(1, 8)]
    for n, h in enumerate(heads, 1):
        toc.append(Paragraph(f"{n}. &nbsp;{esc(h.strip())}", guide.toc_entry))

    flow = []
    flow.append(Spacer(1, A4[1] * 0.30))          # cover text sits low
    flow.extend(cover_flowables(guide))
    flow.append(PageBreak())
    flow.extend(toc)
    flow.append(PageBreak())
    flow.extend(body)

    doc.build(flow)
    return out_pdf


def main():
    for spec in GUIDES:
        out = make_doc(spec)
        size = os.path.getsize(out) / 1024
        print(f"OK {spec['pdf']}  {size:.0f} KB")


if __name__ == "__main__":
    sys.exit(main())
