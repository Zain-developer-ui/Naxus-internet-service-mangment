# 10 — Changelog

Har change ka record. Newest sab se upar. Format: `type(scope): kya badla`.

Ye honest record hai — jo kiya woh likha hai, jo nahi kiya woh bhi.

Types: `fix` (bug), `feat` (naya), `refactor` (behaviour same, structure behtar),
`style` (visual only), `docs`, `chore` (build/config).

---

## [Unreleased] — Feasibility workflow + auth/nav fixes (Phase 5.3)

### feat

- **`feat(operations)`** — Phase 5.3 shuru: survey queue, order review, aur
  connection register. Naye `Services/Feasibility/FeasibilityService.cs`
  (`IFeasibilityService`, 11 methods), `Models/ViewModels/OperationsViewModels.cs`,
  `Controllers/OperationsController.cs`, aur 4 views (`Index`, `Review`,
  `Connections`, `Connection`). `Program.cs` mein `IFeasibilityService` register.
- **`feat(operations)`** — Faisla **rules karte hain, officer nahi**. Officer
  sirf distance + capacity + landline deta hai; `Evaluate()` natija nikaalta
  hai. Live verify: 7.5 km → `Failed` (reason service ne khud likhi);
  2.3 km + capacity → `Passed`. Landline rule (V1) sirf Dial-Up/Telephone pe
  lagta hai — Broadband pe field hidden rehti hai.
- **`feat(operations)`** — Connection lifecycle. `CompleteInstallationAsync`
  `CreateExecutionStrategy` + transaction ke andar connection number banata hai,
  `Connection` (status `Pending`) create karta hai, `ConnectionStatusHistory`
  likhta hai, aur order `Completed` karta hai.
- **`feat(operations)`** — `IsLegalMove()` state machine. `Pending` se
  Active/TempInactive/PermInactive; `Active` se TempInactive/PermInactive;
  `PermanentlyInactive` terminal hai. UI sirf legal moves dikhata hai —
  verified: Active hone ke baad "Pending" option ghayab.

### fix

- **`fix(auth)`** — Admin login ke baad navbar guest jaisa lagta tha ("Login" /
  "New Connection" dikh rahe the, dashboard ka koi link nahi). Wajah:
  `_Navbar.cshtml` **poora static** tha, koi `@if (User.Identity.IsAuthenticated)`
  branch hi nahi thi. Ab signed-in par "My Dashboard" (role ke hisab se) +
  Logout, guest par purane links.
- **`fix(auth)`** — `_Sidebar.cshtml` ke **5 logout links** `/Account/Login` pe
  bhej rahe the (logout POST hai, GET link nahi). Ab asli
  `asp-action="Logout"` POST form + antiforgery token. `sidebar.css` mein
  `.sidebar-logout button` ke liye rules extend kiye.
- **`fix(operations)`** — Connections register ke header cards har filter par
  **0** dikhate the kyunke counts filtered list se ban rahe the. Naya
  `ConnectionRegisterTotals` + `GetConnectionTotalsAsync()` (DB-level counts,
  filter ignore karta hai).
- **`fix(auth)`** — `ChangePassword` (POST) abhi bhi disabled hai
  ("not enabled yet") — ye jaan-boojh kar hai, laken is se admin ka pehla login
  loop mein phans jata hai. Filhal DB se `MustChangePassword=0` kar ke unblock
  kiya. Feature chalu karna **pending** hai.

### docs

- **`docs(memory)`** — `.NET 8 PasswordHasher` ka 210,000-iteration gotcha
  note kiya; navbar/logout layout rules; SQL column-name traps
  (`Users.Role`, `FeasibilityChecks.Result`).

---

## [Unreleased] — New Connection UI fixes (Phase 5.1c)

### fix

- **`fix(orders)`** — Dial-Up select karne par plan grid **khaali** thi. Do
  alag token ek hi cheez ke liye use ho rahe the: type radio `value="@type"`
  (enum member name → `DialUp`) aur plan card `data-type="@plan.ConnectionType"`
  (jahan `PlanOption` ne `DisplayName()` rakha tha → `"Dial-Up"`, hyphen ke
  saath). `DialUp` kabhi `Dial-Up` se match nahi karta. Broadband/Telephone
  sirf ittefaqan chal rahe the. `PlanOption.ConnectionType` ab enum member
  name rakhta hai; `DisplayName()` sirf screen par.
