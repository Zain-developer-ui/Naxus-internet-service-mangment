# NEXUS Service Marketing System — Project Rules

## Project

Aptech eProject — **NEXUS Service Marketing System** (telecom + internet vendor).
ASP.NET Core 8 MVC + Razor + vanilla CSS/JS. Root namespace `NEXUS`.
Workspace: `C:\Users\Zain Ansari\source\repos\Zain-developer-ui\Naxus-internet-service-mangment`

Teammate ne frontend banaya hai ("frontend ready just some improvements").
Zain ko **backend + security** par kaam karna hai, aur frontend ko improve karna hai.

## Source of truth

Do SRS documents (legacy OLE2 `.doc`, readable text extract
`.workbuddy-ai/tmp/*.clean.txt` mein):
- `NEXUS SERVICE MARKETING SYSTEM.doc` — asli requirements
- `Project Specification - NEXUS SERVICE MARKETING SYSTEM.DOC` — Aptech ka process,
  deliverables, documentation standards

## Hard rules (Zain ki preferences)

- Code human-written lagna chahiye: **koi emoji nahi**, koi filler/narrating comment
  nahi, koi boilerplate docblock nahi. Comment sirf genuine non-obvious "why" par.
- **Fake statistics, fake testimonials, fake logos — nahi.** Existing homepage mein
  1245 customers / 42 cities / 4.8★ rating / fake testimonials sab hatane hain.
- Excessive gradients, glassmorphism, neon, flashy animation — nahi.
- Default Bootstrap / student-project look — nahi. Portfolio-grade chahiye.
- **Navbar mein search bar — nahi.**
- Data loss ka darr — actual counts se tasalli do, alfaaz se nahi.

## Frontend ka state (5 Oct 2026 audit)

- `wwwroot/js/validation.js` **missing** lekin `_Layout` load karta hai (404).
- `NEXUS.getUser()` / `NEXUS.saveUser()` **kahin define nahi** — Login crash karega.
- `home.css` mein **0 design tokens**, 99 hardcoded hex. Load order bhi galat
  (`home.css` doosri files se pehle, halanke baad mein aana chahiye).
- Dup CSS loads (Layout + per-page), 49 inline styles Reports mein.
- `csproj` mein EF Core SqlServer refs hain lekin header comments "frontend only"
  claim karte hain — ye comment galat hai.
- Login ANY password accept karta hai, koi authorization nahi.

## Gotchas — ye dobara milenge

- **Enums DB mein strings hain.** Poore project mein `HasConversion<string>()`
  hai (ConnectionType, ConnectionStatus, OrderStatus, BillStatus, PaymentMethod,
  BillingCycle, FeasibilityResult). Yani `[Status]` column `nvarchar` hai aur
  usme `'Active'` jaisa string jata hai. **Raw SQL se test data seed karte waqt
  `Status='Active'` likho, `Status=1` nahi** — warna row ban to jayegi magar EF
  usay kabhi match nahi karega (SQL `N'Active'` bhejta hai).
- **`EnableRetryOnFailure` + manual transaction = 500.** EF khud ke banaye
  transaction ko replay nahi kar sakti. Har transaction wale write path ko
  `_db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { ... })`
  ke andar wrap karo.
- **`Connections.OrderId` par unique index hai** — ek order = ek connection.
  Zyada connections chahiye to alag ConnectionOrders banao.
- **Registration ab `/Orders/New` par hai**, `/Account/Register` sirf 302 karta
  hai. Alag register page delete ho gaya.
- **Optional text field ke liye `IsNullOrWhiteSpace` use karo, `is null` nahi.**
  Browser khaali input ko `""` bhejta hai, null nahi.
- **`sqlcmd` path:** `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd`
  (x86 wala 160 path is machine par maujood nahi hai).
- **Filter token ke liye `DisplayName()` use mat karo.** `ConnectionType.DialUp`
  ka `DisplayName()` `"Dial-Up"` hai (hyphen), enum member name `"DialUp"` hai.
  Jab JS do attributes match karta hai (jaise plan ka `data-type` aur type
  radio ka `value`), dono taraf **enum member name** bhejo. Warna `DialUp`
  kabhi `Dial-Up` se match nahi karega. Sirf screen par dikhane ke liye
  `DisplayName()` — token kabhi nahi.
