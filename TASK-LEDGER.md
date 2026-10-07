# TASK-LEDGER — NEXUS Service Marketing System

Yahan sab kaam track hoti hai. Jo **nahi** hua wo bhi likhna hai — sirf done wali list nahi.

Last updated: 5 Oct 2026

Legend: `DONE` / `PARTIAL` / `TODO` / `PLAN`

---

## Phase 1 — TooTa hua fix (frontend)

| # | Kaam | Status |
|---|---|---|
| 1.1 | `wwwroot/js/validation.js` missing — Layout 404 load karta hai | DONE |
| 1.2 | `NEXUS.getUser()` / `saveUser()` define nahi — Login crash karta hai | DONE |
| 1.3 | CSS load order galat (`home.css` cascade mein pehle aa raha tha) | DONE |
| 1.4 | Duplicate `@section Styles` (Home/Services/Plans/Orders views) | DONE |
| 1.5 | Emoji cleanup ne toota star rating (`CS1011`) | DONE |

## Phase 2 — Design system ek karna

| # | Kaam | Status |
|---|---|---|
| 2.1 | `home.css` — 150 hardcoded hex → `--nexus-*` tokens | DONE |
| 2.2 | `login-stars.css` — hardcoded colors tokens par | DONE |
| 2.3 | `site.css` — 4 naye tokens (`accent-bright`, `info-ink`, `background-alt`, `text-invert`) | DONE |
| 2.4 | Inline styles hatao — Reports (49), Accounts (23), Retail (18), Technical (16) | TODO |
| 2.5 | `!important` cleanup (public-pages 9, site 6, orders 4, forms 3) | TODO |
| 2.6 | CDN dependencies (Font Awesome, Chart.js, Google Fonts) → local | TODO |

## Phase 3 — Fake data hatana

| # | Kaam | Status |
|---|---|---|
| 3.1 | Homepage "1,245 Active Customers" / "42 Cities" counters | DONE |
| 3.2 | "4.8★ rating" / "Trusted Provider" badge | DONE |
| 3.3 | 3 fake testimonials (Bilal/Sana/Usman) | DONE |
| 3.4 | "Join over 1,200 customers across 42 cities" strip | DONE |
| 3.5 | External images (Unsplash 42 refs, pravatar) → 18 local assets | DONE |
| 3.6 | Dashboard topbar search bar hataya (user preference) | DONE |
| 3.7 | 55 emoji 6 dashboards se hataye | DONE |
| 3.8 | Admin dashboard fake chart data / KPI values clear | DONE |
| 3.9 | `ProfileController` hardcoded user + `AvatarUrl` | DONE |
| 3.10 | Feedback page fake rating badges + broken stars | DONE |

## Phase 3.5 — Documentation (context preserve)

| # | Kaam | Status |
|---|---|---|
| D.1 | `docs/01-project-context.md` | DONE |
| D.2 | `docs/02-srs.md` | DONE |
| D.3 | `docs/03-requirements-matrix.md` | DONE |
| D.4 | `docs/04-frontend-audit.md` | DONE |
| D.5 | `docs/05-frontend-decisions.md` | DONE |
| D.6 | `docs/06-database-schema.md` | DONE |
| D.7 | `docs/07-api-contract.md` | DONE |
| D.8 | `docs/08-backend-progress.md` | DONE |
| D.9 | `docs/09-testing-checklist.md` | DONE |
| D.10 | `docs/10-changelog.md` | DONE |
| D.11 | `docs/11-implementation-plan.md` (poora master plan) | DONE |

## Phase 4 — Backend, DB, Security (plan tayyar, review baaki)

| # | Kaam | Status |
|---|---|---|
| 4.1 | SQL Server + EF Core setup, DbContext, migrations | PLAN |
| 4.2 | Domain models (21 tables — dekho `docs/06`) | PLAN |
| 4.3 | Authentication (cookie auth, password hashing, lockout) | PLAN |
| 4.4 | Authorization — 5 roles (Admin/Accounts/Technical/Retail/Customer) | PLAN |
| 4.5 | Forms validation — client + ModelState + service (3 layer) | PLAN |
| 4.6 | Security — CSRF, XSS, IDOR, rate limiting, HTTPS, headers | PLAN |
| 4.7 | API layer — `IApiService` real EF implementation | PLAN |
| 4.8 | Error handling — global handler, logging, branded pages | PLAN |
| 4.9 | Performance — AsNoTracking, paging, indexes, caching | PLAN |
| 4.10 | Accessibility — ARIA, keyboard nav, contrast, focus | PLAN |