- **`fix(orders)`** — plan **auto-select**. `syncPlans()` ab pehla visible plan
  khud check karta hai, is liye form kabhi plan ke bina nahi rehta.
- **`fix(orders)`** — selected plan ka koi visual nahi tha. `.selected` class
  CSS mein thi magar JS usay kabhi lagata hi nahi tha (radio visually hidden
  hai, is liye `:checked` kaafi nahi). `paintPlanSelection()` add ki.
- **`fix(orders)`** — bogus `content: ' Mbps'` hata diya. Plan labels pehle se
  human text hain ("30 hours", "Unlimited"), unit append karne se galat
  lagta tha. `orders.css` aur `forms.css` dono se.
- **`fix(orders)`** — Telephone line ka badge `" hours"` render kar raha tha
  (null `HoursIncluded`). Ab connection type par fallback karta hai.
- **`fix(orders)`** — `AccountIdGenerator.NextOrderIdAsync` ka unused
  `serialLength` hata diya (CS0219).

### style

- **`style(orders)`** — selected plan card: blue border, gradient fill, aur
  corner par white Font Awesome tick badge. Focus ring `:has(input:focus-visible)`
  se, taake keyboard users ko bhi wahi affordance mile.
- **`style(orders)`** — `.choice-speed` 2rem → 1.4rem (labels chhote hain,
  2rem par bhaddi lag rahi thi).

### feat

- **`feat(orders)`** — navbar ka "Register" button ab **"New Connection"**
  hai (plug-circle-plus icon). Home aur Login ke entry points bhi
  "Get New Connection" ho gaye.

---

## [Unreleased] — Billing engine (Phase 5.2)

Product ki core value. Pehle koi billing logic nahi thi — bills sirf mock the.

### feat

- **`feat(billing)`** — `Common/Billing/BillCalculator.cs` — `ChargeLine`,
  `BillBreakdown`, `ChargeTypes`. Pure arithmetic, koi DB nahi, is liye
  testable. Order SRS se: subtotal → discount → **tax discounted amount par**
  → total. `ProrateMonthly`, `RentalLine`, `UsageLine`, `CallLine` bhi.
- **`feat(billing)`** — `Services/Billing/BillingService.cs` — 12 methods:
  draft preview, create, issue, cancel, monthly generation run, payment
  recording, ledger list, customer ledger, detail, payment history.
- **`feat(billing)`** — monthly billing run **idempotent** hai. Jo connection
  us period ke liye pehle se billed hai wo skip hota hai, aur skip ki wajah
  return hoti hai taake UI sach bata sake.
- **`feat(billing)`** — payment `AmountPaid` se status derive karta hai:
  partial → `PartiallyPaid`, poora → `Paid`. Overpayment reject.
- **`feat(billing)`** — `wwwroot/css/billing.css` — invoice document, filter
  bar, panel shell, payment form. Table chrome `tables.css` se aata hai.
- **`feat(billing)`** — `Views/Bills/Index.cshtml` + `Details.cshtml` asli
  models par, mock models chhor kar.

### fix

- **`fix(billing)`** — `BillsController` ab `IApiService` (mock) ki jagah
  `IBillingService` use karta hai. `/Bills` pehle hardcoded `NX12345678` aur
  `Ahmed Khan` dikha raha tha.
- **`fix(billing)`** — `BillDraft` mein `ConnectionId` add kiya. Pehle
  `CreateAsync` mein `c.CityId == c.CityId` jaisa bekaar predicate tha jo
  galat connection pakad sakta tha.
- **`fix(billing)`** — `BillSummary` mein `TaxableAmount` add kiya (view use
  kar raha tha, record mein nahi tha).

### Verified — 46 arithmetic + 15 lifecycle, plus HTTP

