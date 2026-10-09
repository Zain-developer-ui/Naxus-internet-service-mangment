# TASK-LEDGER — NEXUS Service Marketing System

Yahan sab kaam track hoti hai. Jo **nahi** hua wo bhi likhna hai — sirf done wali list nahi.

Last updated: 8 Oct 2026 (4-din plan tayyar)

> **SRS scorecard (8 Oct, code padh kar):** 53 requirements mein se
> **37 DONE (70%)**, **8 PARTIAL (15%)**, **8 MISSING (15%)**.
> Purani `docs/03-requirements-matrix.md` (5 Oct) **ghalat hai** — us waqt 2% tha.
>
> **4-din ka plan: `PLAN-4-DAYS.md`** (workspace root) ya
> `Documents/Obsidian Vault/NEXUS/NEXUS - 4 Day Plan.md`
>
> **Static audit: `STATIC-DATA-AUDIT.md`** — 4 HIGH, 5 MEDIUM, 4 LOW findings (~7 ghante ka kaam)
> **Testing: `TESTING-GUIDE.md`** — team member ke liye ~70 test cases + cross questions

---

## Static data — POORA FIX HO GAYA (8 Oct audit + fix)

**Achhi khabar:** zyadatar project asli DB par chalta hai. Static sirf Customer
portal aur JS ke dead functions mein tha. **13 findings — sab fix.**

| Sev | Kahan | Kya tha | Ab |
|---|---|---|---|
| HIGH | `wwwroot/js/bills.js` | 148-line **dead file** — saare 9 selectors kisi view mein nahi, har page par load, "Payment processed (demo)" fake toast | **Poori file delete** + `_Layout` se script tag hata |
| HIGH | `Views/Customer/MyConnection.cshtml` | Fallback model `Ahmed Khan` / `NX12345678` | Fallback poora hata (`var acc = Model`) |
| HIGH | `Views/Customer/MyConnection.cshtml` | Timeline hardcoded ("08 Jan 2025", "Usman Ahmed") | `ConnectionStatusHistory` + `FeasibilityChecks` se real |
| HIGH | `Views/Customer/MyConnection.cshtml` | Plan features + equipment hardcoded ("Unlimited data" — SRS ke against) | `Plan` columns se derive + `EquipmentProducts` se |
| MED | `MyConnection` + `Customer/Dashboard` | Fake service health ("Excellent", "Stable", "Verified") | Real fields (status, speed, billing) |
| MED | `Customer/Dashboard` | Fallback dates ("25 Oct 2025" waghera) | `"—"` |
| MED | `dashboard.js` | "not available in this demo" | "not wired up yet" |
| MED | `Account/Status.cshtml` | placeholder purana format | `B042000000000001` |
| LOW | `charts.js` | stale comment ("demo data") | Comment update |
| LOW | `Home/Contact` + `Home/Index` | Fake "Live Chat" (4 jagah) | "Send a Message" → real form |
| LOW | `MyConnection` | 40+ inline styles | Classes (`banner-identity`, `badge-ghost`, `stat-value.compact`, `inline-actions`) |

**Naye view-model fields** (`AccountViewModel`): `PlanId`, `PlanFeatures`,
`Timeline`, `Equipment` + `HasPlanFeatures`/`HasTimeline`/`HasEquipment`.
`BillingStatus` ab bills se compute hota hai (pehle khaali reh jata tha).

**Dead field hataya:** `TechnicianName`. **`RouterModel`** ab
`EquipmentProducts` se set hota hai.

**Verified (curl + screenshot):** `/js/bills.js` → **404**. `MyConnection` par
"Bilal Ahmed Khan · B042000000000002 · Broadband 60 Hours", plan features
"60 hours of usage included each cycle" + "$500.00 security deposit", timeline
3 real entries (06 Oct 2026), equipment 6 real products. 10 pages → sab 200.

**Data safe:** Users 6, Customers 2, Orders 2, Connections 1, Bills 0, Plans 10,
Cities 8, EquipmentProducts 6.

---

## M2 — Advanced search (9 Oct 2026) ✅ DONE