## Phase 5 — SRS features jo abhi nahi hain

| # | Kaam | Status |
|---|---|---|
| 5.1 | Vendor management | TODO |
| 5.2 | Stock / equipment inventory | TODO |
| 5.3 | Retail shop management | TODO |
| 5.4 | Bulk / Corporate discount (25/50/75/100%) | TODO |
| 5.5 | Service Tax 12.24% | TODO |
| 5.6 | Account ID format (1 letter + 3 digit city + 12 serial) | TODO |
| 5.7 | Order ID format (D/T/B + 10 serial) | TODO |
| 5.8 | Telephone-only connection type (landline requirement) | TODO |
| 5.9 | Feasibility check workflow | TODO |
| 5.10 | Advanced search (ID/naam/type/date/contact) | TODO |
| 5.11 | Connection lifecycle (Active/Temp Inactive/Perm Inactive) | TODO |
| 5.12 | SRS ke actual plans (USD, hourly/Kbps tiers) — abhi PKR plans hain | TODO |

## Phase 6 — Aptech deliverables

| # | Kaam | Status |
|---|---|---|
| 6.1 | ER Diagrams | TODO |
| 6.2 | Algorithms | TODO |
| 6.3 | GUI Standards Document | TODO |
| 6.4 | Interface Design Document | TODO |
| 6.5 | Unit Testing Check List | PARTIAL (`docs/09` draft tayyar) |
| 6.6 | Project Report + synopsis | TODO |
| 6.7 | 2 status emails (Aptech ko) | TODO |

---

## Phase 4.0 — Foundation ✅ DONE

| # | Kaam | Status |
|---|---|---|
| 4.0.1 | Packages: EF Core 8.0.11 (downgrade), Identity, FluentValidation, Serilog | DONE |
| 4.0.2 | Folder structure (Data/, Domain/, Services/, Common/, Validation/) | DONE |
| 4.0.3 | `Result<T>` + `ErrorKind` + `PagedResult<T>` + `PageRequest` | DONE |
| 4.0.4 | Constants (TaxConstants, IdFormats, BulkDiscountTiers, NexusRoles, enums) | DONE |
| 4.0.5 | DbContext + 22 entity configurations + design-time factory | DONE |
| 4.0.6 | Serilog (console + rolling file, Logs/) | DONE |
| 4.0.7 | `IPasswordService` (Identity PBKDF2 wrapper) | DONE |

## Phase 4.1 — Database ✅ DONE

| # | Kaam | Status |
|---|---|---|
| 4.1.1 | `Initial_Foundation` migration — 22 tables, 0 warnings | DONE |
| 4.1.2 | `NexusDb` created on `DESKTOP-BD6OLPC\SQLEXPRESS` | DONE |
| 4.1.3 | Seed: 8 cities, 10 plans, 29 prices, 4 tiers, 6 products, 1 admin, 1 shop | DONE |
| 4.1.4 | Query filter decision — koi global filter nahi (audit flags only) | DONE |
| 4.1.5 | Concurrency `RowVersion` on Bill / Connection / StockItem | DONE |
| 4.1.6 | Smoke test — sab pages HTTP 200, log mein 0 errors | DONE |

## Phase 4.2 — Authentication ✅ DONE

| # | Kaam | Status |
|---|---|---|
| 4.2.1 | Cookie auth (HttpOnly, SameSite=Lax, 60 min, no sliding) | DONE |
| 4.2.2 | `AuthenticationService` — generic error, 5-attempt lockout | DONE |
| 4.2.3 | `IAccountIdGenerator` — AccountId/OrderId/Bill/Receipt/Connection | DONE |
| 4.2.4 | Login flow + role-based redirect + forced password change | DONE |
| 4.2.5 | Open-redirect protection (`Url.IsLocalUrl`) | DONE |
| 4.2.6 | Frontend sessionStorage hata, topbar real user se | DONE |
| 4.2.7 | User account menu (profile / change pwd / sign out) | DONE |

