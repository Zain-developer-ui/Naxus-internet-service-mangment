/*
  Renders the markdown guides to print-ready HTML, then hands each to Edge in
  headless mode for PDF conversion.

  The styling is deliberately plain: a serif body for long-form reading, a
  sans-serif head for navigation, and page rules that keep table rows and code
  blocks from splitting across a page break.

  Run:  node .workbuddy-ai/tmp/build-pdfs.js
*/

const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');

const ROOT = path.join(__dirname, '..', '..');
const SRC = path.join(ROOT, 'deliverables', 'guides');
const OUT = path.join(ROOT, 'deliverables', 'guides');

const EDGE = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';

const GUIDES = [
    {
        md: 'USER-MANUAL.md',
        pdf: 'NEXUS-User-Manual.pdf',
        title: 'NEXUS Service Marketing System',
        subtitle: 'User Manual',
        accent: '#1e4d3f'
    },
    {
        md: 'DEVELOPER-GUIDE.md',
        pdf: 'NEXUS-Developer-Guide.pdf',
        title: 'NEXUS Service Marketing System',
        subtitle: 'Developer Guide',
        accent: '#2c3e70'
    },
    {
        md: 'INSTALLATION-GUIDE.md',
        pdf: 'NEXUS-Installation-Guide.pdf',
        title: 'NEXUS Service Marketing System',
        subtitle: 'Installation & Deployment Guide',
        accent: '#7a3b1e'
    }
];

/* ---------- minimal markdown renderer ---------- */

function escapeHtml(s) {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function inline(s) {
    return s
        .replace(/`([^`]+)`/g, (_, c) => `<code>${escapeHtml(c)}</code>`)
        .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
        .replace(/(^|[\s(])\*([^*\n]+)\*/g, '$1<em>$2</em>')
        .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');
}

function slug(text) {
    return text.toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .trim()
        .replace(/\s+/g, '-');
}

function renderTable(rows) {
    const cells = r => r.replace(/^\||\|$/g, '').split('|').map(c => c.trim());
    const head = cells(rows[0]);
    const body = rows.slice(2).map(cells);

    const out = ['<table>', '<thead><tr>'];
    for (const h of head) out.push(`<th>${inline(h)}</th>`);
    out.push('</tr></thead>', '<tbody>');
    for (const r of body) {
        out.push('<tr>');
        for (let i = 0; i < head.length; i++) {
            out.push(`<td>${inline(r[i] ?? '')}</td>`);
        }
        out.push('</tr>');
    }
    out.push('</tbody>', '</table>');
    return out.join('\n');
}

function markdown(src) {
    const lines = src.split(/\r?\n/);
    const out = [];
    let i = 0;

    while (i < lines.length) {
        const line = lines[i];

        // fenced code
        if (/^```/.test(line.trim())) {
            const lang = line.trim().slice(3).trim();
            const buf = [];
            i++;
            while (i < lines.length && !/^```/.test(lines[i].trim())) {
                buf.push(lines[i]);
                i++;
            }
            i++;
            out.push(`<pre${lang ? ` data-lang="${lang}"` : ''}><code>${
                escapeHtml(buf.join('\n'))}</code></pre>`);
            continue;
        }

        // table
        if (/^\|/.test(line) && /^\|[\s:|-]+\|$/.test((lines[i + 1] || '').trim())) {
            const rows = [];
            while (i < lines.length && /^\|/.test(lines[i])) {
                rows.push(lines[i].trim());
                i++;
            }
            out.push(renderTable(rows));
            continue;
        }

        // heading
        const h = /^(#{1,4})\s+(.*)$/.exec(line);
        if (h) {
            const level = h[1].length;
            const text = h[2].trim();
            const id = slug(text);
            out.push(`<h${level} id="${id}">${inline(text)}</h${level}>`);
            i++;
            continue;
        }

        // horizontal rule
        if (/^-{3,}$/.test(line.trim())) {
            out.push('<hr />');
            i++;
            continue;
        }

        // list (ordered / unordered), one level
        if (/^\s*([-*]|\d+\.)\s+/.test(line)) {
            const ordered = /^\s*\d+\./.test(line);
            const tag = ordered ? 'ol' : 'ul';
            const items = [];
            while (i < lines.length && /^\s*([-*]|\d+\.)\s+/.test(lines[i])) {
                items.push(lines[i].replace(/^\s*([-*]|\d+\.)\s+/, ''));
                i++;
            }
            out.push(`<${tag}>`);
            for (const it of items) out.push(`<li>${inline(it)}</li>`);
            out.push(`</${tag}>`);
            continue;
        }

        // blockquote / callout
        if (/^>\s?/.test(line)) {
            const buf = [];
            while (i < lines.length && /^>\s?/.test(lines[i])) {
                buf.push(lines[i].replace(/^>\s?/, ''));
                i++;
            }
            out.push(`<blockquote>${markdown(buf.join('\n'))}</blockquote>`);
            continue;
        }

        // blank
        if (!line.trim()) {
            i++;
            continue;
        }

        // paragraph
        const buf = [];
        while (i < lines.length && lines[i].trim() &&
               !/^(#{1,4}\s|```|\||>\s?|-{3,}$|\s*([-*]|\d+\.)\s)/.test(lines[i])) {
            buf.push(lines[i].trim());
            i++;
        }
        out.push(`<p>${inline(buf.join(' '))}</p>`);
    }

    return out.join('\n');
}

