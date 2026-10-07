# NEXUS — Full System Audit

**Date:** 7 October 2026
**Scope:** Har page, har button, har link, har form, har filter — kuch bhi miss nahi.
**Purpose:** Decide karna ke aage kaam kis order mein ho.

Legend:

- **REAL** — DB se asli data, kaam karta hai
- **STATIC** — sirf UI bana hai, hardcoded data, DB se koi taluq nahi
- **BROKEN** — UI hai lekin click/submit pe kuch nahi hota ya galat hota hai
- **MISSING** — feature hi nahi hai
- **NAV-BUG** — navigation/sidebar ka masla

---

## 1. System mein kitne portals hain

**Chhe (6) areas**, chaar roles:

| # | Portal | Role(s) | Kaam |
|---|---|---|---|
| 1 | **Public site** | Guest | Marketing, plans, signup, tracking, status |
| 2 | **Customer portal** | Customer | Apna connection, bills, feedback, profile |
| 3 | **Retail counter** | Retail + Admin | Walk-in signup, customer search, branch dashboard |
| 4 | **Technical** | Technical + Admin | Install queue, lines, network state |
| 5 | **Accounts** | Accounts + Admin | Ledger, payments, collections |
| 6 | **Admin** | Admin | Sab consoles ka control + system config |

**Admin har console tak pahunch sakta hai** — role string mein `+ "," + NexusRoles.Admin` sab jagah hai. Ye sahi hai.

---

## 2. PORTAL 1 — Public site

| Page | Route | Status | Detail |
|---|---|---|---|
| Home | `/` | REAL | Featured plans DB se (`HomeController` → `PlanCatalogService.FeatureAsync`) |
| Plans catalogue | `/Plans` | REAL | DB se, type filter query string se |
| Plan detail | `/Plans/Details/{id}` | REAL | DB se + related plans |
| Services | `/Services` | **STATIC** | `ServicesController.Index()` koi service inject nahi karta (`async` hai lekin `await` nahi). Poora page hardcoded |
| About | `/Home/About` | **STATIC** | Pure static page, koi data nahi |
| Contact | `/Home/Contact` | **BROKEN** | Form hai lekin `data-save-demo="Contact"` — submit pe fake toast, DB mein kuch nahi |
| New Connection | `/Orders/New` | REAL | `RegistrationService` — customer + order dono DB mein |
| Order tracking | `/Orders/Tracking?id=` | **PARTIAL** | `ApiService` (mock file) use karta hai — `Rs.` + `NX12345678` + `Ahmed Khan` hardcoded |
| Account status | `/Account/Status` | **PARTIAL** | Page chalta hai (200, 33KB) — lekin POST ka behaviour (accountId/phone/cnic) verify karna baaki |
| Error | `/Home/Error` | STATIC | Generic error page |
| Privacy | `/Home/Privacy` | **BROKEN (404)** | Link do signup forms mein hai lekin **action maujood nahi** |

**Public ke masle:**
- `ServicesController` — service inject hi nahi karta, poora page hardcoded
- `Orders/Tracking` — `ApiService` mock par chalta hai
- `Account/Status` — service check karni hai

---

## 3. PORTAL 2 — Customer

| Page | Route | Status | Detail |
|---|---|---|---|
| Dashboard | `/Customer/Dashboard` | REAL | `CustomerPortalService` — real account, bills, usage |
| My Connection | `/Customer/MyConnection` | **PARTIAL** | Portal service maujood hai lekin view mein abhi bhi 2 mock hits |
| Bills & Payments | `/Bills` | REAL | `BillingService` |
| Bill detail | `/Bills/Details/{id}` | REAL | Real invoice + IDOR ownership check |
| Profile | `/Profile` | REAL | `ProfileService`, role-aware |
| Feedback | `/Feedback` | **PARTIAL** | Controller POST hai, lekin submit hone ke baad kya? Aur list dikhti hai? |
| Track Order | `/Orders/Tracking` | **PARTIAL** | `ApiService` mock |
| Account Status | `/Account/Status` | **dekhna baaki** | |