## Phase 4.3 — Authorization ✅ DONE

| # | Kaam | Status |
|---|---|---|
| 4.3.1 | 5 roles enforced: Admin, Accounts, Technical, Retail, Customer | DONE |
| 4.3.2 | Har dashboard controller par `[Authorize(Roles=…)]` | DONE |
| 4.3.3 | AJAX requests ko 403, browser ko redirect | DONE |
| 4.3.4 | Branded AccessDenied page | DONE |
| 4.3.5 | `IAccessGuard` ownership checks (IDOR) — **Phase 5 mein, kyunki abhi data nahi** | TODO |

## Phase 4.4–4.7 — partially done

| # | Kaam | Status |
|---|---|---|
| 4.4.1 | Client validation + ModelState | DONE |
| 4.4.2 | Domain business rules (V1–V10) | TODO (Phase 5) |
| 4.5.1 | CSRF tokens har POST par | DONE |
| 4.5.2 | Account enumeration protection | DONE |
| 4.5.3 | Security headers, CSP, rate limiting | TODO |
| 4.6.1 | Auth + generator services | DONE |
| 4.6.2 | Domain services (customer, order, billing…) | TODO (Phase 5) |
| 4.7.1 | AccessDenied page | DONE |
| 4.7.2 | 404 / 500 pages, global handler | TODO |

---

## Phase AUDIT — Full system audit (7 Oct 2026) ✅ DONE

Poora system audit ho gaya — **6 portals, 36 views, 14 controllers, 48 actions**.
Sab kuch likha hai: **`docs/13-full-system-audit.md`**

| Cheez | Count |
|---|---|
| Portals | 6 |
| Pages kaam kar rahe (REAL) | 23 |
| Pages STATIC (sirf UI) | 5 |
| Pages BROKEN | 6 |
| Features MISSING | 17 |
| NAV-BUGS | 10 |
| Missing features (SRS) | 20 |

**Sab se ahem nateeje:**

- **N1 — Double active sidebar ki asal wajah mili:** `wwwroot/js/navigation.js`
  (`initActiveLink`) server ke `IsActive()` ko override kar deta hai, aur
  `path.startsWith(href)` se `/Operations/Connections` pe `/Operations` bhi
  active ho jata hai. Ye har nested route pe hota hai.
- **Plans CRUD bilkul nahi hai** — `/Plans` public page hai, admin ke liye
  edit/create/delete kuch nahi.
- **`/Home/Privacy` 404 hai** lekin do signup forms se link aa raha hai.
- **Contact form submit pe kuch nahi hota** (`data-save-demo`).
- **`Admin/Settings` save nahi hoti** — koi backend nahi.

**Aage ka order (user ne approve kiya):**

- **Phase A** — Admin CRUD backbone (pattern + Plans + Cities + Discounts)
- **Phase B** — Nav fixes (N1–N10)
- **Phase C** — Shared core (ChangePassword, Settings, ApiService hatana)
- **Phase D** — Panel-wise audit + fix (Public → Customer → Retail → Technical → Accounts → Admin)
- **Phase E** — Missing features (Shops, Employees, Vendors, Stock, Users)
- **Phase F** — Design pass (admin pages premium)

---

## Phase 5 — SRS Features

