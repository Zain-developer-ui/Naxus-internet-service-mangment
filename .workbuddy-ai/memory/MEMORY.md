# NEXUS Service Marketing System — Project Rules

## Project

Aptech eProject — **NEXUS Service Marketing System** (telecom + internet vendor).
ASP.NET Core 8 MVC + Razor + vanilla CSS/JS. Root namespace `NEXUS`.
Workspace: `C:\Users\Zain Ansari\source\repos\Zain-developer-ui\Naxus-internet-service-mangment`

Teammate ne frontend banaya. Zain ka kaam: **backend + security** + frontend improve.

**Status (12 Oct 2026): SRS 100% done.** Baqi sirf Aptech deliverables (6.1–6.7),
jo Zain ne alag rakhe hain.

## Source of truth

Do SRS documents (legacy OLE2 `.doc`, text extract `.workbuddy-ai/tmp/*.clean.txt`):
- `NEXUS SERVICE MARKETING SYSTEM.doc` — asli requirements
- `Project Specification - NEXUS SERVICE MARKETING SYSTEM.DOC` — Aptech process

Planning docs (root): `PLAN-4-DAYS.md`, `TASK-LEDGER.md`, `TESTING-GUIDE.md`,
`TESTSPRITE-PLAN.md`, `STATIC-DATA-AUDIT.md`.

## Hard rules (Zain ki preferences)

- Code human-written lagna chahiye: **koi emoji nahi**, filler/narrating comment
  nahi, boilerplate docblock nahi. Comment sirf genuine non-obvious "why" par.
- **Fake statistics, testimonials, logos — nahi.**
- Excessive gradients, glassmorphism, neon, flashy animation — nahi.
- Default Bootstrap / student-project look — nahi. Portfolio-grade chahiye.
- **Navbar mein search bar — nahi.**
- Data loss ka darr — actual DB counts se tasalli do, alfaaz se nahi.
- Roman Urdu mein jawab. Chhote, seedhe. Sawaal ka jawab do, process narration nahi.
- Zain `cmd.exe` use karta hai, Git Bash nahi. Shell commands cmd syntax mein do.

## Gotchas — C# / EF Core

- **Date range: `To` ko poora din cover karo.** `x < To.Date.AddDays(1)`, `x <= To` nahi.
- **`Employee` class `Domain/Organisation/RetailShop.cs` mein hai** (alag file nahi),
  field `FullName` hai `Name` nahi.
- **`SearchOrderRow`** ke fields `ServiceTypeName` / `PlanName` / `StatusLabel` hain.
- **`User.EmployeeId()` `int?` deta hai.** Service ko bhi `int?` lena chahiye, warna `CS1503`.
- **GET + POST same action name = `CS0111`.** C# mein attributes signature ka hissa
  nahi. POST ka method naam alag rakho + `[ActionName("...")]`.
- **`[ValidateAntiForgeryToken]` + `IFormCollection` param = 400.** Seedha `Request.Form` padho.
- **Razor partial mein `decimal / decimal` implicit double nahi** — `(double)(a / b)`.
- **Enums DB mein strings hain** (`HasConversion<string>()` poore project mein).
  Raw SQL seed karte waqt `Status='Active'` likho, `Status=1` nahi — warna EF
  usay kabhi match nahi karega.
- **`EnableRetryOnFailure` + manual transaction = 500.** Wrap karo:
  `_db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { ... })`.
- **`Connections.OrderId` par unique index hai** — ek order = ek connection.
- **Registration `/Orders/New` par hai**; `/Account/Register` sirf 302 karta hai.
- **Optional text field ke liye `IsNullOrWhiteSpace`**, `is null` nahi — browser `""` bhejta hai.
- **Filter token ke liye `DisplayName()` use mat karo.** `ConnectionType.DialUp` ka
  `DisplayName()` `"Dial-Up"` hai (hyphen). JS matching ke liye **enum member name**
  bhejo dono taraf. `DisplayName()` sirf screen par.
- **Static member Razor mein instance se access nahi hota** (`CS0176`) — poora type likho.
- **Plan tariff sirf metered lines par.** `PlanAdminService.NormaliseTariff` broadband
  par null karta hai; `plan-edit.js` field chhupata hai. Dono taraf guard rakho.
- **`RateLimitPartition.GetFixedWindowLimiter` ka param `partitionKey` hai** (camelCase).
- **`Result<T>.Value` nullable hai.** `result.Value ?? Array.Empty<T>()` se CS8600/CS8604 chupte hain.

## Gotchas — CSP / TagHelper / Layout

- **`_DashboardLayout` ka script section `DashboardScripts` hai, `Scripts` nahi.**
  `@section Scripts` likhne se page **500**. Styles ke liye `DashboardStyles`.
  Public `_Layout` mein `Scripts` hai.
- **Sirf do layout hain**: `_Layout` (public) aur `_DashboardLayout`.
- **TagHelper ko `[HtmlTargetElement("script")]` rakho**, `Attributes = "~nonce"` ke saath
  nahi. Nonce sirf un tags par lagao jin par `src` **nahi**.
- **Inline `<script>` par nonce auto lagta hai** — `ScriptNonceTagHelper`
  `Views/_ViewImports.cshtml` mein `@addTagHelper *, NEXUS.Services` se register hota hai.
  Naya `_ViewImports` banao to ye line bhool mat jao.
- **Logout sirf POST hai.** `<a href>` se logout mat banao — redirect loop banta hai.
- `_Sidebar.cshtml` role `User.SidebarRole()` se uthata hai, `ViewData["UserRole"]` se nahi.

## Gotchas — JS / DOM

