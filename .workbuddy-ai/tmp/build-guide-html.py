"""
Markdown -> print-ready HTML for the NEXUS guides.

Rationale: the PDFs rendered by reportlab looked wrong (tables collapsed into
paragraphs, code blocks bleeding into prose, no visual hierarchy). Rather than
fight the reportlab layout engine, emit HTML with real print CSS and let the
user's browser / a converter do the pagination.
"""

import html
import os
import re

GUIDES = [
    {
        "src": "USER-MANUAL.md",
        "out": "NEXUS-User-Manual.html",
        "title": "NEXUS Service Marketing System",
        "subtitle": "User Manual",
    },
    {
        "src": "DEVELOPER-GUIDE.md",
        "out": "NEXUS-Developer-Guide.html",
        "title": "NEXUS Service Marketing System",
        "subtitle": "Developer Guide",
    },
    {
        "src": "INSTALLATION-GUIDE.md",
        "out": "NEXUS-Installation-Guide.html",
        "title": "NEXUS Service Marketing System",
        "subtitle": "Installation & Deployment Guide",
    },
]


def inline(text):
    """Inline markdown -> HTML.

    Code spans are lifted out first, because a code span may legitimately
    contain *, _ or ` and must not take part in the emphasis passes. The
    placeholder is plain text (no punctuation) so the emphasis regexes below
    still see the markers around it as adjacent to a word character.
    """
    spans = []

    def stash(m):
        spans.append(m.group(1))
        return "\ue000%d\ue001" % (len(spans) - 1)

    text = re.sub(r"`([^`]+)`", stash, text)
    text = html.escape(text, quote=False)

    # links [label](href)
    text = re.sub(
        r"\[([^\]]+)\]\(([^)]+)\)",
        r'<a href="\2">\1</a>',
        text,
    )
    # bold, then italic - a lone * left by the bold pass is not an issue
    # because the bold pass consumes its own markers
    text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text, flags=re.S)
    text = re.sub(r"(?<![*\w])\*([^*\n]+?)\*(?![*\w])", r"<em>\1</em>", text)

    def unstash(m):
        return "<code>%s</code>" % html.escape(spans[int(m.group(1))], quote=False)

    return re.sub(r"\ue000(\d+)\ue001", unstash, text)


def slug(text):
    s = re.sub(r"<[^>]+>", "", text)
    s = re.sub(r"[^a-z0-9\s-]", "", s.lower())
    return re.sub(r"\s+", "-", s.strip())


def join_list_items(lines):
    """Fold continuation lines into the list item they belong to.

    A wrapped item looks like:

        - **Some point.** The sentence carries on
          onto the next line.

    Without this the parser treats the indented tail as a new item, which also
    splits inline spans that straddle the break (``**bold across\\nlines**``).
    """
    out = []
    for line in lines:
        if (out and out[-1].strip() and re.match(r"^\s+\S", line)
                and re.match(r"^\s*([-*]|\d+\.)\s+", out[-1])):
            out[-1] = out[-1].rstrip() + " " + line.strip()
        else:
            out.append(line)
    return out