SRS: *"The advanced search option should be included for checking the status of the
order or the status of the connection with the options being the unique id, name on
which the order is placed or the connection is taken, type of connection, date or
period of application or received the connection, contact number."*

Pehle: `/Retail/Search` par **ek text box** tha (`SearchService.SearchAsync(string? term)`).

Ab: naya `/Search/Advanced` — **5 filters**, SRS ke exactly.

| Cheez | Kahan |
|---|---|
| Query + results view model | `Models/ViewModels/AdvancedSearchViewModel.cs` |
| Service | `Services/Search/AdvancedSearchService.cs` |
| Controller | `Controllers/SearchController.cs` (`[Authorize(Roles = StaffRoles)]`) |
| View | `Views/Search/Advanced.cshtml` |
| Sidebar | `_Sidebar.cshtml` — dono branches mein |

**Design faisle:**

- **Alag controller.** `/Retail/...` ke andar nahi rakha kyunki technician aur
  accounts clerk ko bhi wahi filters chahiye. `StaffRoles` = Admin + Accounts +
  Technical + Retail.
- **Do result sets.** Orders `CreatedAt` par filter hote hain (application date),
  lines `ActivatedOn` par (received date). Ek hi period, dono apne apne column se —
  yahi SRS ka *"date or period of application or received the connection"* hai.
- **Khaali query = kuch nahi.** Bina filter ke poori book nahi dikhati; screen
  kehti hai "Enter at least one filter".
- **GET form.** URL shareable rehta hai.
- **`To` date poora din cover karta hai** — `To.Date.AddDays(1)` se `<` compare,
  warna 31 Oct ka din chhoot jata.

**Live verification (curl, 8 tests):**

| Test | Nateeja |
|---|---|
| Build | 0 errors |
| Khaali query | 200 — "Enter at least one filter" |
| `?Id=B0000000001` | 200 — order mila |
| `?Name=Bilal` | 200 — 1 order + 1 line |
| `?Type=Broadband` | 200 — 2 orders + 1 line |
| `?Contact=0321` | 200 — 1 order + 1 line |
| `?From=2026-10-01&To=2026-10-31` | 200 — 2 orders + 1 line |
| `?From=2026-01-01&To=2026-01-31` | 200 — "Nothing matches those filters" |
| `?Name=ZZZNobody` | 200 — "Nothing matches those filters" |
| Guest | **302 → /Account/Login?ReturnUrl=%2FSearch%2FAdvanced** |
| Retail login | **200** |
| Sidebar | har page par exactly **1** active link |

**Data safe:** Users 6, Customers 2, Orders 2, Connections 1, Bills 0, Plans 10, Cities 8.

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

## Phase A/B/C — Nav fixes + mock hatana (7 Oct 2026) ✅ DONE

Sab kuch build-verified aur **live server par curl se test kiya gaya** (`localhost:5199`).

### Nav bugs (N1–N10) — sab fix

| # | Bug | Fix |
|---|---|---|
| N1 | Nested route pe do sidebar items active | `navigation.js` `initActiveLink()` ab sirf `.nav-links` (public navbar) pe chalta hai — sidebar server-side `IsActive()` se mark hoti hai |
| N2 | Accounts sidebar `/Bills/Details` (id ke bina) → 404 | Ab `/Bills?status=Overdue` (real filter) |
| N3 | Admin sidebar `IsActive("retail")` kabhi match nahi karta | Key `dashboard` kar di |
| N4 | Admin sidebar `IsActive("customers")` kabhi match nahi karta | Key `search` kar di |
| N5 | Technical sidebar `Installations` → `/Orders/Tracking` (galat) | Ab `/Operations` |
| N6 | Admin sidebar `Orders` → `/Orders/Tracking` (id ke bina) | Ab `/Retail/Orders`… **nahi**, `/Orders/Tracking` hi rakha magar `SetActiveItem("orders")` ke saath |
| N7 | `OperationsController` `SetActiveItem` call hi nahi karta tha | `Index`/`Review`/`Connections`/`Connection` — sab mein add |
| N8 | `/Home/Privacy` **404** (do signup forms se linked) | Action + real policy page banaya. Ab 200 |
| N9 | Contact form submit pe kuch save nahi hota | Real POST → `Feedback` table, anti-forgery + validation |
| N10 | `/Services` 100% static (`ApiService` mock) | Naya `ServiceCatalogService` — live plans se derive |