Arithmetic (asli `BillCalculator.cs` include kar ke):
`$400` minus 25% → taxable `$300` → tax `$36.72` (na ke `$48.96`) · tiers
9→0%, 10→25%, 15→50%, 25→75%, 50→100% · 100% discount → zero bill, negative
tax nahi · rounding `AwayFromZero` · yearly 12-month line · proration.

Lifecycle (asli DB):
period generate → 1 bill, dobara → 0 naya / 1 skip · `$175 + 12.24% = $196.42`
· 10 live connections → 25% off → `$147.32` · draft par payment reject ·
overpayment reject · cheque bina reference reject · partial → `PartiallyPaid`
→ baaki → `Paid` · settled par payment reject · paid bill cancel reject.

HTTP:
`/Bills` 200 dono invoices ke saath · `/Bills/Details/{id}` 200 tax line ke
saath · draft par payment form chhupa · **bina CSRF token POST → 400** ·
**IDOR (doosre customer ka bill) → 302 `/Account/AccessDenied`** · customer
apni `/Bills` par sirf apna ledger dekhta hai · HTTP payment → receipt
`RCP-202610-00003`, status `Paid`, toast.

Test data hata diya — `Users` 1, baaki sab 0. Build: **0 errors, 0 warnings**.

---

## [Unreleased] — Register + New Connection ek flow mein merge

User ka faisla: *"new connect bassically register hi ho"*. Pehle do parallel
flows the — `/Orders/New` order deyta tha magar account nahi banata tha,
`/Account/Register` account + order dono banata tha. Dono ka data bilkul ek
hi tha, is liye duplicate orders ka rasta khula tha. Ab ek hi flow hai.

### feat

- **`feat(orders)`** — `/Orders/New` ab connection apply karne ke saath saath
  account bhi bana deta hai, ek hi transaction mein. 4-step form: Service /
  Your Details / Installation / Account. Asli DB se 8 cities aur 10 plans,
  USD pricing.
- **`feat(orders)`** — `Models/ViewModels/FormOptions.cs` — `CityOption` aur
  `PlanOption` records, jo `RegisterViewModel.cs` delete hone se mar gaye the.
- **`feat(orders)`** — `Models/ViewModels/OrderTrackingViewModel.cs` — tracking
  page ke liye alag read-only model, taake `OrderViewModel` pure form model rahe.
- **`feat(orders)`** — `wwwroot/css/apply-form.css` — pehle ye styles
  `register.css` mein `.register-page` par scoped the; ab generalise kar diye
  (`.field-error`, `.hint`, `.type-picker`, `.cycle-picker`, `.plan-choice`,
  `.strength`, `.review-note`).
- **`feat(orders)`** — `wwwroot/js/new-connection.js` — 4-step navigation,
  connection type ke hisaab se plan filtering, live summary, password strength
  meter, terms gate.
- **`feat(db)`** — `ConnectionOrder.PreferredSlot` (`nvarchar(40)`), migration
  `20261005175810_Add_Order_PreferredSlot`. Customer ka maanga hua slot
  (e.g. "Morning (9am-12pm)") ab persist hota hai.

### fix

- **`fix(ui)`** — **har page load par "Ready to Get Connected" toast aata tha.**
  Wajah: `new-connection.js` mein `setTimeout(..., 800)` tha. Hata diya.
- **`fix(ui)`** — registration ka entry point nav mein ghalat jagah tha (login
  ke barabar "New Connection", home page par koi entry nahi). Ab navbar ka
  primary button "Register" hai → `/Orders/New`, home page par do account-entry
  cards, aur login page par "Create Account & Order".

### refactor

- **`refactor(registration)`** — `/Account/Register` ab permanent `302` hai
  `/Orders/New` par. `AccountController` se `IRegistrationService` dependency,
  `ApplyErrors`, `Repopulate`, `BuildForm` hata diye.
- **`refactor(registration)`** — `RegistrationService.RegisterAsync` ab
  `OrderViewModel` leta hai. `RegisteredCustomer` mein `OrderId` add kiya.
- **`chore`** — delete kiye: `Views/Account/Register.cshtml`,
  `Models/ViewModels/RegisterViewModel.cs`, `wwwroot/js/register.js`,
  `wwwroot/css/register.css`, `Views/Shared/_OrderNewScripts.cshtml`.

