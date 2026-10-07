# 11 — Implementation Plan (Poora)

NEXUS Service Marketing System. Ye master plan hai — kya banana hai, kis order
mein, aur kyun. Har phase ka apna acceptance criteria hai.

Decisions jo ho gayi (5 Oct 2026):
- **Currency: USD** — SRS hi source of truth. Frontend ke PKR symbols badlenge.
- **Tax: 12.24%** — SRS ke mutabiq, ek constant mein rakhenge.
- **Equipment pricing: scope se bahar** — sirf stock quantity track karenge.
- **Approach: Code First** — DB pehle nahi banani, C# entities se EF banata hai.
- **DB: `NexusDb` on `DESKTOP-BD6OLPC\SQLEXPRESS`** (SQL Server 2022 Express).
- **EF Core 8.0.11** — pehle 9.0.20 tha magar project `net8.0` hai, mismatch theek kiya.

---

## Progress snapshot (5 Oct 2026, raat)

| Phase | Status |
|---|---|
| 4.0 Foundation | ✅ **DONE** — DbContext, `Result<T>`, constants, folders, packages |
| 4.1 Database | ✅ **DONE** — 22 tables live, migration applied, seed verified |
| 4.2 Authentication | ✅ **DONE** — cookie auth, lockout, ID generators, enumeration-safe |
| 4.3 Authorization | ✅ **DONE** — 5 roles enforced on all controllers, AJAX-aware |
| 4.4 Validation | partial — client + ModelState live, domain rules pending (Phase 5) |
| 4.5 Security | partial — CSRF, enumeration, open-redirect done; headers/CSP pending |
| 4.6 Service layer | partial — auth + generator services done, domain services pending |
| 4.7 Error handling | partial — AccessDenied page done, 404/500 pending |
| Phase 5 SRS features | pending |
| 4.8 Performance, 4.9 A11y | pending |

Live DB counts (verified via SQL, not claims):
`Tables=23 · Cities=8 · Plans=10 · PlanPrices=29 · BulkTiers=4 · Products=6 · Users=1 · Employees=1 · RetailShops=1 · Customers=0`

`Customers=0` jaan-boojh kar — fake customers seed nahi kiye. Demo mein register
kar ke banayenge.

### Verified behaviours (curl se, dawa nahi)

| Test | Result |
|---|---|
| Anonymous → protected page | `302` to `/Account/Login?ReturnUrl=…` |
| Wrong password | generic message, no clue which field was wrong |
| Non-existent account | **same** generic message (no enumeration) |
| Correct password | `302`, `NEXUS.Auth` cookie issued |
| 6th failed attempt | locked for 15 minutes |
| Admin cookie → `/Customer/Dashboard` | denied |
| Same request as AJAX | `403` status, not a redirect |
| Logout | cookie cleared, page protected again |
| `returnUrl=/Accounts/Dashboard` | honoured |
| `returnUrl=https://evil.com` | **rejected**, falls back to role home |

### Demo credentials

`admin@nexus.example` / `Nexus@2026` — seeded admin. Pehli login par password
change maangta hai, phir role ke dashboard par bhejta hai.

---

## Table of Contents