### Mock data hataya — `ApiService` poora delete

`Models/Services/Api/ApiService.cs` + `IApiService.cs` **delete**. Uske 4 consumers real services pe shift:

| Page | Pehle | Ab |
|---|---|---|
| `/Services` | 9 hardcoded services | `ServiceCatalogService` — plans se group, live rates |
| `/Orders/Tracking` | Mock "Ahmed Khan" order | `OrderTrackingService` — real order + real timeline |
| `/Account/Status` | Fake "Ahmed Khan" dump | `CustomerPortalService.LookupAsync` — real DB lookup |
| `/Feedback` | Toast dikha kar kuch save nahi | `FeedbackService` — real save + history |

### Naye features

- **Plans CRUD** — `AdminPlansController` (`/Admin/Plans`), `PlanAdminService`, 2 views, CSS+JS.
  Create/Edit/Toggle/Delete. Delete sirf tab jab plan pe koi order na ho (warna `Conflict` + "retire karo").
- **Order tracking timeline** — real `OrderStatus` se 4-stage ladder. Cancelled/FailedFeasibility pe ladder ruk jati hai (`failed` state, red dot).
- **ChangePassword** — pehle "not enabled yet" tha. Ab `AuthenticationService.ChangePasswordAsync` (current password re-verify, same-password reject).
- **Reports** — `ReportService` + `/Admin/Reports` poori tarah rewrite. Har figure DB se.
  Filters GET form (shareable URL), CSV export real (`/Admin/ExportCustomers`).

### Chart.js offline

`chart.umd.min.js` **CDN se local** (`wwwroot/lib/chart.js/`) — competition demo offline ho to charts tootein na.
`charts.js` `autoInit` ab `data-labels`/`data-values` bhi padhta hai (pehle markup mein the magar **ignore** ho rahe the).

### Staff accounts — bara gap tha

System mein sirf **Admin + 2 Customer** the. **Retail, Technical, Accounts** ka koi user hi nahi —
yani 3 portals login hi nahi ho sakte the. `NexusSeeder.SeedStaffAsync` add kiya:

| Role | Account ID | Password | Employee |
|---|---|---|---|
| Retail | `R0000000000001` | `Nexus@2026` | Sana Iqbal (EMP-R01, Clifton outlet) |
| Technical | `T0000000000001` | `Nexus@2026` | Usman Ahmed (EMP-T01) |
| Accounts | `F0000000000001` | `Nexus@2026` | Hina Raza (EMP-F01) |

Admin: `admin@nexus.example` / `Nexus@2027`.
**Customer accounts (`B042...`) ka password pata nahi** — registration flow se bane the, seed se nahi.

### Live verification (curl, running server)

| Cheez | Nateeja |
|---|---|
| Build | 0 errors |
| `/Home/Privacy` | 200 (pehle 404) |
| Admin login | 302 → `/Admin/Dashboard` |
| Retail / Technical / Accounts login | 302 → apne apne dashboard |
| `/Admin/Plans` | 10 real plans |
| Plan create | 302 → DB mein 11, teeno price rows sahi |
| Plan delete | 302 → wapas 10, **0 orphan prices** |
| `/Operations/Connections` sidebar | Sirf "Connections" active (pehle double) |
| Order tracking `B0000000001` | 1 done + **1 failed**, badge danger |
| Order tracking `B0000000002` | 4 done, badge success |
| Reports KPIs | 2 customers, 1 active connection, 2 orders, 1 completed |
| Reports tables | Real account IDs + statuses |
| CSV export | `text/csv`, real rows + plan names + rates |
| Services page | Live rates ($15 / $50 / $175 per cycle) |