### Verified — POST end-to-end (curl + sqlcmd, dawe nahi)

| Test | Nateeja |
|---|---|
| `POST /Orders/New` (asli CityId + Broadband PlanId + password + terms) | `302` → `/Customer/Dashboard` |
| `NEXUS.Auth` cookie | set, HttpOnly, SameSite=Lax |
| `Customers` row | `B041000000000001` = Broadband + Faisalabad(041) + serial |
| `ConnectionOrders` row | `B0000000001`, Broadband, Monthly, `AwaitingFeasibility`, `500.00` |
| `PreferredSlot` / `ScheduledFor` / `Notes` | teeno persist hue |
| `Users` row | `B041000000000001`, `Customer`, `IsActive=1` |
| Dashboard | asli identity — `VT` initials, `Verification Tester`, full Account ID |
| `/Account/Register` (GET) | `302` → `/Orders/New` |

Teeno tables link the: `Users` → `Customers` → `ConnectionOrders`. Test rows
hata diye: **Users 1 (admin), Customers 0, ConnectionOrders 0.**

---

## [Unreleased] — Customer registration (Phase 5, pehla feature)

### feat

- **`feat(registration)`** — poora customer signup flow. `IRegistrationService`
  cities aur plans DB se laata hai, form validate karta hai, Account ID
  generate karta hai, `AppUser` + `Customer` + `ConnectionOrder` ek hi
  transaction mein likhta hai, aur customer ko seedha sign in kara deta hai.
- **`feat(registration)`** — `Views/Account/Register.cshtml` — 3-step form
  (aap ki details / connection / password), asli cities aur plans DB se.
- **`feat(registration)`** — `wwwroot/js/register.js` — landline field sirf
  Dial-Up/Telephone par dikhta hai, plan dropdown type ke hisaab se filter
  hota hai, live plan preview (monthly / yearly / deposit / total), aur
  password strength meter.
- **`feat(registration)`** — `wwwroot/css/register.css` — login page ka
  shell reuse karta hai, sirf grid aur card styles naye.

### fix

- **`fix(registration)`** — duplicate CNIC, duplicate phone, aur duplicate
  email ab reject hote hain. Pehle sirf DB unique index par bharosa tha,
  jo 500 deta.
- **`fix(registration)`** — **Rule V1** ab actually chal raha hai. Dial-Up
  aur Telephone ke liye landline lazmi hai, aur wo landline pehle se
  registered honi chahiye. `Normalise()` khaali string ko `""` return kar
  raha tha, is liye `is null` check kabhi true nahi hua — teen Dial-Up
  customers bina landline ke register ho gaye the. Ab `Optional()` null
  deta hai.
- **`fix(registration)`** — `PlanOption` record tooti hui syntax thi
  (positional record + property body mix). Ab saaf record hai.
- **`fix(data)`** — **`EnableRetryOnFailure` + manual transaction crash.**
  EF retry strategy khud ke banaye transaction ko replay nahi kar sakti,
  is liye registration 500 de raha tha. Ab poora transaction
  `CreateExecutionStrategy().ExecuteAsync()` ke andar hai. Ye bug har
  future transaction wale write path ko todta — ab pattern set hai.
- **`fix(ui)`** — `hidden` attribute wali field ka error message bhi hidden
  ho jata tha. Validation span ab hidden wrapper ke bahar hai.

### Verified (curl + DB, dawe nahi)

| Test | Nateeja |
|---|---|
| Registration happy path | `302` → `/Customer/Dashboard` |
| Account ID format | `B041000000000001` = Broadband + Faisalabad(041) + serial |
| Order auto-create | `B0000000001`, `AwaitingFeasibility`, deposit `500.00` |
| Duplicate CNIC | rejected, field message |
| Dial-Up bina landline | rejected, "Dial-Up requires an existing landline number." |
| Dial-Up ghair-registered landline | rejected |
| Broadband + Dial-Up plan | rejected, "…is a Dial-Up plan." |
| Galat CNIC / phone / password format | sab field-level rejected |
| Login ke baad dashboard | asli naam (`BA`), role pill `Customer`, asli Account ID |