def parse(lines):
    """Markdown subset -> list of (tag, html) blocks.

    Handles: h1-h4, fenced code, tables, blockquotes, ordered/unordered lists,
    horizontal rules and paragraphs. Everything else is a paragraph.
    """
    lines = join_list_items(lines)
    out = []
    i = 0
    n = len(lines)

    while i < n:
        line = lines[i]
        stripped = line.strip()

        # fenced code
        if stripped.startswith("```"):
            lang = stripped[3:].strip()
            i += 1
            body = []
            while i < n and not lines[i].strip().startswith("```"):
                body.append(lines[i])
                i += 1
            i += 1
            cls = ' class="lang-%s"' % lang if lang else ""
            out.append(("code", "<pre%s><code>%s</code></pre>"
                        % (cls, html.escape("\n".join(body), quote=False))))
            continue

        # horizontal rule
        if re.fullmatch(r"-{3,}", stripped):
            out.append(("hr", "<hr>"))
            i += 1
            continue

        # heading
        m = re.match(r"^(#{1,4})\s+(.*)$", stripped)
        if m:
            level = len(m.group(1))
            text = inline(m.group(2).strip())
            out.append(("h%d" % level,
                        '<h%d id="%s">%s</h%d>' % (level, slug(m.group(2)), text, level)))
            i += 1
            continue

        # table: header row followed by a separator row
        if (stripped.startswith("|") and i + 1 < n
                and re.fullmatch(r"\|[\s:|-]+\|", lines[i + 1].strip())):
            head = [c.strip() for c in stripped.strip("|").split("|")]
            i += 2
            rows = []
            while i < n and lines[i].strip().startswith("|"):
                rows.append([c.strip() for c in lines[i].strip().strip("|").split("|")])
                i += 1
            parts = ['<table><thead><tr>']
            parts += ["<th>%s</th>" % inline(c) for c in head]
            parts.append("</tr></thead><tbody>")
            for r in rows:
                parts.append("<tr>")
                parts += ["<td>%s</td>" % inline(c) for c in r]
                parts.append("</tr>")
            parts.append("</tbody></table>")
            out.append(("table", "".join(parts)))
            continue

        # blockquote
        if stripped.startswith(">"):
            body = []
            while i < n and lines[i].strip().startswith(">"):
                body.append(lines[i].strip().lstrip(">").strip())
                i += 1
            out.append(("quote", '<div class="callout">%s</div>'
                        % " ".join(inline(b) for b in body if b)))
            continue

        # unordered list
        if re.match(r"^[-*]\s+", stripped):
            items = []
            while i < n and re.match(r"^[-*]\s+", lines[i].strip()):
                items.append(inline(lines[i].strip()[2:].strip()))
                i += 1
            out.append(("ul", "<ul>%s</ul>"
                        % "".join("<li>%s</li>" % x for x in items)))
            continue

        # ordered list
        if re.match(r"^\d+\.\s+", stripped):
            items = []
            while i < n and re.match(r"^\d+\.\s+", lines[i].strip()):
                items.append(inline(re.sub(r"^\d+\.\s+", "", lines[i].strip())))
                i += 1
            out.append(("ol", "<ol>%s</ol>"
                        % "".join("<li>%s</li>" % x for x in items)))
            continue

        # blank
        if not stripped:
            i += 1
            continue

        # paragraph
        para = []
        while (i < n and lines[i].strip()
               and not re.match(r"^(#{1,4}\s|[-*]\s|\d+\.\s|>|\||```)", lines[i].strip())
               and not re.fullmatch(r"-{3,}", lines[i].strip())):
            para.append(lines[i].strip())
            i += 1
        out.append(("p", "<p>%s</p>" % inline(" ".join(para))))

    return out


def build_toc(blocks):
    """Numbered rows for h2/h3, matching the printed document."""
    rows = []
    for tag, content in blocks:
        if tag not in ("h2", "h3"):
            continue
        level = int(tag[1])
        text = re.sub(r"<[^>]+>", "", content)
        anchor = re.search(r'id="([^"]+)"', content)
        if not anchor:
            continue
        rows.append(
            '<li class="toc-l%d"><a href="#%s">%s</a></li>'
            % (level, anchor.group(1), html.escape(text))
        )
    return '<ul class="toc">%s</ul>' % "".join(rows)