**Data safe:** Plans 10, Orders 2, Customers 2, Bills 0, orphan prices 0. Kuch loss nahi hua.

---

## Phase A (baqi) + Settings backend (7 Oct 2026) ✅ DONE

### Reports ka apna controller — 5 dead links the

`/Admin/Reports` sirf Admin ko milta tha. Retail, Technical aur Accounts ke
dashboard/sidebar se 5 links wahan jaate the aur **302 → AccessDenied** dete the.

- Naya `Controllers/ReportsController.cs` — `[Authorize(Roles = NexusRoles.StaffRoles)]`.
- `Views/Admin/Reports.cshtml` → `Views/Reports/Index.cshtml`.
- `ExportCustomers` bhi wahan shift. `AdminController` se dono actions hata diye.
- **9 links** `/Reports` par repoint (`_Sidebar` x4, Accounts Dashboard x2,
  Admin Dashboard x2, Technical Dashboard x1, `dashboard.js` x1).

**Verify (curl, chaar roles):**

| Role | `/Reports` | `/Reports/ExportCustomers` |
|---|---|---|
| Admin | 200 | 200 |
| Retail | 200 | 200 |
| Technical | 200 | 200 |
| Accounts | 200 | 200 |
| Guest | 302 → `/Account/Login?ReturnUrl=%2FReports` | — |

### Cities CRUD — `/Admin/Settings/Cities`

`SettingsAdminService` (naya) + `AdminSettingsController` + 2 views.

- City code **3 digit** hai aur account ID ke andar embed hota hai, is liye
  jis city mein customer register ho chuka ho wahan code **lock** ho jata hai
  (`CodeLocked` → input `readonly`). Naam badalna phir bhi chalta hai.
- Duplicate name aur duplicate code — dono block.
- Delete sirf tab jab na customers hon na outlets. Warna `Conflict` +
  "not served mark karo".
- `ToggleServed` — city record par rehti hai magar registration par offer nahi hoti.

### Bulk discount slabs CRUD — `/Admin/Settings/Discounts`

- `Rate` DB mein fraction (0.25) hai, form percent (25) dikhata hai.
  `PercentRate` property dono taraf convert karti hai.
- **Overlap detection:** do active slabs ek hi headcount cover karein to
  discount ambiguous ho jata hai. Save par block + list par `Overlaps` badge.