Test data DB se hata diya — `Customers` 0, `ConnectionOrders` 0, `Users` 1
(admin). Admin `A0000000000001` salamat hai.

---

## [Unreleased] — Authentication & authorization

### feat

- **Phase 4.2 Authentication**
  - `Services/Authentication/AuthenticationService.cs` — credential check with
    a single generic failure message for every path, 5-attempt lockout for
    15 minutes, inactive/deleted account handling, last-login stamping.
  - `Services/Authentication/SignInService.cs` — cookie sign-in writing
    `UserId`, `AccountId`, `FullName`, `Role`, `EmployeeId`, `CustomerId`,
    `ShopId`, and a forced-password-change flag into claims. Cookie regenerated
    on every successful login.
  - `Services/Generation/AccountIdGenerator.cs` — Account IDs, order IDs, bill
    numbers, receipt numbers and connection numbers. Each reads the current
    maximum and retries on collision rather than trusting a counter, so two
    concurrent registrations cannot collide.
  - `Common/Constants/NexusClaims.cs` — claim type constants.
  - `Common/Extensions/ClaimsPrincipalExtensions.cs` — `UserId()`,
    `CustomerId()`, `EmployeeId()`, `RoleName()`, `Initials()` and friends, so
    claim strings never leak into view code.

- **Phase 4.3 Authorization**
  - `[Authorize(Roles = …)]` on every dashboard controller. Accounts area is
    Accounts+Admin, Technical is Technical+Admin, Retail is Retail+Admin,
    Customer is Customer only.
  - AJAX callers receive `401`/`403` status codes instead of a redirect to the
    login page; browser navigations still redirect.
  - `Views/Account/AccessDenied.cshtml` — branded 403 page.

- **Login / account screens**
  - `Views/Account/ChangePassword.cshtml` — forced password change after a
    seeded account signs in for the first time.
  - Login form posts a `returnUrl` and shows a validation summary.
  - `_DashboardTopbar.cshtml` now reads the signed-in user from claims and
    carries an account menu (profile, change password, sign out).

### fix

- `IPasswordHasher` → `IPasswordService` earlier, and now `IAuthenticationService`
  → `IUserAuthenticator`: ASP.NET Core ships its own types under both names and
  the duplicates made the references ambiguous (`CS0104`).
- `RedirectContext<>` fully qualified in `Program.cs` (`CS0246`).
- Login page no longer contains a sessionStorage stub writing a fabricated
  user, and the "enter any Account ID" demo hint is gone — it was not true.

### refactor

- `wwwroot/js/site.js` — removed `getUser` / `saveUser` / `clearUser`. The
  server cookie is now the only source of session truth. Added `initUserMenu()`
  (click, outside-click, Escape, focus return) and `NEXUS.timeAgo()`.
- `wwwroot/js/modal.js` → `validation.js` (earlier in this session).

### chore

- `appsettings.json` — `UseMockData: false`, `NexusFinance` section.
- `Program.cs` — cookie authentication registered, `UseAuthentication()`
  added ahead of `UseAuthorization()`, `Logs/` written by Serilog.
- `.gitignore` — `Logs/` excluded.

### verified (curl against the running app)

| Check | Result |
|---|---|
| Anonymous → 6 protected pages | all `302` to login with `ReturnUrl` |
| Wrong password | generic failure message |
| Non-existent account | identical generic message |
| Correct password | `302`, `NEXUS.Auth` cookie issued |
| 6th failed attempt | `Too many failed attempts. Try again in 15 minute(s).` |
| Admin cookie → customer dashboard | denied |
| Same as AJAX | `403`, no redirect |
| After logout | protected page redirects again |
| Local `returnUrl` | honoured |
| External `returnUrl` | rejected, falls back to role home |
| Build | **0 Errors** |

Demo sign-in: `admin@nexus.example` / `Nexus@2026`.

---



### feat