/* ---------- document shell ---------- */

const CSS = `
@page {
    size: A4;
    margin: 20mm 18mm 22mm 18mm;

    @bottom-center {
        content: counter(page);
        font-family: Georgia, serif;
        font-size: 9pt;
        color: #8a8a8a;
    }
}

* { box-sizing: border-box; }

html { font-size: 10.5pt; }

body {
    font-family: Georgia, "Times New Roman", serif;
    color: #1c1c1c;
    line-height: 1.55;
    margin: 0;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
}

h1, h2, h3, h4 {
    font-family: "Segoe UI", "Helvetica Neue", Arial, sans-serif;
    line-height: 1.25;
    page-break-after: avoid;
    break-after: avoid;
}

h1 {
    font-size: 20pt;
    margin: 0 0 6pt;
    letter-spacing: -0.2pt;
}

h2 {
    font-size: 14pt;
    margin: 22pt 0 8pt;
    padding-bottom: 4pt;
    border-bottom: 1.5pt solid var(--accent);
    color: var(--accent);
}

h3 { font-size: 11.5pt; margin: 16pt 0 6pt; }
h4 { font-size: 10.5pt; margin: 13pt 0 4pt; color: #444; }

p { margin: 0 0 8pt; }

a { color: var(--accent); text-decoration: none; }

hr {
    border: none;
    border-top: 0.5pt solid #d5d5d5;
    margin: 16pt 0;
}

/* ---- cover ---- */

.cover {
    page-break-after: always;
    break-after: page;
    height: 240mm;
    display: flex;
    flex-direction: column;
    justify-content: center;
    border-left: 6pt solid var(--accent);
    padding-left: 16pt;
}

.cover .mark {
    font-family: "Segoe UI", Arial, sans-serif;
    font-size: 44pt;
    font-weight: 700;
    color: var(--accent);
    letter-spacing: -2pt;
    line-height: 1;
    margin-bottom: 4pt;
}

.cover .rule {
    width: 60pt;
    height: 3pt;
    background: var(--accent);
    margin: 14pt 0 20pt;
}

.cover h1 {
    font-size: 26pt;
    margin: 0 0 8pt;
    max-width: 150mm;
}

.cover .sub {
    font-family: "Segoe UI", Arial, sans-serif;
    font-size: 15pt;
    color: #555;
    margin: 0 0 26pt;
}

.cover .meta {
    font-family: "Segoe UI", Arial, sans-serif;
    font-size: 9.5pt;
    color: #666;
    line-height: 1.9;
}

.cover .meta strong { color: #1c1c1c; }

/* ---- tables ---- */

table {
    width: 100%;
    border-collapse: collapse;
    margin: 8pt 0 14pt;
    font-family: "Segoe UI", Arial, sans-serif;
    font-size: 8.6pt;
    page-break-inside: avoid;
    break-inside: avoid;
}

thead { display: table-header-group; }

th {
    background: var(--accent);
    color: #fff;
    text-align: left;
    padding: 5pt 6pt;
    font-weight: 600;
    font-size: 8.4pt;
    letter-spacing: 0.2pt;
}

td {
    padding: 4.5pt 6pt;
    border-bottom: 0.5pt solid #e2e2e2;
    vertical-align: top;
}

tbody tr:nth-child(even) { background: #f7f7f5; }

/* ---- code ---- */

pre {
    background: #f5f5f2;
    border: 0.5pt solid #ddd;
    border-left: 2.5pt solid var(--accent);
    padding: 7pt 9pt;
    margin: 8pt 0 12pt;
    font-family: Consolas, "Courier New", monospace;
    font-size: 8pt;
    line-height: 1.45;
    white-space: pre-wrap;
    word-wrap: break-word;
    page-break-inside: avoid;
    break-inside: avoid;
}

pre code { background: none; padding: 0; border: none; }

code {
    font-family: Consolas, "Courier New", monospace;
    font-size: 8.6pt;
    background: #f0f0ed;
    padding: 0.5pt 2.5pt;
    border-radius: 2pt;
    color: #9c2a2a;
}

/* ---- blockquote as callout ---- */

blockquote {
    margin: 9pt 0 12pt;
    padding: 7pt 10pt;
    background: #f8f8f4;
    border-left: 2.5pt solid #c9a227;
    page-break-inside: avoid;
    break-inside: avoid;
}

blockquote p { margin: 0 0 5pt; font-size: 9.6pt; }
blockquote p:last-child { margin-bottom: 0; }
blockquote strong { color: #7a5c00; }

/* ---- lists ---- */

ul, ol { margin: 0 0 9pt; padding-left: 16pt; }
li { margin-bottom: 3pt; }

/* ---- contents ---- */

.toc {
    page-break-after: always;
    break-after: page;
}

.toc h2 { margin-top: 0; }

.toc ol { padding-left: 18pt; }
.toc li { margin-bottom: 4pt; font-family: "Segoe UI", Arial, sans-serif; font-size: 10pt; }
.toc a { color: #1c1c1c; }
`;