| ID | Kaam | Status |
|---|---|---|
| 5.1.1 | Customer registration (asli DB writes) | DONE |
| 5.1.2 | Account ID auto-generate (type + city + serial) | DONE |
| 5.1.3 | Order auto-create on signup | DONE |
| 5.1.4 | Duplicate CNIC / phone / email block | DONE |
| 5.1.5 | Rule V1 — landline requirement | DONE |
| 5.1.6 | Register + New Connection ek flow mein merge | DONE |
| 5.1.7 | POST end-to-end verify (account + order + signin) | DONE |
| 5.1.8 | Navbar "Register" → "New Connection" (home + login bhi) | DONE |
| 5.1.9 | Plan auto-select (pehla visible plan khud check) | DONE |
| 5.1.10 | Selected plan ka visual (border + fill + tick badge) | DONE |
| 5.1.11 | Dial-Up plan filter bug — `DialUp` vs `Dial-Up` token mismatch | DONE |
| 5.1.12 | Currency Rs. → USD (73 occurrences, 14 files) | DONE |
| 5.1.13 | Plans page asli DB data par (mock `IApiService` hataya) | DONE |
| 5.1.14 | Home featured plans + comparison table asli data se | DONE |
| 5.1.15 | Order summary live update (panel form ke bahar tha) | DONE |
| 5.1.16 | Cycle picker form ke andar (POST hi nahi hota tha) | DONE |
| 5.2.1 | Billing engine (12.24% tax, bulk discount, call charges) | DONE |
| 5.2.2 | Invoice + outstanding + payments | DONE |
| 5.2.3 | Monthly billing run (idempotent, per-period) | DONE |
| 5.2.4 | Payment recording + receipt numbers + status transitions | DONE |
| 5.2.5 | Bill views (ledger + invoice detail) asli data par | DONE |
| 5.3.1 | Feasibility workflow + connection lifecycle | DONE |
| 5.3.2 | Navbar auth-aware (signed-in → Dashboard + Logout) | DONE |
| 5.3.3 | Sidebar logout asli POST form (tha `/Account/Login` link) | DONE |
| 5.3.4 | Connections register header counts (filtered list se ban rahe the) | DONE |
| 5.3.5 | `ChangePassword` (POST) chalu karna — abhi disabled hai | TODO |
| 5.3.6 | Accounts dashboard asli data par (`AccountsDashboardService`) | DONE |
| 5.3.7 | Technical dashboard asli data par (`TechnicalDashboardService`) | DONE |
| 5.3.8 | Retail dashboard asli data par (`RetailDashboardService`) | DONE |
| 5.3.9 | Retail search — server-side, asli DB query (`SearchService`) | DONE |
| 5.3.10 | `Retail/NewOrder` — asli form (`RegistrationService` se juda) | DONE |
| 5.3.11 | `Admin/Settings` se PKR / Fiber / Wireless hata diya | DONE |
| 5.4.1 | Advanced search (ID / naam / type / date / contact) | TODO |
| 5.5.1 | Vendor / procurement | TODO |
| 5.5.2 | Stock / inventory | TODO |
| 5.5.3 | Retail shop + employee management | TODO |
| 5.6.1 | Reports (connections, revenue, outstanding, bulk) | TODO |
| 5.7.1 | Customer pages asli data par | TODO |
| 5.7.2 | `IAccessGuard` ownership checks (IDOR) | TODO |

### Ab bhi mock data wale pages (honest list)

Ye pages **abhi bhi** mock rows dikhate hain — 5.3 ke batch mein nahi hue:

| Page | Mock hits | Notes |
|---|---|---|
| `Views/Admin/Reports.cshtml` | 20 | Pura mock: fake customers, orders, "Monthly revenue (PKR)" |
| `Views/Customer/MyConnection.cshtml` | 2 | Chhota — portal service maujood hai, sirf wire karna |
| `Views/Feedback/Index.cshtml` | 2 | Chhota |
| `Models/Services/Api/ApiService.cs` | — | `Rs.` + `NX12345678` / `Ahmed Khan` hardcoded |

### Phase 5 ke bugs jo mile aur fix hue

- **Transaction + retry strategy crash.** `EnableRetryOnFailure` aur manual
  `BeginTransactionAsync` saath kaam nahi karte — EF khud ke banaye transaction
  ko replay nahi kar sakti. Registration 500 de raha tha. Ab poora transaction
  `CreateExecutionStrategy().ExecuteAsync()` ke andar hai. **Ye pattern ab har
  transaction wale write path ke liye zaroori hai.**
- **Rule V1 chup-chaap skip ho raha tha.** `Normalise()` khaali string ko `""`
  deta tha, is liye `is null` check kabhi true nahi hua. Teen Dial-Up customers
  bina landline register ho gaye the. `Optional()` null deta hai — test ke baad
  DB saaf kar diya.
- **Enum columns mein int likhna (Phase 5.2).** Raw SQL se test data seed karte
  waqt `Status=1` likha, magar poore project mein enums `HasConversion<string>()`
  hain — DB mein `'Active'` store hota hai. `GenerateForPeriodAsync` ne 0
  connections dhundhe. Code theek tha, test data galat. **Raw SQL se seed karte
  waqt enum columns mein string value do.**