- **Phase 4.0 Foundation**
  - `Common/Result.cs` — `Result<T>` + `ErrorKind` (None/NotFound/Validation/
    Conflict/Unauthorized/Forbidden/Failure). Har service method isi ko return
    karegi; controller `Kind` dekh kar status code chune ga.
  - `Common/PagedResult.cs` — `PagedResult<T>` + `PageRequest` (default 20,
    max 100 per page).
  - `Common/Constants/TaxConstants.cs` — ServiceTaxRate `0.1224m`.
  - `Common/Constants/IdFormats.cs` — SRS ke ID shapes (order `D/T/B`+10 digits,
    account `1 letter + 3-digit city + 12-digit serial`) + validators.
  - `Common/Constants/BulkDiscountTiers.cs` — 25/50/75/100% slabs, `TierFor()`.
  - `Common/Constants/NexusRoles.cs` — 5 roles, string constants (Authorize
    attribute mein direct use hote hain).
  - `Common/Constants/NexusEnums.cs` — ConnectionType/Status, OrderStatus,
    FeasibilityResult, BillStatus, PaymentMethod, BillingCycle + DisplayName
    aur BadgeClass extensions.
  - `Common/Abstractions/IPasswordService.cs` — hashing contract.

- **Phase 4.1 Database (Code First)**
  - 22 entities: City, AppUser, Employee, RetailShop, Plan, PlanPrice,
    BulkDiscountTierEntity, EquipmentProduct, StockItem, StockMovement,
    Customer, ConnectionOrder, FeasibilityCheck, Connection,
    ConnectionStatusHistory, Bill, BillLine, Payment, Vendor, PurchaseOrder,
    PurchaseOrderLine, Feedback.
  - `Data/NexusDbContext.cs` — audit stamping (`SaveChanges` override),
    concurrency tokens, decimal precision.
  - `Data/Configurations/` — 4 files. Unique indexes (AccountId, Cnic, OrderId,
    BillNumber, ReceiptNumber, PoNumber, OutletCode, Product+Shop), search
    indexes, string enum conversions.
  - `Data/Seed/NexusSeeder.cs` — reference data: 8 cities, 10 plans (SRS ke
    asli USD rates), 29 plan prices, 4 bulk tiers, 6 products, 1 admin,
    1 retail shop. **Koi fake customer/bill/order nahi.**
  - `Services/Security/PasswordService.cs` — Identity PBKDF2 wrapper.
  - `Data/NexusDbContextFactory.cs` — design-time factory (bare context).

### fix

- **EF Core 9.0.20 → 8.0.11 downgrade.** Project `net8.0` target karta hai magar
  packages 9.x the — build to hota magar runtime par mismatch. Ab match hai.
- `IPasswordHasher` → `IPasswordService` rename (`CS0305` — Identity ke
  `IPasswordHasher<TUser>` se clash).
- `Result<T>.Cast<TOut>()` — galat overload call kar raha tha (`CS1501`).
- 22 entities mein cross-namespace `using` add kiye (`CS0246`).
- **Seeder order bug:** cities `AddRange` ke baad `SaveChanges` aakhir mein tha,
  is liye `SeedDemoOutletAsync` ko city DB mein nahi milti thi aur
  `RetailShops=0` reh jaati. Cities ke foran baad `SaveChangesAsync` daala.

### chore

- `appsettings.json` — connection string (`NexusDb`), `UseMockData: false`,
  `NexusFinance` section (ServiceTaxRate, CurrencyCode USD, symbol `$`),
  Serilog config.
- `Program.cs` — DbContext register (retry 3x, 30s timeout), Serilog
  (console + rolling file `Logs/nexus-.log`, 14 din retained), startup par seed.
- `dotnet-ef 8.0.11` local tool manifest (`.config/dotnet-tools.json`).

### verified

- `dotnet build` → **0 Errors, 10 Warnings** (sab `ServiceViewModel.cs` CS8618).
- `dotnet ef migrations add Initial_Foundation` → **0 warnings**.
- `dotnet ef database update` → applied clean.
- SQL se counts: `Tables=23, Cities=8, Plans=10, PlanPrices=29, BulkTiers=4,
  Products=6, Users=1, Employees=1, RetailShops=1, Customers=0`.
- Smoke test: `/`, `/Plans`, `/Services`, `/Account/Login` → **HTTP 200**.
- Serilog log → **0 errors**.

### Decision — soft delete