1. [Order of work](#1-order-of-work)
2. [Phase 4.0 — Foundation](#phase-40--foundation)
3. [Phase 4.1 — Database](#phase-41--database-sql-server--ef-core)
4. [Phase 4.2 — Authentication](#phase-42--authentication)
5. [Phase 4.3 — Authorization](#phase-43--authorization-5-roles)
6. [Phase 4.4 — Validation](#phase-44--validation-3-layers)
7. [Phase 4.5 — Security](#phase-45--security)
8. [Phase 4.6 — Service / API Layer](#phase-46--service--api-layer)
9. [Phase 4.7 — Error Handling](#phase-47--error-handling)
10. [Phase 5 — SRS Features](#phase-5--srs-features)
11. [Phase 4.8 — Performance](#phase-48--performance)
12. [Phase 4.9 — Accessibility](#phase-49--accessibility)
13. [Phase 6 — Aptech Deliverables](#phase-6--aptech-deliverables)
14. [Definition of Done](#definition-of-done)

---

## 1. Order of work

Order random nahi hai. Har phase pehle wale par depend karta hai — DB ke bagair
auth nahi, auth ke bagair authz nahi, service layer ke bagair SRS features nahi.

```
4.0 Foundation      DbContext + Result<T> + DI ka dhancha
        |
4.1 Database        21 tables, migrations, seed
        |
4.2 Authentication  login, hashing, cookie, lockout
        |
4.3 Authorization   5 roles, policies, IDOR guards
        |
4.4 Validation      3 layers, sab forms
        |
4.5 Security        CSRF, XSS, headers, rate limit
        |
4.6 Service Layer   IAiService -> real EF, controllers patle
        |
4.7 Error Handling  global middleware, branded pages
        |
5.x SRS Features    billing, feasibility, bulk discount, search, vendor/stock
        |
4.8 Performance     paging, AsNoTracking, indexes
4.9 Accessibility   ARIA, keyboard, contrast
        |
6.x Aptech Docs     ER, algorithms, GUI standards, report
```

**Estimated: ~4.0 se 4.7 tak ka core ~60-70% kaam hai. Phase 5 sab se bara hai
(SRS features jo bilkul nahi hain). Phase 6 sirf documentation hai.**

---

## Phase 4.0 — Foundation

Kuch bhi banane se pehle dhancha. Ye chhota phase hai magar baad mein bohot bachata hai.

### 4.0.1 Packages

| Package | Version | Kaam |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.20 | ✅ already hai |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.20 | ✅ already hai |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.20 | ✅ already hai |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 9.0.20 | **add** — password hashing + user store |
| `FluentValidation.AspNetCore` | 11.x | **add** — server-side validation, clean rules |
| `Serilog.AspNetCore` | 8.x | **add** — structured logging |
| `Serilog.Sinks.File` | 6.x | **add** — file sink |

### 4.0.2 Folder structure (naya)

```
NEXUS.Services/
├── Data/
│   ├── NexusDbContext.cs
│   ├── Configurations/        (IEntityTypeConfiguration per entity)
│   └── Seed/
├── Domain/                    (entities — abhi Models/ mein hain)
│   ├── Customers/
│   ├── Catalog/
│   ├── Orders/
│   ├── Billing/
│   └── Organisation/
├── Services/                  (business logic, interfaces)
│   ├── Interfaces/
│   │   ├── ICustomerService.cs
│   │   ├── IOrderService.cs
│   │   ├── IBillingService.cs
│   │   ├── IFeasibilityService.cs
│   │   └── ...
│   └── Implementation/
├── Common/
│   ├── Result.cs              (Result<T> + ErrorKind)
│   ├── PagedResult.cs
│   └── Constants/             (TaxConstants, IdFormats, DiscountTiers)
├── Validation/                (FluentValidation validators)
└── Controllers/               (patle — service call karte hain)
```

**Kyun:** ab `Models/Services/Api/*.cs` ek `ApiService` hai jo hardcoded data
return karta hai. Usay domain entities + real service layer se replace karna hai.
Controller ke andar business logic nahi jaayegi — warna Phase 5 mein sab tootega.

### 4.0.3 `Result<T>` — pehla building block

```csharp
public enum ErrorKind { None, NotFound, Validation, Conflict, Unauthorized, Forbidden, Failure }

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ErrorKind Kind { get; }
    public string? Message { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
```

Har service method `Result<T>` return karegi. Controller `Kind` dekh kar status
code chune ga. Isse exception-heavy code nahi likhna parega aur `docs/07` ka
contract enforce hoga.

### 4.0.4 Constants

```csharp
public static class TaxConstants      { public const decimal ServiceTaxRate = 0.1224m; }
public static class IdFormats         { /* order prefix D/T/B, account letter+city+serial */ }
public static class BulkDiscountTiers { /* 10-15 => .25, 15-25 => .50, 25-50 => .75, 50+ => 1.00 */ }
```

Ye numbers sirf ek jagah honge. UI, bill, aur test — sab yahin se padhenge.

### Acceptance (4.0)

- [ ] `dotnet build` clean
- [ ] DbContext register ho gaya DI mein (khali ho to bhi)
- [ ] `Result<T>` + `PagedResult<T>` compile hote hain
- [ ] Constants file bani, koi magic number baaki nahi
- [ ] Serilog file sink kaam kar raha hai

---

## Phase 4.1 — Database (SQL Server + EF Core)

Poora schema `docs/06-database-schema.md` mein hai. 21 tables. Yahan sirf
migrations ka order aur rationale.

### 4.1.1 Migrations ka order

Ek hi bari migration nahi — 6 chhoti, taake kuch toote to pata chale.

| # | Migration | Tables |
|---|---|---|
| M1 | `Initial_Geography` | City |
| M2 | `Identity_Org` | AppUser, Role, Employee, RetailShop |
| M3 | `Catalog_Pricing` | Plan, PlanPrice, BulkDiscountTier, EquipmentProduct, StockItem |
| M4 | `Customers_Orders` | Customer, ConnectionOrder, FeasibilityCheck, Connection, ConnectionStatusHistory |
| M5 | `Billing_Payments` | Bill, BillLine, Payment |
| M6 | `Procurement_Feedback` | Vendor, PurchaseOrder, PurchaseOrderLine, Feedback |

Har migration se pehle: `dotnet ef migrations add <Name>` →
`dotnet ef database update` → verify.

### 4.1.2 Key decisions

| Cheez | Faisla | Kyun |
|---|---|---|
| PK type | `int` identity | Simple, demo-safe, readable |
| Money columns | `decimal(18,2)` | Float kabhi nahi — paisa round off ho jata hai |
| AccountId / OrderId | `string` + unique index | Format user ko dikhta hai (`D`+10 digits) |
| Enum storage | `string` convert | DB mein `"Active"` padhne mein aasan |
| Soft delete | `IsDeleted` + global query filter | Customer record kabhi hard-delete nahi |
| Audit fields | `CreatedAt`, `UpdatedAt`, `CreatedBy` | Aptech report + debugging ke liye |
| Concurrency | `RowVersion` (`byte[]`) on Bill, Connection, StockItem | Do log ek hi bill edit na karein |
| Cascade | Restricted default; explicit cascade sirf line items par | Galti se data udna nahi chahiye |

### 4.1.3 Seed data (real, fake nahi)

| Table | Kya | Kitna |
|---|---|---|
| City | SRS ke city codes ke saath | SRS mein jo hain |
| Plan | SRS ke actual plans (USD) | Dial-Up 4 + Broadband 4 |
| PlanPrice | Rental / Half-Yearly / Yearly per plan | per SRS table |
| BulkDiscountTier | 4 slabs (25/50/75/100) | 4 rows |
| Role | Admin, Accounts, Technical, Retail, Customer | 5 rows |
| AppUser | **1 admin** — demo ke liye, password `Admin@123` (change on first login) | 1 row |
| RetailShop | 1-2 demo shops | 2 rows |

**Jhooti customers, jhoote bills, jhoote orders seed NAHI karenge.** Demo mein
register kar ke banayenge. Ye user ki hard preference hai.

### Acceptance (4.1)

- [ ] 6 migrations apply, DB bani
- [ ] Seed chal gaya, counts verify (cities, plans, tiers, roles)
- [ ] `dotnet ef database update` clean dobara chalega
- [ ] FK violation test: orphan insert fail hota hai
- [ ] Unique constraint test: duplicate AccountId fail hota hai
- [ ] Global query filter: deleted customer queries mein nahi aata

---

## Phase 4.2 — Authentication

Abhi login kuch bhi accept karta hai. Ye phase usay asli banata hai.

### 4.2.1 Approach

**ASP.NET Core Cookie Authentication + ASP.NET Identity ka password hasher.**

Poora Identity framework nahi chahiye (uska UI override karna mushkil hai).
Sirf `UserManager` ka `IPasswordHasher<T>` le kar cookies se session banayenge.
Isse SRS ke "Account ID se login" ka flow saaf rehta hai.

### 4.2.2 Login flow

```
1. User enters AccountId ya Email + password
2. AppUser lookup (AccountId == x || Email == x) && !IsDeleted
3. Fail ho to SAME generic message: "Account ID or password is incorrect"
   -> "user not found" kabhi mat batao (enumeration leak)
4. Lockout check: IsLockedOut?
5. IPasswordHasher.Verify
6. Fail -> AccessFailedCount++, kuch attempts ke baad lock
7. Pass -> ResetAccessFailed, cookie set
8. Cookie: claims = UserId, AccountId, FullName, Role, EmployeeId (agar hai)
9. Redirect: ReturnUrl agar safe hai, warna role ke hisaab se dashboard
```

### 4.2.3 Cookie settings

| Setting | Value | Kyun |
|---|---|---|
| `HttpOnly` | `true` | JS se cookie padhi na ja sake (XSS se bachao) |
| `SecurePolicy` | prod: `Always`, dev: `SameAsRequest` | HTTPS enforce |
| `SameSite` | `Lax` | CSRF cushion + normal navigation chale |
| `ExpireTimeSpan` | 60 minutes sliding | Session expiry |
| `SlidingExpiration` | `true` | Active user ko logout na kare |

### 4.2.4 Password policy

| Rule | Value |
|---|---|
| Minimum length | 8 |
| Upper + lower + digit | zaroori |
| Special char | zaroori |
| Hashing | Identity PBKDF2 (`IPasswordHasher`), **SQL mein plaintext kabhi nahi** |
| Lockout | 5 attempts → 15 min |
| First-login change | seeded admin ko force |

### 4.2.5 Account ID generation (SRS format)

SRS: **1 letter + 3-digit city code + 12-digit serial** (16 chars total).
Ye generator ek jagah hoga: `IAccountIdGenerator`.

```
letter  = connection type  (D = Dial-Up, B = Broadband, T = Telephone)
city    = City.Code (3 digits, DB se)
serial  = 12-digit zero-padded, per (type, city) sequence
```

Order ID: **`D` / `T` / `B` + 10 digits** — `IOrderIdGenerator`, same pattern.

**Race condition:** do customers ek waqt register karein to same serial na mile.
Hal: DB sequence ya `INSERT` ke andar unique index par retry, transaction ke andar.

### 4.2.6 Frontend session (abhi wala)

Abhi `site.js` `sessionStorage` use kar raha hai. Ye **demo-only** tha.
Backend auth aane par: server cookie hi source of truth hoga. `NEXUS.getUser()`
rakhenge magar wo `_Layout` mein Razor se inject hoga, sessionStorage se nahi.

### Acceptance (4.2)

- [ ] `/Admin/Dashboard` anonymous → `/Account/Login?ReturnUrl=...`
- [ ] Sahi creds → dashboard, cookie set
- [ ] Ghalat creds → generic error, "user not found" leak nahi
- [ ] 5 ghalat attempt → lockout message
- [ ] Password DB mein hash, plaintext nahi (manually check)
- [ ] Logout → cookie clear, protected page block
- [ ] ReturnUrl open-redirect safe (external URL reject)
- [ ] Account ID format SRS ke mutabiq

---

## Phase 4.3 — Authorization (5 roles)

Auth ke baad foran. Warna har dashboard khula rehta hai.

### 4.3.1 Roles

| Role | Kar sakta hai | Nahi kar sakta |
|---|---|---|
| **Admin** | Sab kuch, employees, vendors, stock, plans, reports | — |
| **Accounts** | Bills generate, payments record, reports | Order create, feasibility, stock |
| **Technical** | Apne assigned orders, feasibility check, status update | Bills, pricing, customers |
| **Retail** | Customer register, order create, payment record | Bill generate, pricing edit |
| **Customer** | Apna profile, apna order track, apni bills, feedback | Kisi ka bhi data |

### 4.3.2 Implementation

- **Policies** `Program.cs` mein register: `RequireRole` + custom handlers
- Har controller par `[Authorize(Roles = "...")]`
- Har action par specific policy jahan zaroori
- Enum `NexusRoles` (string constants) — typos se bachao

### 4.3.3 IDOR — sab se bara risk

URL mein ID guess kar ke dusre ka data na khul jaye. Ye har jagah chahiye:

```
Customer: /Orders/Tracking/42   -> order.CustomerId == currentUser.CustomerId ?
Technical: /Orders/Detail/42    -> assignment exists?
Bills: /Bills/Detail/42         -> bill.CustomerId == currentUser.CustomerId ?
```

**Rule: role check kaafi nahi. Ownership check bhi zaroori.**
Access fail -> `404` (403 se behtar — existence bhi leak nahi hoti).

Ek `IAccessGuard` helper banayenge:
```csharp
Task<Result<T>> EnsureOwnershipAsync<T>(int id, ClaimsPrincipal user);
```
Har controller isay call karega — copy-paste nahi.

### 4.3.4 UI visibility

Links role ke hisaab se hide — magar **sirf hide kaafi nahi**. Server par bhi
enforce. `_DashboardSidebar.cshtml` mein `User.IsInRole(...)` se links filter.

### Acceptance (4.3)

- [ ] Har role ka cross-check (5 × 5 matrix) — sahi jagah 403/404
- [ ] Customer doosre ka order ID daale → 404
- [ ] Technical dusre ki assigned order khole → 404
- [ ] Direct URL guess har controller par test
- [ ] Menu sirf allowed links dikhaye
- [ ] Server-side enforce confirmed (browser se curl se)

---

## Phase 4.4 — Validation (3 layers)

### 4.4.1 Teen layers

| Layer | Kahan | Kya pakadta hai | Skippable? |
|---|---|---|---|
| **1. Client** | `validation.js` + MVC attrs | Format, required, range — fast feedback | Haan (JS off) |
| **2. ModelState** | Controller/ViewModel | DataAnnotations — server par dobara | Nahi |
| **3. Domain** | Service layer | Business rules — DB ke against | Nahi |

**Rule: Layer 1 kabhi bharosa mat karo. Layer 2 har POST par. Layer 3 un
cheezon ke liye jo DB dekhe bagair pata na chalein.**

### 4.4.2 Layer 2 — ViewModels

Ab controllers domain models direct lete hain. Har form ke liye alag ViewModel:

| Form | ViewModel | Zaroori rules |
|---|---|---|
| Register | `CustomerRegisterVm` | Name, CNIC pattern, phone, address, connection type |
| Login | `LoginVm` | Required, max length |
| Order/New | `OrderCreateVm` | Connection type, plan, landline number (Dial-Up par zaroori) |
| Feasibility | `FeasibilityVm` | Address, distance, connection type |
| Payment | `PaymentRecordVm` | Amount > 0, method, date <= today |
| Bill generate | `BillGenerateVm` | Customer, period, validate ranges |
| Feedback | `FeedbackVm` | Rating 1-5, message length |
| Employee | `EmployeeVm` | Name, role, shop assignment |
| Vendor/Stock | `VendorVm`, `StockItemVm` | Name, quantity >= 0 |

**Overposting guard:** `[Bind]` nahi — alag ViewModel. User `Role` ya `Balance`
khud set na kar sake.

### 4.4.3 Layer 3 — Business rules (ye Layer 2 nahi pakad sakta)

| # | Rule | Kahan check |
|---|---|---|
| V1 | Dial-Up + landline zaroori, aur landline usi vendor ki | `OrderService.Validate` |
| V2 | Landline kisi aur customer ke naam par na ho | DB lookup |
| V3 | Plan customer ke connection type se match kare | `OrderService` |
| V4 | Feasibility PASS se pehle order create nahi | `OrderService` (state check) |
| V5 | Duplicate CNIC se doosra customer nahi | unique index + service |
| V6 | Bill period overlap na ho (same customer) | `BillingService` |
| V7 | Payment amount bill ke outstanding se zyada na ho | `BillingService` |
| V8 | Bulk discount sirf tab jab connection count tier mein aaye | `DiscountCalculator` |
| V9 | Temp Inactive connection par naya bill na bane | `BillingService` |
| V10 | Stock mein itna na ho jitna available nahi | `StockService` |

### 4.4.4 Validation message handling

- `ModelState` fail → view dobara, `.field-error` inline
- Service layer fail → `Result<T>` ka `Errors` dict → `ModelState` mein merge
- Client ko hamesha same UI mile — pata na chale validation kahan se aayi

### Acceptance (4.4)

- [ ] Har form par client + server dono test
- [ ] JS band kar ke submit → server rokta hai
- [ ] V1–V10 ka har rule ka test case (manual ya xUnit)
- [ ] Error messages user-friendly, English, koi stack trace nahi
- [ ] Overposting test: extra field bhejo → ignore ho
- [ ] Existing test checklist ka section 3 + 7 green

---

## Phase 4.5 — Security

### 4.5.1 Checklist

| # | Cheez | Kaise |
|---|---|---|
| S1 | **CSRF** | `[ValidateAntiForgeryToken]` har POST par; Razor forms mein automatic |
| S2 | **XSS** | Razor auto-encode; `Html.Raw` har instance review; user text kisi `@Html.Raw` mein nahi |
| S3 | **SQL Injection** | EF parameterized only; koi `FromSqlRaw` string concat nahi |
| S4 | **IDOR** | `IAccessGuard` (Phase 4.3) |
| S5 | **Overposting** | ViewModels (Phase 4.4) |
| S6 | **Password storage** | Identity hasher, plaintext nahi, logs mein nahi |
| S7 | **Enumeration** | Login ka generic error; register ka "email already exists" bhi generic |
| S8 | **Session fixation** | Login par cookie regenerate |
| S9 | **Rate limiting** | `AddRateLimiter` — login (5/min/IP), public search (30/min) |
| S10 | **HTTPS** | `UseHttpsRedirection` + HSTS prod mein |
| S11 | **Security headers** | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, CSP |
| S12 | **Error disclosure** | Prod mein detailed errors off; custom 500 |
| S13 | **File upload** | (agar ho) extension whitelist, size limit, random naam, `wwwroot` ke bahar store |
| S14 | **Secrets** | Connection string `appsettings.Development.json` mein ya user-secrets; repo mein plaintext prod secret nahi |
| S15 | **Logging** | Password/token/CNIC log nahi; failed logins log hon |

### 4.5.2 CSP (Content Security Policy)

Abhi Font Awesome, Chart.js, Google Fonts external se aa rahe hain. CSP lagane
se ye block ho sakte hain. Do options:

**Option A (recommended):** assets local karo, phir strict CSP:
```
default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'
```
`'unsafe-inline'` style ke liye chahiye kyunki 910 inline styles hain — Phase 4.8
mein woh hatenge to CSP aur tight karengi.

**Option B:** CDN allow karo CSP mein (`https://cdnjs.cloudflare.com` etc.).
Aasan magar offline demo tootegi aur supply-chain risk rehta hai.

**Faisla: Option A.** Assets local karne se offline high-fidelity demo bhi ho jayega.

### 4.5.3 Anti-forgery specifics

- Har `<form>` Razor se banayein (`asp-action`) → token automatic
- AJAX POST: `RequestVerificationToken` header bhejna hoga — `site.js` mein
  ek helper banayenge jo har `fetch` par token attach kare
- `[AutoValidateAntiforgeryToken]` globally + `[IgnoreAntiforgeryToken]` sirf
  jahan genuinely zaroori

### Acceptance (4.5)

- [ ] Har POST par token; token ke bagair request fail
- [ ] `Html.Raw` ka audit — 0 unsafe instance
- [ ] Rate limit test: 6th login attempt block
- [ ] Security headers response mein maujood (devtools check)
- [ ] HTTPS redirect kaam karta hai
- [ ] Prod error page stack trace nahi dikhata
- [ ] CSP violations console mein zero

---

## Phase 4.6 — Service / API Layer

`docs/07-api-contract.md` mein Option C chuna: `IApiService` server-side EF
implementation. Yahan detail.

### 4.6.1 Pattern

```
Controller  ->  I<Domain>Service  ->  DbContext
   (patla)         (business logic)     (data)
```

Controller mein: model bind, authorize, service call, result → view/redirect.
Controller mein business logic **nahi**.

### 4.6.2 Service interfaces (jo banenge)

| Interface | Methods (mukhtasar) |
|---|---|
| `ICustomerService` | Register, GetById, Update, Search, GetAccountId |
| `IOrderService` | Create, GetById, GetForCustomer, AssignTechnician, UpdateStatus |
| `IFeasibilityService` | Check, GetResult, ListPending |
| `IConnectionService` | Activate, Deactivate, GetStatus, GetHistory |
| `IBillingService` | GenerateBill, GetBill, GetOutstanding, AddLine |
| `IPaymentService` | Record, GetForBill, GetReceipt |
| `IPlanService` | List, GetById, GetPricing, UpdatePrice |
| `IDiscountService` | CalculateBulkDiscount, GetTierForCount |
| `IInventoryService` | GetStock, Allocate, Adjust |
| `IVendorService` | List, Create, Update, GetPurchaseOrders |
| `IRetailShopService` | List, GetById, GetStaff |
| `IEmployeeService` | List, Create, AssignShop, AssignRole |
| `IReportService` | Connections, Revenue, Outstanding, Bulk |
| `ISearchService` | GlobalSearch (ID/naam/type/date/contact) |
| `IFeedbackService` | Submit, List, GetByCustomer |
| `IAccountIdGenerator` | NextConnectionId, NextOrderId |
| `IAccessGuard` | EnsureOwnership, CanAccess |

### 4.6.3 Har service method ka shape

```csharp
Task<Result<CustomerDto>> RegisterAsync(CustomerRegisterVm vm, CancellationToken ct);
Task<Result<PagedResult<OrderListItemDto>>> SearchAsync(OrderSearchFilter f, CancellationToken ct);
```

- `CancellationToken` har jagah (Phase 4.8 ke liye)
- DTO return, entity nahi — warna lazy-loading aur over-fetching
- `async` sab — sync-over-async nahi

### Acceptance (4.6)

- [ ] Hardcoded `ApiService` hata diya
- [ ] Har controller service use kar raha hai, DbContext direct nahi
- [ ] Koi controller method 30 line se bara nahi
- [ ] Har service method `Result<T>` return karta hai
- [ ] DTOs entities se alag hain
- [ ] `CancellationToken` propagate ho raha hai

---

## Phase 4.7 — Error Handling

### 4.7.1 Layers

| Layer | Kya |
|---|---|
| **Global middleware** | `UseExceptionHandler` + custom `IExceptionHandler` — log + friendly page |
| **Service layer** | Expected failures `Result<T>` se — exception nahi |
| **Controller** | `Result.Kind` → `NotFound()`, `Forbid()`, redirect, ya view |
| **UI** | Branded 400/403/404/500 pages, validation inline, toast for transient |
| **AJAX** | JSON error envelope, `site.js` toast dikhaye |

### 4.7.2 Branded error pages

Abhi default developer pages hain. Banane hain: `/Views/Shared/Error.cshtml`
(500), `404.cshtml`, `403.cshtml`, `400.cshtml` — sab `_Layout` ke saath, navbar
wapas jaane ka link. Prod mein stack trace **kabhi nahi**.

### 4.7.3 Empty states

Har list/table ke liye empty state: "No records found" + action button.
Blank table dikhana bura UX hai aur demo mein "kuch nahi hai" lagta hai.

### 4.7.4 Logging

Serilog se structured logging:
- Info: har login success, order create, bill generate
- Warning: failed login, lockout, forbidden access
- Error: unhandled exception (poora stacktrace **file mein**, user ko nahi)
- **Nahi log karna:** password, CNIC number, card data, tokens

### Acceptance (4.7)

- [ ] 404/403/500 pages branded, navbar ke saath
- [ ] Prod mein stack trace leak nahi
- [ ] Har expected failure friendly message deta hai
- [ ] AJAX failure toast dikhata hai, silent fail nahi
- [ ] Empty state har list par
- [ ] Log file ban rahi hai, sensitive data nahi

---

## Phase 5 — SRS Features

**Ye sab se bara phase hai. SRS ka ~64% yahan hai.**

SRS ka requirements matrix `docs/03-requirements-matrix.md` mein hai. Yahan
implementation order aur asli logic.

### 5.1 Billing engine (Sab se pehle)

Kyun pehle: ye core value hai, aur baaki cheezein ispar depend karti hain.

**Bill calculation:**
```
lines       = subscription rental + call charges + equipment + adjustments
subtotal    = sum(lines)
discount    = bulk discount (agar corporate/bulk customer)
taxable     = subtotal - discount
serviceTax  = taxable * 12.24%
total       = taxable + serviceTax
```

- `Bill` + `BillLine` (har line ka description + amount)
- Sirf **Accounts** dept generate kare
- Bill number format decide karna (SRS mein nahi — propose: `INV-YYYY-NNNNN`)
- `BulkDiscountTier` se configured slabs use karo — hardcode nahi

**Call charges (landline):** SRS mein per-minute rates hain (local 55c/75c/70c/60c,
STD 2.25$/2.00$/1.75$). Ye `CallRate` table ya constants mein.

### 5.2 Feasibility workflow

```
Order create -> feasibility PENDING
     |
Feasibility check (distance from server, area availability)
     |
PASS -> order CONFIRMED -> technical assign
FAIL -> customer ko reason -> order REJECTED
```

- Dial-Up: distance + server availability
- Broadband: distance + area coverage
- Dial-Up + landline ho to **dono** ka check
- `FeasibilityCheck` table: result, checked by, date, notes

### 5.3 Connection lifecycle

SRS: **Active / Temporarily Inactive / Permanently Inactive**

- Har transition `ConnectionStatusHistory` mein record
- Temp Inactive par bill generate nahi (rule V9)
- Perm Inactive par connection wapas nahi
- Reconnection ka flow (agar SRS chahata hai) — reader se confirm

### 5.4 Bulk / Corporate discount

| Connections | Discount |
|---|---|
| 10–15 | 25% |
| 15–25 | 50% |
| 25–50 | 75% |
| 50+ | 100% |

`IDiscountService.CalculateBulkDiscount(customerId)` — count DB se, phir tier
lookup. Bill par applied discount line dikhaye.

### 5.5 Advanced search

SRS: **ID / naam / type / date / contact number** se search.

Ek `ISearchService` — sab entities across (Customer, Order, Connection, Bill).
Result typed groups mein. Server-side paging. Public search par rate limit.

### 5.6 Catalog — SRS ke asli plans

Abhi PKR plans (BASIC/STANDARD/PREMIUM) SRS se match nahi karte. Replace karo:

| Type | Plans | Pricing (USD) |
|---|---|---|
| Dial-Up | 10h (50), 30h (130), 60h (260) | Unlimited 28K ($75/mo), 56K ($100/mo) |
| Broadband | 30h (175), 60h (315) | Unlimited 64K ($225/mo), 128K ($350/mo) |
| Telephone | Landline rental + call rates | SRS table |

Security Deposit: Dial-Up $325, Broadband $500, Landline $250.

**Frontend ke saare `Rs.`/PKR symbols hatane hain** — Phase 5.6 mein.

### 5.7 Vendor & Procurement

- `Vendor` — supplier info
- `PurchaseOrder` + `PurchaseOrderLine` — vendor se equipment mangwana
- Admin only

### 5.8 Stock / Inventory

- `EquipmentProduct` — modem, router, cable types
- `StockItem` — per retail shop quantity
- `StockMovement` — allocation/receipt/adjustment history
- `RowVersion` for concurrency (do log same stock edit karein)
- Low-stock alert (threshold)

### 5.9 Retail shop management

- `RetailShop` — location, city, contact
- Staff assignment (`Employee.ShopId`)
- Per-shop stock view
- Retail role: apni shop ka dashboard

### 5.10 Employee management

- `Employee` — name, role, shop, contact, join date
- Admin: create, role assign, deactivate
- Har employee ka `AppUser` link

### Acceptance (Phase 5)

- [ ] Requirements matrix ke FR-01..FR-132 mein se 0 MISSING
- [ ] 0 FAKE (koi hardcoded value nahi)
- [ ] Billing calculation hand-verified (tax, discount, total)
- [ ] Feasibility ka dono route test
- [ ] Bulk discount ke 4 slabs test
- [ ] Search ke 5 criteria test
- [ ] SRS ke plans DB mein, frontend par USD

---

## Phase 4.8 — Performance

Bilkul aakhir mein (pehle correct, phir fast).

| Cheez | Kaise |
|---|---|
| **Paging** | Har list server-side. `PagedResult<T>`, default 20 per page |
| **AsNoTracking** | Read-only queries par lazmi |
| **Async** | Sab DB calls async, `CancellationToken` |
| **Indexes** | FK columns, search columns (AccountId, OrderId, CNIC, Phone, Date) |
| **N+1** | `.Include()` ya projection — loop ke andar query nahi |
| **Projection** | `Select()` se sirf zaroori columns |
| **Caching** | Plans/Cities/bulk tiers `IMemoryCache` (rarely change) |
| **Static assets** | Local, versioned (`asp-append-version`), cache headers |
| **Inline styles** | 910 → CSS classes (CSP bhi tight hogi) |
| **Images** | Lazy load, size optimise |
| **Minify** | Prod build mein CSS/JS minify |

### Acceptance (4.8)

- [ ] Har list paged
- [ ] `AsNoTracking` read queries par
- [ ] Query log mein N+1 nahi
- [ ] Indexes add, execution plans sane
- [ ] Dashboard local par 2s se kam
- [ ] 0 inline styles views mein

---

## Phase 4.9 — Accessibility

| # | Cheez |
|---|---|
| A1 | Har interactive element keyboard se reachable |
| A2 | Focus ring saathe (kabhi `outline: none` akele nahi) |
| A3 | Har image ka `alt`; decorative par `aria-hidden="true"` |
| A4 | Har input ka `<label>` |
| A5 | Validation `aria-live`/`aria-describedby` se announce |
| A6 | Contrast WCAG AA (4.5:1) — tokens se verify |
| A7 | Tables mein `<th scope>` |
| A8 | Skip-to-content link |
| A9 | Headings logical order (h1 → h6) |
| A10 | Icon-only buttons par `aria-label` |
| A11 | Modal focus trap + Esc close + focus wapas |
| A12 | Reduced motion respect (`prefers-reduced-motion`) |

### Acceptance (4.9)

- [ ] Keyboard-only walkthrough — poora flow ho jaye
- [ ] axe DevTools scan — 0 critical/serious issue
- [ ] Contrast check har token par
- [ ] Testing checklist ka section 12 green

---

## Phase 6 — Aptech Deliverables

SRS mein required documents. Code ke saath ye bhi banane hain.

| # | Deliverable | Source |
|---|---|---|
| 6.1 | **ER Diagram** | `docs/06-database-schema.md` se banao |
| 6.2 | **Algorithms** | Billing calc, feasibility, ID generation, discount, search |
| 6.3 | **GUI Standards Document** | Tokens (`site.css`), components, spacing, typography |
| 6.4 | **Interface Design Document** | Screens + flows (register → order → feasibility → bill) |
| 6.5 | **Unit Testing Check List** | `docs/09-testing-checklist.md` se formal doc |
| 6.6 | **Project Report + Synopsis** | Full write-up |
| 6.7 | **Status emails (2)** | Aptech format, `STATUS:` subject |

Aptech ka documentation checklist (`docs/02-srs.md` mein) follow karna hai.

---

## Definition of Done

Koi bhi phase "done" nahi hai jab tak:

1. `dotnet build` → **0 errors**
2. Relevant testing checklist section **green**
3. Koi hardcoded/fake value **nahi**
4. Koi business logic controller mein **nahi** (service mein hai)
5. Validation **teeno layers** mein
6. Authz **ownership** ke saath (sirf role nahi)
7. Error path **tested** (khali, ghalat, unauthorized)
8. `TASK-LEDGER.md` **update**
9. `docs/10-changelog.md` mein entry
10. Code **human-written** lage — no emoji, no filler comments, no AI tells

---

## Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Phase 5 bohot bara hai | Deadline miss | 5.1 (billing) pehle — highest value |
| Team ke saath merge conflicts | Kaam zaya | Feature branches, chhote commits |
| SRS ke aur ambiguities | Rework | Har phase se pehle SRS dobara padho |
| Team "frontend kafi hai" samajhta hai | Documentation missing | Requirements matrix dikhao (`docs/03`) |
| Sandbox build block | Slow iteration | `dangerouslyDisableSandbox` use karo |

---

## Agla qadam

Phase 4.0 (Foundation) shuru — DbContext, `Result<T>`, constants, folder structure.
Ye chhota hai (~1-2 ghante ka kaam) aur baaki sab ispar khara hai.

Uske baad 4.1 (Database) — 21 tables, 6 migrations, seed.