function docShell(guide, body, tocHtml) {
    return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<title>${escapeHtml(guide.title)} — ${escapeHtml(guide.subtitle)}</title>
<style>
:root { --accent: ${guide.accent}; }
${CSS}
</style>
</head>
<body>

<section class="cover">
    <div class="mark">N</div>
    <div class="rule"></div>
    <h1>${escapeHtml(guide.title)}</h1>
    <p class="sub">${escapeHtml(guide.subtitle)}</p>
    <div class="meta">
        <div><strong>Version</strong> &nbsp; 1.0</div>
        <div><strong>Date</strong> &nbsp; October 2026</div>
        <div><strong>Project</strong> &nbsp; Aptech eProject</div>
        <div><strong>Platform</strong> &nbsp; ASP.NET Core 8 MVC &middot; SQL Server</div>
    </div>
</section>

${tocHtml}

${body}

</body>
</html>`;
}

/* ---------- toc ---------- */

function buildToc(body) {
    const heads = [...body.matchAll(/<h2 id="([^"]+)">(.*?)<\/h2>/g)];
    if (heads.length === 0) return '';

    const items = heads.map(m =>
        `<li><a href="#${m[1]}">${m[2].replace(/<[^>]+>/g, '')}</a></li>`).join('\n');

    return `<section class="toc">
<h2>Contents</h2>
<ol>
${items}
</ol>
</section>`;
}

/* ---------- main ---------- */

function main() {
    for (const guide of GUIDES) {
        const mdPath = path.join(SRC, guide.md);
        if (!fs.existsSync(mdPath)) {
            console.error(`missing: ${mdPath}`);
            continue;
        }

        const md = fs.readFileSync(mdPath, 'utf8');

        // The title block at the top of each guide becomes the cover, so drop
        // everything before the first section heading.
        const start = md.indexOf('\n## ');
        const body = markdown(start > 0 ? md.slice(start) : md);

        const html = docShell(guide, body, buildToc(body));

        const htmlPath = path.join(SRC, guide.md.replace(/\.md$/, '.html'));
        fs.writeFileSync(htmlPath, html, 'utf8');

        const pdfPath = path.join(OUT, guide.pdf);
        const res = spawnSync(EDGE, [
            '--headless=new',
            '--disable-gpu',
            '--no-sandbox',
            '--no-pdf-header-footer',
            `--print-to-pdf=${pdfPath}`,
            'file:///' + htmlPath.replace(/\\/g, '/')
        ], { encoding: 'utf8', timeout: 180000 });

        if (res.error) {
            console.error(`${guide.pdf}: ${res.error.message}`);
            continue;
        }

        const ok = fs.existsSync(pdfPath);
        const size = ok ? (fs.statSync(pdfPath).size / 1024).toFixed(0) : 0;
        console.log(`${ok ? 'OK  ' : 'FAIL'} ${guide.pdf}  ${size} KB`);
    }
}

main();