- **Filter token kabhi `DisplayName()` se mat banao (Phase 5.1c).** Plan card
  ka `data-type` `"Dial-Up"` tha (display name) lekin type radio ka `value`
  `"DialUp"` (enum member name). Is liye Dial-Up select karne par **0 plans**
  dikhte the — `DialUp` kabhi `Dial-Up` se match nahi karta. Broadband aur
  Telephone sirf ittefaqan chal rahe the kyunke unke dono naam ek jaise hain.
  Token ke liye `ToString()`, dikhane ke liye `DisplayName()`.

---

## BLOCKER — RESOLVED ✅

**Currency: USD.** (5 Oct 2026) SRS hi source of truth hai. Frontend ke saare
PKR symbols (`Rs. 1,200 / 2,000 / 3,500`) SRS ke USD plans se replace honge.
Tax 12.24% hi rahega (SRS ke mutabiq). Equipment pricing scope se bahar —
sirf stock quantity track karenge.

Poora implementation plan: **`docs/11-implementation-plan.md`**

---

## Notes / Findings

- **Code First approach** chuna. DB pehle nahi banani — C# entities se EF tables
  banata hai. Migration `Data/Migrations/20261005155909_Initial_Foundation`.
- **EF Core 9.0.20 → 8.0.11 downgrade kiya.** Project `net8.0` hai magar packages
  9.x the — runtime par weird errors deta. Ab match hai.
- `csproj` ke comments "frontend-only, no database" **galat the** — ab DB asli
  mein live hai.
- **Query filter decision:** koi global soft-delete filter nahi rakha. Wajah:
  "deleted" ka matlab har screen par alag hai — closed customer phir bhi Accounts
  ledger, outstanding dues aur audit trail mein aana chahiye. Filter model-level
  par lagane se wo rows har jagah se chhup jaatin. Ab services per-query decide
  karengi. `IsDeleted` sirf audit field hai.
- Migration ke waqt `RetailShops=0` aa raha tha — wajah: `SeedCitiesAsync`
  `AddRange` karta tha magar `SaveChanges` sab ke aakhir mein tha, is liye
  `SeedDemoOutletAsync` ko city DB mein nahi milti thi. Cities ke foran baad
  ek `SaveChangesAsync` daala.
- `IPasswordHasher` naam Identity ke `IPasswordHasher<TUser>` se clash kar raha
  tha (`CS0305`). `IPasswordService` rename kiya.
- Build: **0 Errors, 10 Warnings** (sab `ServiceViewModel.cs` ke CS8618 — Phase 4
  mein fix honge). Migration: **0 warnings**.
- 910 inline styles abhi baaki hain (Reports 49, Status 150 hex) — Phase 2 backlog.
- **Registration ab asli hai** (Phase 5.1). Baaki sab pages abhi bhi
  `ApiService.cs` ke mock se data le rahe hain — ye Phase 5 ka asli kaam hai.
- **Billing ab asli hai** (Phase 5.2). `BillsController` + dono bill views asli
  DB par. Tax discounted amount par lagta hai, tiers SRS ke, aur billing run
  idempotent hai. 46 arithmetic + 15 lifecycle checks pass, plus HTTP par
  CSRF aur IDOR dono verified.
- **`Connections.OrderId` par unique index hai** — ek order = ek connection.
  Is se zyada connections chahiye to alag orders banane honge.
- **DB ki haalat (6 Oct, test data hatane ke baad):** `Users` 1,
  `Customers` 0, `ConnectionOrders` 0, `Connections` 0, `Bills` 0, `Payments` 0.
  Sirf reference data (8 tables) bhara hua hai.
- **`MustChangePassword` test artifact:** lockout testing ke waqt admin ka
  `MustChangePassword = 0` kar diya tha. Wapas `1` kar diya gaya hai aur
  verify bhi.
- **Registration aur New Connection ek hi flow hain** (Phase 5.1b). `/Orders/New`
  connection bhi leta hai aur account bhi banata hai, ek transaction mein.
  `/Account/Register` sirf `302` karta hai `/Orders/New` par. `ConnectedOrder`
  ke saath `PreferredSlot` / `ScheduledFor` / `Notes` bhi persist hote hain.
  POST end-to-end verify hua: `302` → `/Customer/Dashboard`, teen tables
  (Users → Customers → ConnectionOrders) linked, phir test rows delete.