- **JS sirf `form` ke andar mat dhoondo.** `/Orders/New` ka summary `<aside>` form ke
  **bahar** hai. Summary ke liye `document` par search karo.
- **Har input form ke andar hona chahiye.** Warna wo POST hi nahi hota. Visually aside
  mein rakho to `form="<id>"` lazmi hai.
- **Font Awesome icons par `aria-hidden` central pass se lagta hai** —
  `site.js` → `initDecorativeIcons()`. 841 `<i>` tags hain, haath se mat likho.
- **`aria-current="page"` sidebar par JS se lagta hai** — `navigation.js` → `initAriaCurrent()`.
  Server-side sirf `class="active"` aata hai.

## Gotchas — playwright-cli (is machine par setup hai)

Plugin cache folder mein: `.../playwright-cli/0.1.0/playwright-cli.js`.

- **`page.click(sel, { noWaitAfter: true })` POST ABORT kar deta hai.** Server log mein
  `Operation cancelled by user` aata hai, aur lagta hai "login fail ho raha hai" jabke
  sign-in ho chuka tha. Sahi:
  ```js
  await Promise.all([
      page.waitForNavigation({ timeout: 90000 }).catch(() => {}),
      page.click('button[type="submit"]')
  ]);
  ```
- **Browser `user-data-dir: <in-memory>` hai — cookies invocations ke darmiyan nahi rehti.**
  Login aur audit **ek hi `run-code` call** mein karo.
- **`run-code` ko async arrow function do**, bare statements nahi (`SyntaxError: Unexpected token 'const'`).
  Return value `### Result` ke neeche aata hai; `console.log` capture **nahi** hota.
- `open` ke baad `eval` alag process mein `about:blank` deta hai — `goto` + `eval` chain karo.
- `fullPage: true` screenshot mein `.reveal` sections **invisible** aate hain
  (IntersectionObserver). Pehle scroll karo.
- Login page ke fields: `#AccountIdOrEmail`, `#Password`. `button[type="submit"]`.
  Uske pehle ek `button[type="button"].toggle` hai — `button` se match mat karo.

## SQL / DB (is machine par)

- `sqlcmd`: `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd`
- Server `DESKTOP-BD6OLPC\SQLEXPRESS`, `sa`/`aptech`, DB `NexusDb`.
  Handshake ~1s leta hai — server doosri machine par lagta hai.
- Tables: `Users` (custom, `Role` column — Identity tables nahi), `Roles` nahi.
  `ConnectionOrders` mein `FeasibilityStatus`/`TotalAmount` columns **nahi**;
  `FeasibilityChecks` mein column `Result` hai (`FeasibilityResult` nahi).
- **Baseline counts:** Users 6, Plans 10, Customers 2, Orders 2, Connections 1.
- **⚠️ `Connection Timeout Expired ... [Post-Login] complete=13079` = transient network
  timeout, GHALAT PASSWORD NAHI.** 12 Oct ko yehi hua. Pehle SQL Server check karo
  (`SELECT COUNT(*) FROM Users`), phir `sys.dm_exec_sessions` se dekho ke connection
  leak hai ya nahi. **Password mat badlo, baar baar login try mat karo** — account lock
  ho jayega. Seedha HTTP client (Python) se test karo, wo saaf jawab deta hai.
- **Seed passwords:** sab staff accounts (`admin@nexus.example`,
  `retail@nexus.example`, `technical@nexus.example`, `accounts@nexus.example`) ka
  password **`Nexus@2027`** hai. Seeder mein `Nexus@2026` likha hai — wo **purani**
  value hai, DB mein nahi chalti.
  Lockout unlock: `UPDATE Users SET AccessFailedCount=0, LockoutEnd=NULL WHERE Email='...'`.
- **Password hashing:** .NET 8 `PasswordHasher` **210,000** iterations.
  Hash ka doosra byte-group: `AQAAAAEAAYag` = 100k, `AQAAAAIAAYag` = 210k.

## Security

- `Services/Security/SecurityHeadersMiddleware.cs` — 6 headers + CSP.
- CSP `script-src` per-request **nonce** par; `Items["csp-nonce"]` pipeline ke shuru mein.
- `style-src` mein `'unsafe-inline'` **abhi hai** (purane inline `style=""` ki wajah se) —
  jaana-boojha gap.
- Login rate limit: per-IP 10/min fixed window, queue 0.
- **Rate limit + lockout TestSprite/load testing todenge** — dozens of cloud IPs ek saath
  login karenge. Test se pehle limit barhao, dedicated account banao.

## Design tokens (site.css)

`--nexus-navy: #0A1F44`, `--nexus-blue: #1E6FE5`, `--nexus-accent: #38BDF8`,
`--nexus-background: #F5F8FC`, `--nexus-text: #0A1F44`, `--nexus-muted: #64748B`,
`--nexus-border: #E2E8F0`, `--nexus-success: #10B981`, `--nexus-warning: #F59E0B`,
`--nexus-danger: #EF4444`. Naye CSS mein inhe use karo, hardcoded hex nahi.

## Workflow

- 24 user-level skills installed hain (`~/.workbuddy-ai/skills/`). UI/design se pehle
  `impeccable-design/craft-floor.md` + `redesign-existing-projects`; security se pehle
  `security-review`; kaam ke baad `verification-loop`.
- `dotnet build` ab sandbox mein **chalta hai** (~2-3 min). Background mein chalao.
- Server: `dotnet run --no-build --urls http://0.0.0.0:5199`.
  Band karo: `taskkill //F //IM NEXUS.Services.exe` (migrations se pehle zaroori).
- App logs: `Logs/nexus-YYYYMMDD.log` (Serilog).
- `dotnet ef --no-build` stale DLL use karta hai — pehle `dotnet build`.