**Customer ke masle:**
- `MyConnection` mein 2 mock hits bache hue
- `Feedback` — POST ka behaviour confirm karna hai
- `Tracking` mock service par

---

## 4. PORTAL 3 — Retail counter

| Page | Route | Status | Detail |
|---|---|---|---|
| Dashboard | `/Retail/Dashboard` | REAL | `RetailDashboardService` |
| New Order | `/Retail/NewOrder` | REAL | `RegistrationService` — asli POST, DB mein likhta hai |
| Customer Search | `/Retail/Search` | REAL | `SearchService` — server-side, 4 tabs |

**Retail ke masle:**
- `Retail/Dashboard` mein 1 mock hit baaki (wo `Bilal Ahmed Khan` asli customer hai — false positive)
- **Sidebar mein `/Plans` link hai lekin `/Retail/*` ke liye koi plans screen nahi** — Retail plans dekh nahi sakta (sirf admin `/Plans` dekhta hai)
- **Admin sidebar mein Retail Sales ka `IsActive("retail")` key hai lekin `RetailController` sirf `dashboard`/`neworder`/`search` set karta hai** — NAV-BUG

---

## 5. PORTAL 4 — Technical

| Page | Route | Status | Detail |
|---|---|---|---|
| Dashboard | `/Technical/Dashboard` | REAL | `TechnicalDashboardService` |
| Installations | `/Orders/Tracking` | **PARTIAL** | `ApiService` mock — technical ke liye ye galat jagah jata hai |
| Survey Queue | `/Operations` | REAL | `FeasibilityService` |
| Survey review | `/Operations/Review/{id}` | REAL | Real order detail + actions |
| Connections | `/Operations/Connections` | REAL | Real register + totals |
| Connection detail | `/Operations/Connection/{id}` | REAL | Real history + status change |

**Technical ke masle:**
- **Sidebar: `/Technical/Dashboard` aur `/Operations` dono ka active key `dashboard`... nahi, `dashboard` vs `operations`** — lekin `Technical/Dashboard` link `/Orders/Tracking` pe jata hai jo galat hai
- `/Orders/Tracking` mock
- **Technical sidebar mein `/Feedback` link hai — technical ko customer feedback dikhna chahiye? Question.**

---

## 6. PORTAL 5 — Accounts

| Page | Route | Status | Detail |
|---|---|---|---|
| Dashboard | `/Accounts/Dashboard` | REAL | `AccountsDashboardService` |
| Bills | `/Bills` | REAL | `BillingService` |
| Invoices | `/Bills/Details` | **BROKEN** | Sidebar link `/Bills/Details` — **ye `/Bills/Details` pe jata hai lekin id chahiye!** Ye 404 dega |
| Record Payment | POST `/Bills/RecordPayment` | REAL | Real payment + receipt |
| Customer Dues | `/Retail/Search` | REAL | Search service |
| Reports | `/Admin/Reports` | **STATIC** | Poora mock — 20 hits |

**Accounts ke masle:**
- **Sidebar mein `/Bills/Details` link bina id ke** — BROKEN, 404
- `/Admin/Reports` mock — **aur ye link Accounts, Technical, Retail, Admin — chaar sidebars mein hai**

---

## 7. PORTAL 6 — Admin

| Page | Route | Status | Detail |
|---|---|---|---|
| Dashboard | `/Admin/Dashboard` | REAL | `AdminDashboardService` |
| Reports | `/Admin/Reports` | **STATIC** | Poora mock (557 lines, 20 mock hits) |
| Settings | `/Admin/Settings` | **STATIC** | Form hai, save nahi hota. Koi settings service nahi |
| Plans | `/Plans` | **BROKEN** | Sidebar se khulta hai lekin **public** plans page — admin ke liye koi edit nahi |
| Survey Queue | `/Operations` | REAL | |
| Connections | `/Operations/Connections` | REAL | |
| Customers | `/Retail/Search` | REAL | |
| Retail Sales | `/Retail/Dashboard` | REAL | |
| Orders | `/Orders/Tracking` | **PARTIAL** | Mock service |
| Installations | `/Technical/Dashboard` | REAL | |
| Billing | `/Accounts/Dashboard` | REAL | |
| My Profile | `/Profile` | REAL | |

