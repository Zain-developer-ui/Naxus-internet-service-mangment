"""
Renders the guide HTML files to PDF using Edge in headless mode.

The output is staged inside the project's own .workbuddy-ai/tmp folder rather
than a system temp directory: the sandbox permits writes under the workspace,
and Edge's headless writer is silent about a path it cannot write to.

Run:  python .workbuddy-ai/tmp/html-to-pdf.py
"""

import os
import shutil
import subprocess
import sys

EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "deliverables", "guides")
STAGE = os.path.join(ROOT, ".workbuddy-ai", "tmp", "pdf-stage")
PROFILE = os.path.join(ROOT, ".workbuddy-ai", "tmp", "pdf-profile")

GUIDES = [
    ("USER-MANUAL.html", "NEXUS-User-Manual.pdf"),
    ("DEVELOPER-GUIDE.html", "NEXUS-Developer-Guide.pdf"),
    ("INSTALLATION-GUIDE.html", "NEXUS-Installation-Guide.pdf"),
]


def render(html_name, pdf_name):
    src_html = os.path.join(SRC, html_name)
    if not os.path.exists(src_html):
        return f"missing {html_name}"

    staged_html = os.path.join(STAGE, html_name)
    staged_pdf = os.path.join(STAGE, pdf_name)
    shutil.copyfile(src_html, staged_html)

    if os.path.exists(staged_pdf):
        os.remove(staged_pdf)

    subprocess.run(
        [EDGE,
         "--headless=old",
         "--disable-gpu",
         "--no-sandbox",
         "--no-pdf-header-footer",
         f"--user-data-dir={PROFILE}",
         f"--print-to-pdf={staged_pdf}",
         "file:///" + staged_html.replace("\\", "/")],
        capture_output=True, text=True, timeout=240)

    if not os.path.exists(staged_pdf):
        return f"FAILED {pdf_name}"

    final = os.path.join(SRC, pdf_name)
    shutil.copyfile(staged_pdf, final)
    return f"OK {pdf_name}  {os.path.getsize(final) / 1024:.0f} KB"


def main():
    os.makedirs(STAGE, exist_ok=True)
    os.makedirs(PROFILE, exist_ok=True)

    for html_name, pdf_name in GUIDES:
        print(render(html_name, pdf_name))


if __name__ == "__main__":
    sys.exit(main())