CSS = """
:root {
  --ink: #16181d;
  --ink-soft: #4a4f5a;
  --ink-faint: #767c88;
  --rule: #dfe2e8;
  --rule-soft: #eceef2;
  --accent: #1f3a63;
  --accent-soft: #eef2f8;
  --code-bg: #f6f7f9;
  --page: 210mm;
}

* { box-sizing: border-box; }

html { -webkit-text-size-adjust: 100%; }

body {
  margin: 0;
  font-family: "Segoe UI", "Helvetica Neue", Arial, sans-serif;
  font-size: 10.5pt;
  line-height: 1.62;
  color: var(--ink);
  background: #f1f2f4;
}

.sheet {
  max-width: var(--page);
  margin: 0 auto;
  background: #fff;
  padding: 22mm 20mm 18mm;
  box-shadow: 0 1px 24px rgba(20, 24, 34, .10);
}

/* ---- cover ---- */
.cover {
  border-bottom: 3px solid var(--accent);
  padding-bottom: 12mm;
  margin-bottom: 10mm;
}
.cover .mark {
  display: inline-block;
  background: var(--accent);
  color: #fff;
  font-weight: 700;
  font-size: 15pt;
  letter-spacing: .08em;
  padding: 5px 12px 6px;
  border-radius: 3px;
  margin-bottom: 9mm;
}
.cover h1 {
  font-size: 27pt;
  line-height: 1.16;
  margin: 0 0 4mm;
  letter-spacing: -.015em;
  color: var(--ink);
  border: 0;
  padding: 0;
}
.cover .subtitle {
  font-size: 15pt;
  font-weight: 400;
  color: var(--accent);
  margin: 0 0 9mm;
}
.cover dl {
  margin: 0;
  display: grid;
  grid-template-columns: max-content 1fr;
  gap: 2mm 8mm;
  font-size: 9.5pt;
}
.cover dt { color: var(--ink-faint); }
.cover dd { margin: 0; color: var(--ink-soft); }

/* ---- toc ---- */
.toc-block { margin-bottom: 12mm; }
.toc-block h2 {
  font-size: 11pt;
  text-transform: uppercase;
  letter-spacing: .12em;
  color: var(--ink-faint);
  border: 0;
  padding: 0 0 3mm;
  margin: 0 0 4mm;
}
ul.toc { list-style: none; margin: 0; padding: 0; }
ul.toc li { border-bottom: 1px dotted var(--rule); }
ul.toc a {
  display: block;
  padding: 2.2mm 0;
  color: var(--ink);
  text-decoration: none;
  font-size: 10pt;
}
ul.toc .toc-l3 a {
  padding-left: 8mm;
  color: var(--ink-soft);
  font-size: 9.5pt;
}

/* ---- headings ---- */
h1, h2, h3, h4 { line-height: 1.28; }
h2 {
  font-size: 15pt;
  margin: 13mm 0 4mm;
  padding-bottom: 2.5mm;
  border-bottom: 1.5px solid var(--rule);
  color: var(--ink);
}
h3 {
  font-size: 12pt;
  margin: 9mm 0 3mm;
  color: var(--accent);
}
h4 {
  font-size: 10.5pt;
  margin: 7mm 0 2mm;
  color: var(--ink);
}
h2:first-of-type { margin-top: 0; }
h2 + h3, h2 + p, h3 + p { margin-top: 0; }

p { margin: 0 0 3.4mm; }
a { color: var(--accent); }

/* ---- lists ---- */
ul, ol { margin: 0 0 3.6mm; padding-left: 6.5mm; }
li { margin-bottom: 1.4mm; }
li > ul, li > ol { margin-top: 1.4mm; margin-bottom: 0; }

/* ---- code ---- */
code {
  font-family: "Cascadia Mono", Consolas, "Courier New", monospace;
  font-size: .875em;
  background: var(--code-bg);
  border: 1px solid var(--rule-soft);
  border-radius: 3px;
  padding: .1em .32em;
  color: #23303f;
}
pre {
  background: var(--code-bg);
  border: 1px solid var(--rule);
  border-left: 3px solid var(--accent);
  border-radius: 3px;
  padding: 3.6mm 4.5mm;
  margin: 0 0 4.2mm;
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-word;
}
pre code {
  background: none;
  border: 0;
  padding: 0;
  font-size: .875em;
  line-height: 1.55;
  color: #23303f;
}

/* ---- tables ---- */
table {
  width: 100%;
  border-collapse: collapse;
  margin: 0 0 4.6mm;
  font-size: 9.5pt;
}
thead th {
  background: var(--accent-soft);
  color: var(--accent);
  text-align: left;
  font-weight: 600;
  padding: 2.4mm 3mm;
  border: 1px solid var(--rule);
  border-bottom: 1.5px solid var(--accent);
}
tbody td {
  padding: 2.2mm 3mm;
  border: 1px solid var(--rule-soft);
  vertical-align: top;
}
tbody tr:nth-child(even) td { background: #fafbfc; }
td code, th code { background: none; border: 0; padding: 0; }

/* ---- callout ---- */
.callout {
  background: var(--accent-soft);
  border-left: 3px solid var(--accent);
  border-radius: 0 3px 3px 0;
  padding: 3.4mm 4.5mm;
  margin: 0 0 4.2mm;
  color: var(--ink-soft);
}

hr {
  border: 0;
  border-top: 1px solid var(--rule);
  margin: 8mm 0;
}

/* ---- print ---- */
@page {
  size: A4;
  margin: 17mm 16mm 16mm;
}
@media print {
  body { background: #fff; font-size: 10pt; }
  .sheet {
    max-width: none;
    margin: 0;
    padding: 0;
    box-shadow: none;
  }
  h2, h3, h4 { break-after: avoid; page-break-after: avoid; }
  table, pre, .callout { break-inside: avoid; page-break-inside: avoid; }
  table { font-size: 9pt; }
  thead { display: table-header-group; }
  tr { break-inside: avoid; page-break-inside: avoid; }
  a { color: var(--ink); text-decoration: none; }
  .cover { break-after: page; page-break-after: always; }
  .toc-block { break-after: page; page-break-after: always; }
  .page-break { break-before: page; page-break-before: always; }
}
"""

TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title} \u2014 {subtitle}</title>
<style>{css}</style>
</head>
<body>
<div class="sheet">

  <header class="cover">
    <div class="mark">NEXUS</div>
    <h1>{title}</h1>
    <p class="subtitle">{subtitle}</p>
    <dl>
      <dt>Version</dt><dd>1.0</dd>
      <dt>Date</dt><dd>October 2026</dd>
      <dt>Platform</dt><dd>ASP.NET Core 8 MVC &middot; SQL Server</dd>
      <dt>Prepared for</dt><dd>Aptech eProject submission</dd>
    </dl>
  </header>

  <nav class="toc-block">
    <h2>Contents</h2>
    {toc}
  </nav>

  <main>
{body}
  </main>

</div>
</body>
</html>
"""


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.abspath(os.path.join(here, "..", ".."))
    src_dir = os.path.join(root, "deliverables", "guides")

    for g in GUIDES:
        path = os.path.join(src_dir, g["src"])
        with open(path, "r", encoding="utf-8") as fh:
            lines = fh.read().split("\n")

        # Drop the markdown's own H1/H2 title pair and its contents list - the
        # cover and the generated TOC replace them.
        body_lines = []
        skip = True
        for idx, line in enumerate(lines):
            if skip:
                if re.match(r"^##\s+Table of Contents", line):
                    continue
                if idx < 2:
                    continue
                if re.match(r"^##\s+Table of Contents", line):
                    continue
                skip = False
            body_lines.append(line)

        # strip the manual TOC block (from "## Table of Contents" to the next "## ")
        trimmed = []
        in_toc = False
        for line in body_lines:
            if re.match(r"^##\s+Table of Contents", line):
                in_toc = True
                continue
            if in_toc:
                if re.match(r"^#{2,4}\s", line):
                    in_toc = False
                else:
                    continue
            trimmed.append(line)

        blocks = parse(trimmed)
        body = "\n".join(content for _, content in blocks)
        # extra bottom padding so print margins stay clean
        rendered = TEMPLATE.format(
            title=html.escape(g["title"]),
            subtitle=html.escape(g["subtitle"]),
            css=CSS,
            toc=build_toc(blocks),
            body=body,
        )

        out_path = os.path.join(src_dir, g["out"])
        with open(out_path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(rendered)

        tables = sum(1 for t, _ in blocks if t == "table")
        codes = sum(1 for t, _ in blocks if t == "code")
        print("OK %-32s %6d bytes  blocks=%d tables=%d code=%d"
              % (g["out"], len(rendered), len(blocks), tables, codes))


if __name__ == "__main__":
    main()