**Admin ke masle (sab se zyada):**
- **Plans CRUD bilkul nahi** — aap ne khud bola. `/Plans` public page hai, edit/create/delete kuch nahi
- **Settings save nahi hoti** — form ka koi backend nahi
- **Reports pura mock**
- **Admin ke paas in cheezon ka koi control nahi** (SRS ke mutabiq hona chahiye):
  - Cities manage karna
  - Discount tiers manage karna
  - Retail shops manage karna
  - Employees manage karna
  - Vendors / procurement
  - Stock / inventory
  - Customers list (admin view, edit)
  - User accounts / roles
  - Audit log

---

## 8. NAV-BUGS (jo aap ne screenshot mein dekha)

### N1 — Double active sidebar — **ASAL WAJAH MIL GAYI**

`wwwroot/js/navigation.js` line 120-129 (`initActiveLink`) **khud** `active`
class lagata hai, `path.startsWith(href)` se. Server ka `_Sidebar.cshtml`
`IsActive()` already sahi kaam kar raha hai — JS usay **override** kar deta hai.

**Kyun double hota hai:** `/Operations/Connections` pe `path = "/operations/connections"`.

| Link | Check | Nateeja |
|---|---|---|
| `/Operations` | `path.startsWith("/operations")` | **TRUE → active** (galat) |
| `/Operations/Connections` | `path === href` | **TRUE → active** (sahi) |

Dono active. Ye bug **har nested route pe** hoga.

**Fix:** `initActiveLink()` ko sidebar ke liye band kar do (server already karta
hai), ya sirf public `.nav-links` ke liye chalao.

**Ye ek bug 3 jagah lagta hai:** `/Operations` vs `/Operations/Connections`,
`/Bills` vs `/Bills/Details`, `/Retail/Dashboard` vs `/Retail/Search`.

### N2 — Accounts sidebar `/Bills/Details` bina id

Sidebar link `/Bills/Details` pe jata hai, lekin action signature
`Details(int id)` hai. **Id ke bina 404 dega.**

### N3 — Admin sidebar `Retail Sales` `IsActive("retail")`

`RetailController.Dashboard` `"dashboard"` set karta hai, `"retail"` nahi.
Sidebar mein `IsActive("retail")` hai → kabhi active nahi hota.

### N4 — `Customers` link ke do jagah alag key

| Sidebar | Link | Key |
|---|---|---|
| Admin | `/Retail/Search` | `IsActive("customers")` |
| Retail | `/Retail/Search` | (koi key nahi) |
| Technical | `/Retail/Search` | (koi key nahi) |

`RetailController.Search` `"search"` set karta hai → **teenon mein kabhi active
nahi hota**.

### N5 — Technical sidebar `Installations` → `/Orders/Tracking`

`/Orders/Tracking` customer ka tracking tool hai (aur wo bhi mock). Technical
ko `/Operations/Connections` pe jana chahiye.

### N6 — Admin sidebar `Orders` → `/Orders/Tracking`

Id ke bina ye search page hai, orders list nahi.

### N7 — `/Operations/Review/{id}` pe `SetActiveItem` controller se nahi hota

`OperationsController.Index` aur `Review` — dono mein `ViewData.SetActiveItem()`
call **nahi** hai. View mein `ViewData["ActiveSidebar"]` set hai, to chal jata
hai — lekin ye nazuk hai aur JS bug ke sath mil kar double-active banata hai.

### N8 — `/Home/Privacy` **404 hai**

Link do jagah use hota hai:
- `Views/Orders/New.cshtml:328-329` — "Terms of Service" + "Privacy Policy"
- `Views/Retail/NewOrder.cshtml:306-307`

Action **maujood hi nahi** `HomeController` mein. Dono signup forms se tootay
hue link ja rahe hain.

### N9 — Contact form submit pe kuch nahi hota

