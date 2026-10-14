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
- **`validation.js` server ke `data-val-*` attributes parh kar chalti hai**, apne
  hardcoded regex se nahi. `regex` rule `data-val-regex-pattern` + `data-val-regex`
  use karta hai; `length` rule `data-val-length-min` + `data-val-length`.
  **Naya hardcoded rule mat add karo** — warna client aur server drift kar jate hain
  (13 Oct ko phone `^03\d{9}$` vs server `^0\d{3}-\d{7}$` — poora order wizard dead tha).
- **Dash-required fields par `inputmode="numeric"` mat lagao** — mobile par dash key
  hi nahi hoti. Phone/CNIC/Landline se hata diya.
- **Razor runtime compilation OFF hai** — `.cshtml` badalne par `dotnet build` lazmi.
  Sirf `wwwroot/` (JS/CSS) badalne par rebuild ki zaroorat nahi.

## Testing — TestSprite (is machine par setup hai)

- `testsprite test steps <test-id>` **passed runs ke liye bhi** chalta hai. **Sirf
  status par bharosa mat karo** — 13 Oct ko signup test "passed" tha magar wizard
  phone field par phansa raha tha aur DB mein kuch bana hi nahi. Steps parh kar pakda.
- **Pass hone ka asli saboot DB hai.** Har "passed" test ke baad relevant table ka
  count baseline se compare karo.
- `test artifact get <run-id>` **sirf failed/blocked** runs ke liye hai; passed run par
  "Resource not found" deta hai.
- **Har run se pehle `testsprite-cleanup.sql` chalao** — tests fixed naam use karte hain,
  aur duplicate guard unhe reject kar deta hai.

## Billing rules (14 Oct ko tay hue)

- **Advance cap hota hai, poora nahi lagta.** `applied = min(AdvanceRemaining,
  max(0, charges))`. Bill **kabhi negative nahi** banta; baqi advance order par
  `AdvanceRemaining` mein bacha rehta hai aur agli bill par lagta hai.
  `ConnectionOrder.AdvanceApplied` / `AdvanceRemaining` dekho.
- **Purana `AdvanceAdjustedOn` akela kaafi nahi** — nayi logic `AdvanceRemaining`
  dekhti hai. Jo rows purani logic se consume ho chuki thin unka
  `AdvanceApplied = QuotedAmount` backfill karna lazmi tha (migration
  `AdvanceAppliedBackfill`).
- **Discount base sirf advance + security deposit** (FR-99), poora subtotal nahi.
- **Discount row invoice par sirf tab dikhta hai jab `DiscountAmount > 0`** —
  single-connection customer par 0 hota hai, is liye row nahi aata. Ye sahi hai.
- **Tier `customer.LiveConnectionCount` se aata hai**, aur pehla slab **10** se
  shuru hota hai — UI se 10+ connections banana practical nahi.

## Staff accounts

- **Account id = role letter + 13 digits.** `A`=Admin, `F`=Accounts,
  `T`=Technical, `R`=Retail (customers `B` + 15 digits, 16 chars).
  `OrganisationService.NextStaffAccountIdAsync` table se max+1 leta hai.
- **Staff logins `/Admin/Organisation/Logins` se bante hain** (14 Oct ko add hua).
  Employee form ka dropdown sirf **unlinked** logins dikhata hai — pehle chaaron
  linked the, is liye dropdown khaali tha (TC-026).

## Connection banane ka flow (test/demo ke liye)

`AwaitingFeasibility` order se: `POST /Operations/RecordCheck` (DistanceFromExchangeKm,
ExchangeHasCapacity, LandlineVerified, Remarks) → `Confirm` (ScheduledFor) →
`StartInstallation` → `CompleteInstallation`. **Connection `Pending` banta hai** —
`POST /Operations/ChangeStatus` se `Active` karo, warna billing run usay skip kar deti
hai (`BillingService` line ~402 sirf `ConnectionStatus.Active` uthata hai).
Feasibility: 5 km se zyada door → fail.

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
- **⚠️ `Connection Timeout Expired ... [Post-Login] complete=NNNNN` = transient network
  timeout, GHALAT PASSWORD NAHI.** 12 Oct (`13079`) aur 13 Oct (`21744`) dono dafa yehi
  hua. Pehle SQL Server check karo (`SELECT COUNT(*) FROM Users`), phir
  `sys.dm_exec_sessions` se dekho ke connection leak hai ya nahi (app ka
  `program_name` `.NET SqlClient` hai; **0 rows = leak nahi**). **Password mat badlo,
  baar baar login try mat karo** — account lock ho jayega. Seedha HTTP client (Python)
  se test karo, wo saaf jawab deta hai.
- **`Connect Timeout` connection string mein set karo.** Default **15s** hai — SQL Server
  kabhi kabhi post-login mein 20s+ leta hai aur 500 de deta hai. `appsettings.json`:
  `...;Connect Timeout=60;Min Pool Size=5`. `Min Pool Size` is liye ke app ke pooled
  connections idle par 0 ho jate hain, phir har burst ka pehla request **naya login**
  karta hai — wahi slow hota hai.
- **`EnableRetryOnFailure` query *iterate* karte waqt aane wale error ko retry NAHI karta.**
  Is liye connection timeout phir bhi 500 deta hai.
- **Seed passwords — HAR ACCOUNT KA ALAG HAI. Ye 12 Oct ko offline verify kiya gaya:**
  | Account | Password |
  |---|---|
  | `admin@nexus.example` | `Nexus@2027` |
  | `retail@nexus.example` | **`Nexus@2026`** |
  | `technical@nexus.example` | **`Nexus@2026`** |
  | `accounts@nexus.example` | `Nexus@2027` |
  | `ayesha.siddiqui@example.com` | `Nexus@2026` (12 Oct ko reset) |
  | `bilal.khan@example.com` | `Nexus@2026` (12 Oct ko reset) |

  Seeder (`Data/Seed/NexusSeeder.cs:201,258`) `Nexus@2026` likhta hai, aur
  line 275 par **existing users ko skip** karta hai — password reset nahi karta.
  Is liye jinka password baad mein badla gaya (admin, accounts) un par naya
  chalta hai, baqi par seeder wala. **"Sab ka ek hi password hai" mat maano.**
  **Customer accounts seeder banata hi nahi** — wo registration (`/Orders/New`)
  se bante hain, is liye unka password kisi ko pata nahi hota. 12 Oct ko dono
  ko `Nexus@2026` par reset kiya gaya (purane hashes `2026-10-12.md` mein hain).
  Lockout unlock: `UPDATE Users SET AccessFailedCount=0, LockoutEnd=NULL WHERE Email='...'`.
- **Password hash offline verify karne ka tareeqa (lockout se bachne ke liye).**
  Hash `PasswordHash` column mein base64 hai, format:
  `0x01 | prf(4 BE) | iterations(4 BE) | saltLen(4 BE) | salt | subkey`.
  prf: `0`=SHA1, `1`=SHA256, `2`=SHA512. Phir `pbkdf2_hmac(algo, pw, salt, iters, len(subkey))`
  aur `compare_digest`. **Login try karne se behtar hai** — account lock nahi hota.
  App ka `PasswordHasher` **SHA512 / 100,000** likhta hai (prf=2). Nayi hash banane
  ke liye bhi yahi parameters use karo, phir **read-back kar ke dobara verify karo**.

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