Global query filter **nahi** rakha. Wajah: "deleted" ka matlab har screen par
alag hai. Closed customer phir bhi Accounts ledger, outstanding-dues report aur
audit trail mein aana chahiye — model-level filter se wo rows har jagah se chhup
jaatin. `IsDeleted` ab sirf audit flag hai; filtering per-query services karengi.

---



### docs

- `docs/01-project-context.md` — naya. Project ka sach: stack, 13 controllers,
  "abhi ki haalat" (DB nahi, auth nahi, sab hardcoded).
- `docs/02-srs.md` — naya. Dono `.doc` files se poora SRS extract: 5 roles, ID
  formats, saare charges, 12.24% tax, bulk discount slabs, feasibility rules.
  Sath "Khule sawal" section: Yearly rental missing, Half-Yearly validity
  "1 month" likha hai, USD vs PKR, tax rate confirm karna hai.
- `docs/03-requirements-matrix.md` — naya. FR-01 se FR-132 traceability.
  Sach: **~64% MISSING, ~15% FAKE, ~30% PARTIAL, ~2% DONE.**
- `docs/04-frontend-audit.md` — naya. A: bugs (A1–A6), B: design (B1–B4),
  C: user preference violations (C1–C8), D: security (D1–D5), E: static views,
  F: strengths.
- `docs/05-frontend-decisions.md` — naya. FIX-1.1 se FIX-3.8, har ek ka reason.
- `docs/06-database-schema.md` — naya. 21 tables, relationships, indexes,
  concurrency, 6 migrations ka plan.
- `docs/07-api-contract.md` — naya. Option C chuna (`IApiService` server-side EF),
  `Result<T>` + `ErrorKind`, 3-layer validation, endpoint→role mapping.
- `docs/08-backend-progress.md` — naya. 9 phases (4.0 Foundation → 4.9
  Accessibility) + Phase 5 SRS features, checkbox tables ke saath.
- `docs/09-testing-checklist.md` — naya. 13 sections, 100+ checks, demo smoke test
  script, aur "known gaps" section.
- `docs/10-changelog.md` — ye file.
- `TASK-LEDGER.md` — naya. Root par, 6 phases, honest status.

### fix

- `js/validation.js` — **404 fix.** `_Layout.cshtml:51` `validation.js` load kar
  raha tha jo exist nahi karta tha. Asli validation code `modal.js` mein para tha
  (naam aur content match nahi karte the). File rename ki, header comment theek kiya.
- `js/site.js` — `NEXUS.getUser()` / `saveUser()` / `clearUser()` add kiye. Ye
  `Login.cshtml` call kar raha tha magar kahin defined nahi the → `TypeError`.
  Ab sessionStorage-backed hain, try/catch ke saath, fail par `null` return.
- `Views/Account/Login.cshtml` — crash-prone script block replace kiya. Hardcoded
  `Areeb` user hata, `getUser()` call par guard laga, narrating comments hataye.
- `Views/Shared/_Layout.cshtml` — **CSS order theek kiya.** `home.css` (0 tokens)
  `public-pages.css` / `orders.css` / `dashboards-luxury.css` (token-based) se
  *pehle* load ho raha tha, is liye cascade ulta tha. Naya order general → specific,
  `responsive.css` sab se aakhir.
- `Views/Feedback/Index.cshtml` — toota hua star rating theek kiya. Emoji cleanup
  script ne `new string('★', f.Rating)` se `★` hata diya tha → `CS1011: Empty
  character literal`. `@for` loop + Font Awesome + `.star-off` se replace kiya.

### style

- `css/home.css` — **150 hardcoded hex → 0.** Sab `var(--nexus-*)` tokens par.
- `css/login-stars.css` — hardcoded hex → tokens.
- `css/site.css` — 4 naye tokens: `--nexus-accent-bright: #7DD3FC`,
  `--nexus-info-ink: #075985`, `--nexus-background-alt: #F4F7FB`,
  `--nexus-text-invert: #FFFFFF`.
- `css/components.css` — `.avatar-initials`, `.avatar-lg`, `.avatar-xl` add kiye.
- `css/cards.css` — `.stars` ab `inline-flex` + `gap: 3px`, `.star-off { opacity: 0.25 }`.
- `css/orders.css` — `.technician-row`, `.technician-name`, `.technician-role`,
  `.technician-note`.