- **`playwright-cli` is machine par setup hai** (plugin cache folder mein
  `npm install` chal chuka). Use: `node playwright-cli.js -s=<name> open <url>`
  phir `eval "<fn>"`. **Har call alag process hai** — `open` aur `eval` ek hi
  command mein `&&` se chain karo, warna page `about:blank` reh jata hai.
- **JS sirf `form` ke andar mat dhoondo.** `/Orders/New` ka summary panel
  `<aside>` hai jo form ke **bahar** hai. `form.querySelectorAll` se summary
  elements milte hi nahi — chup-chaap kuch update nahi hota. Summary ke liye
  `document` par search karo.
- **Har input form ke andar hona chahiye.** `BillingCycle` radios galti se
  `<form>` ke bahar the — yani wo POST hi nahi hote the aur har order default
  par chala jata. Agar koi input visually aside mein rakho to `form="<id>"`
  attribute lazmi hai, warna move kar do.

## SRS features jo abhi bhi nahi hain

Vendor mgmt, Stock/inventory, Retail shop mgmt, Employee management,
Advanced search, Reports.

(DONE: registration + order, billing engine + payments + invoices,
bulk discount, service tax, **feasibility workflow + connection lifecycle**.)

## Layout, navbar aur logout

- **Sirf do layout hain**: `_Layout` (public) aur `_DashboardLayout`
  (dashboards, `BodyClass = dashboard-body`). Dashboard views khud
  `Layout = "_DashboardLayout"` set karte hain.
- `_Sidebar.cshtml` role `ViewData["UserRole"]` se uthata hai; default
  `"Customer"` hai — dashboard view ko ye set karna **zaroori** hai warna
  galat sidebar aayega.
- **Logout sirf POST hai** (`AccountController.Logout`, `[ValidateAntiForgeryToken]`).
  Kabhi `<a href="/Account/Login">` se logout mat banao — us se redirect loop
  banta hai aur user logged-in reh jata hai. Hamesha form + token.
- `_Navbar.cshtml` auth-aware hai: signed-in → "My Dashboard" + Logout;
  guest → Login + New Connection.

## Password hashing ka gotcha

.NET 8 `PasswordHasher` **210,000** iterations (100,000 nahi). Hash ka doosra
byte-group: `AQAAAAEAAYag` = 100k, `AQAAAAIAAYag` = 210k. Seed user
`admin@nexus.example` / `Nexus@2026` hai laken `MustChangePassword=1` hai —
aur `ChangePassword` POST abhi **disabled** hai ("not enabled yet"), to pehla
login change-password page pe atak jayega. DB se reset karke ya
`MustChangePassword=0` karke hi aage ja sakte ho.

## SQL / CLI notes (is machine par)

- `sqlcmd`: `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd`
- Server `DESKTOP-BD6OLPC\SQLEXPRESS`, `sa`/`aptech`, DB `NexusDb`.
- Tables: `Users` (custom, `Role` column — Identity tables nahi), `Roles` nahi.
  `ConnectionOrders` mein `FeasibilityStatus`/`TotalAmount` columns **nahi** hain;
  `FeasibilityChecks` mein column `Result` hai (`FeasibilityResult` nahi).
- SQL Express connection pool bhaari load pe `Post-Login complete=14158` timeout
  de sakta hai. Thora ruk kar retry karo, query galat nahi hai.


## Design tokens (site.css)

`--nexus-navy: #0A1F44`, `--nexus-blue: #1E6FE5`, `--nexus-accent: #38BDF8`,
`--nexus-background: #F5F8FC`, `--nexus-text: #0A1F44`, `--nexus-muted: #64748B`,
`--nexus-border: #E2E8F0`, `--nexus-success: #10B981`, `--nexus-warning: #F59E0B`,
`--nexus-danger: #EF4444`. Naye CSS mein inhe use karo, hardcoded hex nahi.

## Workflow notes

- Build `dotnet build` sandbox mein block hota hai. Zain ne kaha build par waqt
  zaya na karo — design/code padh kar audit karo.
- Zain `cmd.exe` use karta hai, Git Bash nahi. Shell commands cmd syntax mein do.
- Roman Urdu mein jawab do. Chhote, seedhe. Sawaal ka jawab do, process narration
  mat karo.
- Progress `TASK-LEDGER.md` mein track karni hai (root mein, abhi banani hai).