- Open-ended top slab ke liye checkbox (blank number input aur "deliberately
  blank" wire par ek jaise lagte hain).

### `Admin/Settings` — poora page fake tha

Pehle 100% static demo tha: "1,245 customers", "842 Active", "API Status: Mock /
Demo", "no changes are persisted to a database", aur teen fake action buttons
(cache clear / refresh demo / health check) jo sirf toast dikhate the.

Ab **settings hub** hai — `SystemOverview` live DB se bharta hai:

| Tile | Live figure |
|---|---|
| Customer Accounts | `Customers` count |
| Live Connections | `Connections` where `Status = Active` |
| Orderable Plans | `Plans` where `IsActive` |
| Cities Served | `Cities` where `IsServiced` |
| Cities & Service Areas | total + serviced → `/Admin/Settings/Cities` |
| Bulk Discount Slabs | total + active → `/Admin/Settings/Discounts` |
| Service Plans | total + active → `/Admin/Plans` |
| Billing & Tax | bills issued + `TaxConstants.ServiceTaxRate` (12.24%) |

Runtime panel bhi real: `EnvironmentName`, DB name (`NexusDb`), server time,
process uptime. `TaxConstants` se padha jata hai is liye hub kabhi engine se
drift nahi kar sakta.

### Live verification (curl, running server)

| Cheez | Nateeja |
|---|---|
| Build | 0 errors |
| `/Admin/Settings` | 200 — 2 customers / 1 live connection / 10 plans / 8 cities served |
| `/Admin/Settings/Cities` | 200 — 6 real cities (Lahore 42, Karachi 21, ...) |
| `/Admin/Settings/Discounts` | 200 — 4 real slabs (10-15 → 25%, 15-25 → 50%, 25-50 → 75%, 50+ → 100%) |
| City create | 302 → 9 cities, `Testville` list mein |
| City delete | 302 → wapas 8 |
| Delete guard | Lahore/Karachi par Delete button **render hi nahi hota** (customers hain) |
| Slab create (5-9) | 302 → 5 slabs |
| Slab overlap branch | 12-14 try kiya → 200 form, error `MinConnections` ke neeche: *"This slab overlaps the active 10-15 slab. Adjust the bounds or deactivate one."* |
| Slab delete | 302 → wapas 4 |

**Data safe:** Cities 8, Plans 10, Slabs 4, Orders 2, Customers 2 — test rows
(Testville + 5-9 slab) hatane ke baad original state.

---

## Phase E (pehla batch) — Back-office registers (7 Oct 2026) ✅ DONE

SRS ke woh features jo "nahi hain" list mein the. Chaar registers ek saath:

| Module | Route | Service | Authorize |
|---|---|---|---|
| Equipment stock + adjustments | `/Admin/Inventory` | `InventoryService` | Admin + Retail |
| Equipment catalogue | `/Admin/Inventory/Products` | `InventoryService` | Admin + Retail |
| Vendors | `/Admin/Vendors` | `VendorService` | Admin |
| Outlets + Staff | `/Admin/Organisation` | `OrganisationService` | Admin |

### Rules jo service layer mein hain

- **Stock cannot go negative.** `after < 0` → `Fail` with the exact on-hand figure.
  Clamping quietly would hide a ledger error, so it is refused.
- **Every adjustment needs a reason** — audited against the ledger, never optional.
- **Movement row + quantity update in one `SaveChangesAsync`** — the ledger and
  the on-hand figure can never disagree.
- **Product delete guard:** stock on hand, or appearing on a past purchase order →
  `Conflict`. Empty stock rows are cleaned up with the product.
- **Vendor soft delete** (`ISoftDeletable`) — purchase orders point back at it.
  Duplicate name and duplicate NTN both blocked.
- **Employee must link a real staff login.** Technical job filtering reads
  `User.Role`, so a floating employee row would be invisible. One employee per
  login, and customer logins are filtered out of the picker.
- **Outlet delete guard:** staff assigned, or stock on hand → `Conflict`.

### Live verification (curl, running server)

| Cheez | Nateeja |
|---|---|
| Build | 0 errors |
| 9 naye pages | sab 200 |
| `/Admin/Inventory` (seeded 12 + 3) | 2 products, 15 units, **1 below threshold** |
| Low-only filter | sirf `Broadband Router` (3 < 5) |
| Shop filter / search filter | dono sahi |
| Stock issue −2 | 302 → on hand **12 → 10**, ledger row `-2 / 10 / "Issued to installation B042-1"` |
| Over-issue −999 (on hand 10) | 200, field error: *"Only 10 unit(s) on hand. Cannot remove 999."* |
| Blank reason | 200, field error: *"A reason is required for every stock change."* |
| Vendor create | 302 → list mein |
| Employee create (spare staff login) | 302 → `EMP-T02` add |
| Employee double-link | blocked: *"System Administrator already has an employee record."* |
| New-employee picker | linked logins **filtered out** — sirf unlinked dikhte hain |
| `/Admin/Settings` | 4 naye tiles: Equipment, Vendors, Outlets & Staff |

**Data safe:** Stock 0, Movements 0, Employees 4, Vendors 0, Users 6, Plans 10,
Cities 8, Tiers 4 — test rows hatane ke baad. Kuch loss nahi hua.

### Inventory field-error prefix bug

`Result<int>.Fail(nameof(model.QuantityDelta))` **bare** property name deta hai
(`QuantityDelta`), lekin view model `Adjust.*` prefix se bind hota hai. Error
`ModelState` mein chala jata tha magar `asp-validation-for="Adjust.QuantityDelta"`
se match nahi karta tha — field ke neeche kuch render nahi hota tha, 200 ke saath
chup-chaap fail. Fix: controller `ModelState.AddModelError($"Adjust.{field}", …)`.

---

## Phase F (pehla batch) — Sidebar, confirm, website settings (8 Oct 2026)

### F1 — Confirm dialog sirf ek page par kaam karta tha

`data-confirm` handler `plan-edit.js` mein tha, aur wo **sirf** `AdminPlans/Edit`
load karta hai. Yani Cities / Discounts / Vendors / Organisation ke delete buttons
kabhi confirm **maangte hi nahi** the — seedha delete ho jata.

- Handler `site.js` mein shift ho gaya, **event delegation** ke sath (redirect ke baad
  nayi render hui rows bhi cover hoti hain).
- `site.js` mein `data-toast` / `data-toast-type` support add — koi bhi form submit
  par toast fire kar sakta hai.
- `plan-edit.js` se duplicate handler hata diya (warna double confirm).

### F2 — Sidebar active-state (root cause mili)

Do alag bugs the:

1. **View controller ko override kar raha tha.** `Cities.cshtml` mein
   `ViewData["ActiveSidebar"] = "settings"` likha tha, aur view **controller ke
   baad** chalti hai — is liye controller ka `SetActiveItem("settings-cities")`
   bekaar ho jata tha aur sirf "Settings" light hota tha. **25 views** se ye line
   hata di. Ab controller single source of truth hai.
2. **Key collision.** Admin sidebar mein `/Admin/Dashboard` aur `/Retail/Dashboard`
   dono `IsActive("dashboard")` the — dono active. Ab `admin-dashboard` /
   `retail-dashboard` alag keys hain.

### F3 — Cities "Stop serving" dobara on nahi hota tha

Sabit hua ke backend theek tha (curl se `POST ToggleServed` → 302, status flip).
Masla F1 wala hi tha — aur ab toggle ke **dono** direction par toast bhi lagta hai
(`data-toast` + `data-toast-type`).

### F4 — `Admin/Settings` website settings ban gaya

Pehle tile directory thi (Cities/Plans/Vendors ke cards) — ye ghalat tha. Ab asli
**website settings** hain, DB se wired:

- Naya `Domain/Organisation/SiteSetting.cs` — key/value store (`Key`, `Value`, `Kind`).
- `Services/Settings/SiteSettingsCatalog.cs` — 17 settings, 5 groups (General,
  Branding, Features, Contact, Security). Naya switch add karne ke liye sirf yahan
  ek entry — migration ki zaroorat nahi.
- `Services/Settings/SiteSettingsService.cs` — read (default fallback ke sath) +
  save (kind-wise validation pehle, phir ek `SaveChangesAsync`).
- `AdminController.Settings` ab GET form + POST save. Runtime panel (env, DB,
  uptime, live counts) side mein.
- Naya `wwwroot/js/settings.js` — colour swatch live preview.

| # | Kaam | Status |
|---|---|---|
| F1 | `data-confirm` global (site.js, delegation) | DONE |
| F2 | Sidebar active state — saare views + key collision | DONE |
| F3 | Cities toggle + dono taraf toast | DONE |
| F4 | `Admin/Settings` → website settings (DB backed) | DONE |
| F5 | Admin pages design pass (charts kam, header ek jaisa) | PARTIAL — Reports + Admin Dashboard DONE; 4 dashboards baqi |
| F6 | Settings POST — save waqai kaam karta hai | DONE |
| F7 | Skills adopt (24 user-level) | DONE |
| F8 | Obsidian vault setup (9 notes) | DONE |
| F9 | Admin Dashboard design pass + real charts | DONE |
| F10 | `.reveal` JS-fail fallback + print | DONE |

### F9 — Admin Dashboard (design + real data)

Pehle: 8 stat cards (hero-metric template), **3 khaali canvases**, 34 inline styles.

Ab:
- **"Waiting on someone" board** — 4 linked cells ek surface par (dividers ke saath,
  chaar alag cards nahi). Har cell `/Operations`, `/Bills?status=Overdue` waghera par
  jata hai. Blocked kaam warning ink leta hai, clear kaam muted.
- **Figures strip** — 4 reference figures (customers, revenue, outstanding, payments).
- **Real chart** — `IReportService.BuildAsync(ReportFilter.Default())` se
  `ReportPage.Monthly` liya. Ab orders chart real data dikhata hai
  (`0,0,0,0,0,2` — DB ke 2 orders). Revenue chart sirf tab jab revenue > 0 ho,
  warna flat-zero line ek khaali rectangle hai.
- **Service mix** → `_Breakdown` partial (real counts).
- **Inline styles 34 → 1** (sirf bar width, jo dynamic hai).

`_Breakdown.cshtml` `Views/Reports/` → `Views/Shared/` move (ab do jagah use hota hai).

### F10 — `.reveal` JS-fail par page blank ho jata tha

`.reveal { opacity: 0 }` CSS mein tha aur JS use reveal karta tha. Agar script
fail ho jaye ya print ho, **poora page invisible** reh jata — correct DOM ke saath.
Ab `.js .reveal` (class `<head>` mein inline script se lagti hai) + `@media print`
override.

### F11 — `charts.js` ka fake fallback (landmine)

Har chart function mein fake demo series thi (`[180000, 195000, …]`). Agar
`NEXUS_CHART_DATA` define na ho to wo chart mein chali jati. Ab `series()` helper
null deta hai aur chart banaye hi nahi jate.

### F12 — `.mono` identifiers wrap ho rahe the

`CON-B-000001` do lines mein toot raha tha (`CON-B-` / `000001`). Ab
`white-space: nowrap` + `tabular-nums`.

### Status document

Poora feature/flow/security audit: `Documents/Obsidian Vault/NEXUS/NEXUS - Status Report.md`

---

## M1 — Bill ke hisaab se connection status (8 Oct 2026) ✅ DONE

SRS ka core rule: *"they provide only the postpaid connection for which the bill
will be generated **based on which the status of the connection depends**."*

Pehle: `ConnectionStatus` enum mein `TemporarilyInactive` / `PermanentlyInactive`
the, magar **koi rule nahi** ke status kab badle. Sirf manual change tha.

Ab:

| Cheez | Kahan |
|---|---|
| Policy numbers (15 din suspend, 45 din close) | `Common/Constants/LifecycleConstants.cs` |
| Legal status moves — **ek jagah** | `Common/Extensions/ConnectionStatusExtensions.cs` |
| Sweep service | `Services/Lifecycle/ConnectionLifecycleService.cs` |
| Screen | `/Operations/Overdue` (`Views/Operations/Overdue.cshtml`) |

**Rules:** bill due date se **15 din** baad → `TemporarilyInactive`; **45 din**
baad → `PermanentlyInactive`. Draft / cancelled / paid bills ignore. Payment aa
jaye to line wapas `Active`.

**Design faisla:** sweep automatic timer par **nahi** hai. Ek line dead karna wo
tabdeeli hai jo operator pehle dekh le — is liye wahi query preview screen aur
apply button dono ko chalati hai.

**`IsLegalMove` refactor:** `FeasibilityService` mein private thi. Ab shared
extension hai, aur dono jagah se wahi use hoti hai — manual change aur automatic
sweep kabhi disagree nahi kar sakte.

### Live verification (curl + sqlcmd)

| Cheez | Nateeja |
|---|---|
| Build | 0 errors |
| Test bill (60 din overdue, $196.42) | insert |
| `GET /Operations/Overdue` | **200** — `CON-B-000001`, `Active → Permanently Inactive`, 1 line to close |
| `POST /Operations/ApplyOverdue` | **302 → /Operations/Overdue** |
| `Connections` | `Active` → `PermanentlyInactive`, `DeactivatedOn` stamp |
| `ConnectionStatusHistory` | row likhi: `Active → PermanentlyInactive`, reason service ne banaya, `ChangedById=1` |
| Sidebar (5 pages) | har page par exactly **1** active link |
| Test data | bill delete, connection wapas `Active`, history 2 rows (original) |

**Data safe:** Bills 0, Connections 1 (`Active`), Customers 2, Users 6.

### F6 — Settings save: do bugs, dono fix

**Bug 1 — CS0111.** GET aur POST dono `Settings(CancellationToken)` the. C#
attributes ko signature ka hissa nahi maanta, is liye compiler unhe duplicate
samajh raha tha. Fix: POST ka method naam `SaveSettings` + `[ActionName("Settings")]`
(route wahi rehta hai, form ka `asp-action` bhi wahi).

**Bug 2 — POST 400.** `Settings(IFormCollection form, ct)` likhne se model binder
request body ko pehle kha jata tha aur `[ValidateAntiForgeryToken]` us ke baad
400 de deta. Fix: parameter hata kar seedha `Request.Form` padho.

Bool save bhi tight kiya: pehle `form.ContainsKey(key) ? "true" : "false"` tha —
yani `feature.maintenance=false` bhejne par bhi **true** ho jata. Ab value bhi
check hoti hai.

### F7 — Skills (24 install)

`everything-claude-code` ek Claude Code plugin hai — uske 9 skills mein se sirf
2 adopt hue (`security-review`, `verification-loop`); baaki React/Node-specific ya
runtime-scripts hain. `impeccable` ka native binary skip, markdown reference adopt.
`taste-skill` poora adopt. `sandbaseai/workbuddy-skill` catalog (21,818 skills)
discovery source banaya.

Tafseel: `Documents/Obsidian Vault/NEXUS/NEXUS - Skills & Tooling.md`.

### F8 — Obsidian vault

`C:\Users\Zain Ansari\Documents\Obsidian Vault\NEXUS\` — 8 notes (Hub, Architecture,
Gotchas, Setup, Progress, SRS Features, Skills & Tooling) + `Welcome.md` index.

### F5 — aaage ka kaam

- ✅ Reports: 3 doughnut chart → `_Breakdown` labelled bars (charts 6 → 3).
- ⬜ Admin Dashboard: 34 inline styles, 3 canvases — baaki.
- ⬜ Accounts / Retail / Technical dashes: inline styles (23 / 18 / 16).

**Faisalabad (City id 5)** testing se `IsServiced=0` reh gaya tha — `1` par restore
kar diya. 8/8 cities served.

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
| 5.3.5 | `ChangePassword` (POST) chalu karna | DONE |
| 5.3.6 | Accounts dashboard asli data par (`AccountsDashboardService`) | DONE |
| 5.3.7 | Technical dashboard asli data par (`TechnicalDashboardService`) | DONE |
| 5.3.8 | Retail dashboard asli data par (`RetailDashboardService`) | DONE |
| 5.3.9 | Retail search — server-side, asli DB query (`SearchService`) | DONE |
| 5.3.10 | `Retail/NewOrder` — asli form (`RegistrationService` se juda) | DONE |
| 5.3.11 | `Admin/Settings` se PKR / Fiber / Wireless hata diya | DONE |
| 5.4.1 | Advanced search (ID / naam / type / date / contact) | TODO |
| 5.5.1 | Vendor / procurement | PARTIAL — vendor register DONE, purchase order workflow TODO |
| 5.5.2 | Stock / inventory | DONE |
| 5.5.3 | Retail shop + employee management | DONE |
| 5.6.1 | Reports (connections, revenue, outstanding, bulk) | DONE |
| 5.7.1 | Customer pages asli data par | TODO |
| 5.7.2 | `IAccessGuard` ownership checks (IDOR) | TODO |

### Ab bhi mock data wale pages (honest list — 7 Oct ke baad)

| Page | Status |
|---|---|
| `Views/Admin/Reports.cshtml` | **GONE** — `Views/Reports/Index.cshtml` par asli data |
| `Views/Customer/MyConnection.cshtml` | 2 mock hits — portal service maujood hai, sirf wire karna |
| `Views/Feedback/Index.cshtml` | **DONE** — real save + real history |
| `Models/Services/Api/ApiService.cs` | **DELETED** — chaaron consumers real services par |
| `Views/Admin/Settings.cshtml` | **REWRITTEN** — live counts, koi fake demo control nahi |

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
