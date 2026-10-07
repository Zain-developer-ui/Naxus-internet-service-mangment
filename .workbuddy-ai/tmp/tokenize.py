import re, io

MAP = [
    # order matters: longer/more specific first
    ("#0A1F44", "var(--nexus-navy)"),
    ("#061631", "var(--nexus-navy-dark)"),
    ("#0F2A5C", "var(--nexus-navy-mid)"),
    ("#1E6FE5", "var(--nexus-blue)"),
    ("#1558B8", "var(--nexus-blue-dark)"),
    ("#E8F1FD", "var(--nexus-blue-light)"),
    ("#38BDF8", "var(--nexus-accent)"),
    ("#BAE6FD", "var(--nexus-accent-soft)"),
    ("#F5F8FC", "var(--nexus-background)"),
    ("#FAFBFD", "var(--nexus-surface-2)"),
    ("#F1F5F9", "var(--nexus-surface-3)"),
    ("#334155", "var(--nexus-text-soft)"),
    ("#64748B", "var(--nexus-muted)"),
    ("#94A3B8", "var(--nexus-muted-light)"),
    ("#E2E8F0", "var(--nexus-border)"),
    ("#EEF2F7", "var(--nexus-border-soft)"),
    ("#CBD5E1", "var(--nexus-border-strong)"),
    ("#10B981", "var(--nexus-success)"),
    ("#059669", "var(--nexus-success-dark)"),
    ("#D1FAE5", "var(--nexus-success-bg)"),
    ("#F59E0B", "var(--nexus-warning)"),
    ("#D97706", "var(--nexus-warning-dark)"),
    ("#FEF3C7", "var(--nexus-warning-bg)"),
    ("#EF4444", "var(--nexus-danger)"),
    ("#DC2626", "var(--nexus-danger-dark)"),
    ("#FEE2E2", "var(--nexus-danger-bg)"),
    ("#0284C7", "var(--nexus-info)"),
    ("#E0F2FE", "var(--nexus-info-bg)"),
    ("#E7EFF9", "var(--nexus-blue-light)"),
    ("#FFFFFF", "var(--nexus-surface)"),
]

files = ["wwwroot/css/home.css", "wwwroot/css/login-stars.css"]

for path in files:
    src = io.open(path, encoding="utf-8").read()
    before = len(re.findall(r"#[0-9A-Fa-f]{6}", src))
    for hexv, tok in MAP:
        src = re.sub(hexv, tok, src, flags=re.IGNORECASE)
    after = re.findall(r"#[0-9A-Fa-f]{6}", src)
    io.open(path, "w", encoding="utf-8", newline="").write(src)
    print(f"{path}: {before} hex -> {len(after)} left")
    if after:
        from collections import Counter
        print("   leftover:", Counter(a.upper() for a in after).most_common())