- `css/dashboards-luxury.css` — `.banner-avatar`.

### refactor

- **Fake data poora hata.** Har jagah se:
  - `data-count="1245"`, "42 Cities", "4.8", "1,245+ Customers" → neutral
    product facts (Dial-Up/Broadband/Landline, Feasibility Check, Postpaid Billing).
  - 3 fake testimonials (Bilal Khan / Sana Iqbal / Usman Ahmed) → service promises.
  - "4.8★ Average Rating" badges → hata.
  - "Trusted Provider" → "Certified Technicians".
  - "Join over 1,200 customers" → "Apply online or at any retail outlet".
  - Admin dashboard ke fake chart arrays khali, KPI values `—`.
- **55 emoji hataye** 6 dashboards se (Welcome messages, "Bills 💳", "My Connection 🔌", `👋`).
- `Views/Shared/_DashboardTopbar.cshtml` — **search bar hataya** (user preference),
  pravatar img → initials avatar.
- `Views/Orders/Tracking.cshtml` — pravatar technician img + inline styles →
  `avatar-initials` span + naye CSS classes.
- `Views/Profile/Index.cshtml` — pravatar `AvatarUrl` hata, existing `@initials`
  var use kiya.
- `Controllers/ProfileController.cs` — `AvatarUrl` line hatai.
- `Models/ViewModels/ProfileViewModel.cs` — `AvatarUrl` property hatai.
- `Views/Home/Index.cshtml` — duplicate `@section Styles` hata.
- `Views/Services/Index.cshtml`, `Views/Plans/Index.cshtml`, `Views/Orders/New.cshtml`
  — duplicate `@section Styles` hata.

### chore

- `wwwroot/images/` — **18 Unsplash images download kiye.** Phir 10 files ke
  andar 42 `images.unsplash` references ko `~/images/*.jpg` se replace kiya.
  Demo offline chalega, external CDN par depend nahi.
- 2 Unsplash photos 404 de rahi theen (`photo-1544428571-95f3fb8c4f2f`,
  `photo-1585141445799-4bcd9a18e35c`) — alternate photos se replace kiya.

### verified

- `dotnet build` → **Build succeeded. 10 Warning(s), 0 Error(s).**
  (10 warnings `ServiceViewModel.cs` ke CS8618 nullable warnings hain —
  backend phase mein fix honge.)
- `grep images.unsplash|pravatar` → **0 matches.**
- `grep #RRGGBB` in `home.css` / `login-stars.css` → **0 matches.**
- Saare `~/images/*` refs → **0 missing files.**

---

## Nahi hua (honest list)

Ye sab **abhi tak nahi hua**. Task ledger mein bhi pending hai.

- **910 inline styles** views mein baaki hain. `Reports.cshtml` mein 49,
  `Status.cshtml` mein 150 hex colors. Phase 2 backlog.
- **`!important` cleanup** — `public-pages.css` 9, `site.css` 6, `orders.css` 4,
  `forms.css` 3.
- **CDN dependencies** — Font Awesome, Chart.js, Google Fonts abhi bhi external.
- **Database** — bilkul nahi. Koi table, koi migration, koi seed nahi.
- **Authentication / Authorization** — nahi. Login page sirf UI hai.
- **Business logic** — tax, discount, feasibility, billing — kuch nahi.
- **API layer** — nahi. Controllers hardcoded view models return kar rahe hain.
- **Validation backend** — nahi. Sirf client-side helper hai.
- **Search** — nahi (SRS mein chahiye).
- **Currency decision** — pending. SRS USD kehta hai, frontend PKR use kar raha
  hai. Ye pehla blocker hai — iske bagair pricing ka kaam start nahi ho sakta.

---

## Blocker (sab se pehle chahiye)

**Currency: USD ya PKR?**

SRS mein Security Deposit `325 / 500 / 250$` likha hai, aur connection fees
dollar mein. Frontend PKR symbols use kar raha hai. Ye decide hone tak
pricing, billing, aur discount ka kaam ruka hua hai. Jawab chahiye.