`Views/Home/Contact.cshtml:155` — `<form data-save-demo="Contact">`. `data-save-demo`
matlab JS sirf fake toast dikhata hai. **Koi message DB mein nahi jata, koi
email nahi jata.**

### N10 — `ServicesController` koi service inject nahi karta

`public async Task<IActionResult> Index()` — `async` hai lekin `await` kuch nahi.
Poora `/Services` page hardcoded hai.

---

## 9. Missing features (SRS ke mutabiq)

| # | Feature | SRS ref | Status |
|---|---|---|---|
| M1 | Plans CRUD (admin) | Core | **MISSING** |
| M2 | Cities CRUD | Core | **MISSING** |
| M3 | Discount tiers CRUD | Bulk discount | **MISSING** |
| M4 | Retail shops CRUD | Retail | **MISSING** |
| M5 | Employees CRUD | HR | **MISSING** |
| M6 | Vendors / procurement | Phase 5.5 | **MISSING** |
| M7 | Stock / inventory | Phase 5.5 | **MISSING** |
| M8 | Admin customers list + edit | Core | **MISSING** |
| M9 | User accounts management | Core | **MISSING** |
| M10 | Reports (asli) | Phase 5.6 | **STATIC** |
| M11 | Settings persistence | Core | **STATIC** |
| M12 | ChangePassword (POST) | Phase 5.3.5 | **DISABLED** |
| M13 | Contact form submit | Core | **BROKEN** — `data-save-demo` |
| M14 | Feedback list / management | Core | **PARTIAL** |
| M15 | Audit log | Core | **MISSING** |
| M16 | Advanced search filters | Phase 5.4 | **PARTIAL** |
| M17 | `IAccessGuard` IDOR checks | Phase 5.7 | **PARTIAL** |
| M18 | Privacy / Terms page | Core | **404** |
| M19 | `/Services` real content | Core | **STATIC** |
| M20 | Admin order list (asli) | Core | **MISSING** — sirf tracking |

---

## 10. Design issues

| # | Issue | Kahan |
|---|---|---|
| D1 | Admin pages "boring" — stat cards sab flat, hierarchy kam | `/Admin/*`, `/Operations/*` |
| D2 | Connection detail page design kamzor | `/Operations/Connection/{id}` |
| D3 | Reports page pare mock hone ke sath design bhi purana | `/Admin/Reports` |
| D4 | `Admin/Settings` form ka koi save feedback nahi | `/Admin/Settings` |
| D5 | 910 inline styles poore project mein | Sab views |
| D6 | 22 `!important` | CSS files |

---

## 11. Recommended order (mera mashwara)

**Phase A — Admin CRUD backbone** *(sab se pehle, kyunke sab is pe depend karta hai)*
1. A1 — Reusable CRUD pattern (list → edit → save → redirect → toast)
2. A2 — Plans CRUD (aap ka diya hua example)
3. A3 — Cities CRUD
4. A4 — Discount tiers CRUD

**Phase B — Nav fixes** *(chhota, par sab jagah nazar aata hai)*
5. B1 — N1 (double active) + N7
6. B2 — N2 (Bills/Details 404)
7. B3 — N3, N4, N5, N6 (key/link alignment)

**Phase C — Shared core**
8. C1 — ChangePassword (POST) enable
9. C2 — Settings persistence
10. C3 — `ApiService` (mock) hatao, Orders/Tracking real karo

**Phase D — Panel-wise audit + fix**
11. Public → Customer → Retail → Technical → Accounts → Admin (Reports last)

**Phase E — Missing features**
12. Shops, Employees, Vendors, Stock, User management

**Phase F — Design pass**
13. Admin pages ko premium look

---

## 12. Numbers

| | Count |
|---|---|
| Portals | **6** |
| Views | **36** |
| Controllers | **14** |
| Actions | **48** |
| Services | **14** |
| Pages REAL | **23** |
| Pages STATIC | **5** |
| Pages BROKEN | **6** |
| Pages MISSING (feature nahi) | **17** |
| NAV-BUGS | **10** |
| Missing features (SRS) | **20** |
