# 12 — Feature Status (Honest)

Last updated: 6 Oct 2026. Ye file sach bolti hai — jo hua aur jo nahi hua, dono.

---

## Ek nazar mein

| Layer | Kitna ho gaya |
|---|---|
| **Foundation (infra)** | 90% — DB, auth, authz, Result pattern, logging |
| **Business features (SRS)** | **22%** — registration + order + billing ab asli hain |
| **Frontend (UI)** | 89% — apply form aur bills asli data par, USD pricing |
| **Aptech documentation** | 15% — internal notes hain, deliverable docs nahi |

**Sab se bara sach:** infrastructure tayyar hai, magar **SRS ke features ~90% aba**
**nahi hain.** DB mein 22 tables hain — magar unmein se sirf 8 mein data hai, aur
wo bhi reference data (cities, plans, products). Kisi asli feature ka code nahi likha.

---

## Kya ADD ho gaya (Phase 4 ke baad)

### Infrastructure — ye asli, chalta hua code hai

| # | Cheez | Status | Kahan |
|---|---|---|---|
| 1 | SQL Server database `NexusDb` | ✅ live | 22 tables |
| 2 | EF Core Code First + migration | ✅ | `Data/Migrations/` |
| 3 | 22 domain entities | ✅ | `Domain/` |
| 4 | DbContext + audit stamping + concurrency | ✅ | `Data/NexusDbContext.cs` |
| 5 | Entity configurations (indexes, precision, conversions) | ✅ | `Data/Configurations/` |
| 6 | Seed data (cities, plans, tiers, products, admin, shop) | ✅ | `Data/Seed/` |
| 7 | `Result<T>` + `ErrorKind` envelope | ✅ | `Common/Result.cs` |
| 8 | `PagedResult<T>` + `PageRequest` | ✅ | `Common/PagedResult.cs` |
| 9 | Constants (tax, ID formats, bulk tiers, roles, enums) | ✅ | `Common/Constants/` |
| 10 | **Authentication** — cookie, lockout, enumeration-safe | ✅ tested | `Services/Authentication/` |
| 11 | **Authorization** — 5 roles, AJAX-aware | ✅ tested | sab controllers |
| 12 | ID generators (AccountId/OrderId/Bill/Receipt) — race-safe | ✅ | `Services/Generation/` |
| 13 | Password hashing (Identity PBKDF2) | ✅ | `Services/Security/` |
| 14 | Serilog logging (console + rolling file) | ✅ | `Logs/` |
| 15 | Login / Logout / ChangePassword / AccessDenied pages | ✅ | `Views/Account/` |
| 16 | Open-redirect protection | ✅ tested | `AccountController` |
| 17 | User account menu + role pill | ✅ | `_DashboardTopbar.cshtml` |

### Phase 5 — pehla asli feature

| # | Cheez | Status | Kahan |
|---|---|---|---|
| 23 | **Customer registration** — poora flow, asli DB writes | ✅ tested | `RegistrationService` |
| 24 | Account ID auto-generate (type + city + serial) | ✅ verified | `B041000000000001` |
| 25 | Order auto-create on signup (AwaitingFeasibility) | ✅ verified | `ConnectionOrders` |
| 26 | Duplicate CNIC / phone / email block | ✅ tested | service |
| 27 | **Rule V1** — Dial-Up/Telephone needs existing landline | ✅ tested | service |
| 28 | Plan filtered by connection type | ✅ tested | server + UI |
| 29 | 4-step combined apply form (asli cities + plans from DB) | ✅ | `Views/Orders/New.cshtml` |
| 30 | Live plan preview (monthly / yearly / deposit) | ✅ | `new-connection.js` |
| 31 | Password strength meter | ✅ | `new-connection.js` |
| 32 | Auto-signin after signup | ✅ | `OrdersController` |
| 33 | Register + New Connection ek hi flow mein merge | ✅ verified | `/Account/Register` → 302 `/Orders/New` |
| 34 | POST end-to-end (account + order + signin, teen tables linked) | ✅ verified | curl + sqlcmd |
| 35 | **Billing engine** — lines, discount, 12.24% tax, totals | ✅ 46 checks | `BillCalculator` |
| 36 | Monthly billing run, per-period idempotent | ✅ 15 checks | `BillingService` |
| 37 | Bill numbers `INV-2026-00001` (race-safe) | ✅ verified | `AccountIdGenerator` |
| 38 | Payment recording + receipt `RCP-202610-00003` | ✅ verified | `BillingService` |
| 39 | Status transitions Draft→Issued→PartiallyPaid→Paid | ✅ verified | service |
| 40 | Bulk discount tiers applied to the bill | ✅ verified | `BulkDiscountTiers` |
| 41 | Ledger + invoice screens on real data | ✅ HTTP 200 | `Views/Bills/` |
| 42 | CSRF on the payment POST | ✅ 400 without token | `[ValidateAntiForgeryToken]` |
| 43 | IDOR — customer cannot read another's bill | ✅ 302 AccessDenied | `BillsController` |

### Frontend (pehle ke session mein)

| # | Cheez | Status |
|---|---|---|
| 18 | Broken fixes (validation.js, getUser crash, CSS order) | ✅ |
| 19 | `home.css` 150 hardcoded colors → design tokens | ✅ |
| 20 | Fake stats / testimonials / ratings hataye | ✅ |
| 21 | 55 emoji hataye, 18 local images (offline demo) | ✅ |
| 22 | Navbar search bar hataya (user preference) | ✅ |
| 44 | Navbar button "Register" → **"New Connection"** | ✅ |
| 45 | Home / Login entry copy "Get New Connection" (plug icon) | ✅ |
| 46 | Plan **auto-select** — pehla visible plan khud select hota hai | ✅ browser-verified |
| 47 | Selected plan ka **visual** — border + fill + corner tick | ✅ browser-verified |
| 48 | `.selected` class JS se paint hoti hai (radio hidden hai) | ✅ browser-verified |
| 49 | Bogus `content: ' Mbps'` hataya (`orders.css` + `forms.css`) | ✅ |
| 50 | Telephone line ka badge `" hours"` → `"Telephone"` | ✅ render-verified |
| 51 | **Dial-Up plan filter bug** — type token mismatch (`DialUp` vs `Dial-Up`) | ✅ browser-verified |
| 52 | **Currency Rs. → $** — 73 occurrences, 14 files | ✅ verified 0 left |
| 53 | Fake counters (`Rs. 258,000`) → real USD scale (`$928`) | ✅ |
| 54 | **Plans page asli DB data par** (`IApiService` mock hata) | ✅ 10 plans |
| 55 | Plans filter (All / Broadband / Dial-Up / Telephone) | ✅ browser-verified |
| 56 | Home featured plans asli data se (fake 5/15/30 Mbps hataye) | ✅ |
| 57 | Comparison table asli plans se (hardcoded Basic/Standard/Premium hataye) | ✅ |
| 58 | **Order summary live update** — panel form ke bahar tha, JS use dhundh hi nahi sakta tha | ✅ browser-verified |
| 59 | **Cycle picker form ke andar** — bahar tha, yani `BillingCycle` POST hi nahi hota tha | ✅ verified |
| 60 | `modal.js` missing tha (har page par 404) | ✅ |
| 61 | **Phase 5.3 — survey queue** (`/Operations`), stats + filter | ✅ verify |
| 62 | **Order review** (`/Operations/Review/{id}`) — survey + action flow | ✅ verify |
| 63 | **Feasibility rules** — faisla officer nahi, rules karte hain | ✅ verified |
| 64 | Pass path: 2.3 km + capacity → `FeasibilityPassed` | ✅ verify |
| 65 | Fail path: 7.5 km → `FeasibilityFailed` (reason service ne likhi) | ✅ verify |
| 66 | Confirm → Start → Complete (state guards) | ✅ verify |
| 67 | **Complete se `Connection` bane** (`CON-B-000001`, status `Pending`) | ✅ verify |
| 68 | `ConnectionStatusHistory` likhi jaye (transaction ke andar) | ✅ verify |
| 69 | **Connections register** (`/Operations/Connections`) + search/filter | ✅ verify |
| 70 | **Legal status moves** (`IsLegalMove`) — UI sirf valid dikhaye | ✅ verified |
| 71 | Pending → Active par `ActivatedOn` stamp + billable (rule V9) | ✅ verified |
| 72 | **Navbar auth-aware** — signed-in par Dashboard + Logout | ✅ verified |
| 73 | Guest navbar wesa hi (Login + New Connection) | ✅ verified |
| 74 | **Sidebar logout POST form** (5 links `/Account/Login` pe ja rahe the) | ✅ verified |
| 75 | **Register header counts** — DB-level, filter ignore | ✅ verified |

---

## Kya BAKI hai — SRS features (ye asli kaam hai)

`docs/03-requirements-matrix.md` ke mutabiq **FR-01 se FR-132** mein se
**~64% MISSING** the. Phase 4 ne infrastructure banaya, features nahi.

### Billing — DONE ✅

`BillCalculator` arithmetic rakhta hai (pure, testable), `BillingService` DB
ka kaam karta hai. SRS ka order: subtotal → discount → **tax discounted amount
par** → total.

| ID | Feature | Status | Kahan |
|---|---|---|---|
| BL-01 | Bill generation engine (lines → subtotal → discount → tax → total) | ✅ 46 checks | `BillCalculator` |
| BL-02 | Service Tax 12.24% applied (discounted amount par) | ✅ verified | `TaxConstants` |
| BL-03 | Bill number generation `INV-{year}-{00001}` | ✅ verified | `AccountIdGenerator` |
| BL-04 | Receipt number generation `RCP-{yyyymm}-{00001}` | ✅ verified | `AccountIdGenerator` |
| BL-05 | Invoice / bill detail screen real data se | ✅ HTTP 200 | `Views/Bills/Details.cshtml` |
| BL-06 | Billing ledger screen (staff + customer views) | ✅ HTTP 200 | `Views/Bills/Index.cshtml` |
| BL-07 | Outstanding dues calculation | ✅ verified | `BillSummary.OutstandingAmount` |
| BL-08 | Overdue detection + filter | ✅ verified | `BillQuery.OverdueOnly` |
| BL-09 | Payment recording → bill balance update | ✅ verified | `BillingService` |
| BL-10 | Auto status: Draft→Issued→PartiallyPaid→Paid | ✅ 15 checks | `RecordPaymentAsync` |
| BL-11 | Overpayment rejected | ✅ verified | service |
| BL-12 | Cancel bill (draft/issued only, reason lazmi) | ✅ verified | `CancelAsync` |
| BL-13 | Monthly billing run, per-period idempotent | ✅ verified | `GenerateForPeriodAsync` |
| BL-14 | Only Active lines billed (SRS rule V9) | ✅ verified | `PrepareDraftAsync` |
| BL-15 | Payment blocked until the bill is issued | ✅ verified | service |
| BL-16 | Cheque payment needs a reference | ✅ verified | service |
| BL-17 | CSRF on the payment POST | ✅ 400 without token | `[ValidateAntiForgeryToken]` |
| BL-18 | IDOR — customer cannot read another's bill | ✅ 302 AccessDenied | `BillsController` |
| BL-19 | Landline call charges (local 55c/75c/70c/60c, STD 2.25$/2.00$/1.75$) | ⬜ BLOCKED | `CallLine` ready, koi metered source nahi |

**BL-19 blocked kyun hai:** `BillCalculator.CallLine()` maujood hai aur
arithmetic test bhi ho chuki, magar SRS kahin nahi kehta ke per-period call
minutes kahan se aate hain. Jab tak koi usage-capture screen na bane, ye line
kabhi generate nahi hogi. Iske liye ek input screen chahiye — wo Phase 5.3
(call metering) ka kaam hai.

### Bulk / Corporate discount — DONE ✅

| ID | Feature | Status | Kahan |
|---|---|---|---|
| BD-01 | Connection count → tier lookup (10-15/15-25/25-50/50+) | ✅ 9 boundary checks | `BulkDiscountTiers` |
| BD-02 | Discount applied to the bill (subtotal par) | ✅ verified | `BillBreakdown` |
| BD-03 | Tax charged on the **discounted** amount, not gross | ✅ verified | `BillCalculator` |
| BD-04 | Discount line dikhana bill par | ✅ verified | `BillingService.CreateAsync` |
| BD-05 | 100% tier → zero bill, negative tax nahi | ✅ verified | `BillCalculator` |
| BD-06 | Corporate flag customer record par | ✅ | `Customer.IsCorporate` |
| BD-07 | Live connection count (deleted + permanently-inactive exclude) | ✅ verified | `Customer.LiveConnectionCount` |

Boundary proof: 9→0%, 10→25%, 14→25%, 15→50%, 24→50%, 25→75%, 49→75%,
50→100%, 500→100%.

Worked example (real DB, not hand-waved): 10 live connections → 25% off `$175`
→ discount `$43.75` → taxable `$131.25` → tax `$16.07` → total **`$147.32`**.

**Ek baat saaf:** tier `Customer.LiveConnectionCount` se aata hai — yani
discount **customer ke poore account** par lagta hai, per-connection nahi.
SRS isay "bulk/corporate discount" kehta hai, jo account-level hai.

### Feasibility workflow — 0%
- [ ] Feasibility check form + save
- [ ] Distance / capacity rules
- [ ] Dial-Up landline verification
- [ ] PASS/FAIL → order state transition
- [ ] Technician assignment

### Connection lifecycle — 0%
- [ ] Activate connection (order → connection)
- [ ] Temp Inactive / Perm Inactive transitions
- [ ] `ConnectionStatusHistory` record karna
- [ ] Temp Inactive par bill rokna (rule V9)
- [ ] Reconnection flow

### Registration — DONE ✅

`/Orders/New` ab connection bhi leta hai aur account bhi banata hai, ek hi
transaction mein. Alag register page delete kar diya (`/Account/Register`
sirf `302` karta hai). POST verify hua: `302` → `/Customer/Dashboard`,
`Users` → `Customers` → `ConnectionOrders` teen tables linked.

- [x] Customer registration form (real, DB mein save)
- [x] AccountId generator wire karna
- [x] CNIC uniqueness check
- [x] Security deposit calculation
- [x] Landline requirement validation (Dial-Up)

### Search — 0%
- [ ] Advanced search: ID / naam / type / date / contact
- [ ] Cross-entity search
- [ ] Server-side paging

### Vendor / Procurement — 0%
- [ ] Vendor CRUD
- [ ] Purchase order create
- [ ] PO line items
- [ ] Receive stock from PO

### Stock / Inventory — 0%
- [ ] Stock list per shop
- [ ] Stock allocation
- [ ] Stock movement history
- [ ] Low stock alert
- [ ] Concurrency handling (RowVersion)

### Retail shop management — 0%
- [ ] Shop CRUD
- [ ] Staff assignment
- [ ] Per-shop dashboard real data

### Employee management — 0%
- [ ] Employee CRUD
- [ ] Role assignment
- [ ] Shop assignment

### Reports — 0%
- [ ] Connection report
- [ ] Revenue report
- [ ] Outstanding dues report
- [ ] Bulk connection report

### Customer-facing — 0%
- [ ] Customer dashboard real data se
- [ ] My Connection page real
- [ ] My Bills real
- [ ] Order tracking real
- [ ] Feedback submit → DB

---

## Kya BAKI hai — non-feature

### Pending cleanup
- [ ] `ApiService.cs` (mock) ko poora hata kar real services
- [ ] Hardcoded `NX12345678` aur `Ahmed Khan` hata (4 jagah)
- [ ] `ProfileController` ka fake data
- [ ] 910 inline styles views mein
- [ ] `!important` cleanup (22 instances)

### Security (Phase 4.5 adhoora)
- [ ] Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`)
- [ ] Content Security Policy
- [ ] Rate limiting on login + public search
- [ ] File upload validation (agar zaroori ho)
- [ ] Secrets user-secrets mein (abhi plaintext appsettings mein)

### Error handling (Phase 4.7 adhoora)
- [ ] Branded 404 page
- [ ] Branded 500 page
- [ ] Global exception handler with logging
- [ ] Empty states har list par
- [ ] AJAX error toast

### Performance (Phase 4.8)
- [ ] Server-side paging har list par
- [ ] `AsNoTracking` read queries par
- [ ] N+1 queries fix
- [ ] Indexes verify execution plans
- [ ] Memory cache (plans, cities, tiers)
- [ ] Static asset caching + minification

### Accessibility (Phase 4.9)
- [ ] Keyboard navigation full audit
- [ ] Focus rings everywhere
- [ ] ARIA labels on icon buttons
- [ ] Contrast WCAG AA verify
- [ ] Table `<th scope>`
- [ ] Skip-to-content link
- [ ] Modal focus trap
- [ ] `prefers-reduced-motion`

### Testing
- [ ] Unit tests (kya koi test project bhi hai? — **nahi**)
- [ ] Integration tests
- [ ] `docs/09-testing-checklist.md` actually chalana

### CDN / offline
- [ ] Font Awesome local
- [ ] Chart.js local
- [ ] Google Fonts local

---

## Phase 6 — Aptech deliverables (0%)

| # | Deliverable | Status |
|---|---|---|
| 6.1 | ER Diagram | ❌ (schema ready hai, diagram banani hai) |
| 6.2 | Algorithms document | ❌ |
| 6.3 | GUI Standards Document | ❌ |
| 6.4 | Interface Design Document | ❌ |
| 6.5 | Unit Testing Check List | partial (`docs/09` draft hai) |
| 6.6 | Project Report + Synopsis | ❌ |
| 6.7 | 2 status emails | ❌ |

---

## Numbers

| Metric | Count |
|---|---|
| DB tables | 22 |
| Tables with real data | 8 (reference only) |
| Tables with **zero** rows | 14 |
| Domain entities | 22 |
| Service interfaces implemented | 4 of ~17 |
| Controllers with real data | **0** |
| SRS features done | ~10% |
| Build errors | 0 |

### Zero-row tables (features jo bilkul nahi hain)

`FeasibilityChecks · ConnectionStatusHistory · Vendors · PurchaseOrders ·
PurchaseOrderLines · StockItems · StockMovements · Feedback`

Ye 8 tables khaali hain kyunki unka **koi code hi nahi** hai jo unmein likhe.

In tables ka **code ab maujood hai** (bas test data hata diya, is liye rows 0
hain): `Customers · ConnectionOrders · Connections · Bills · BillLines ·
Payments`.

---

## Ab kya agla

1. ~~Registration~~ — DONE (5.1)
2. ~~Billing engine~~ — DONE (5.2)
3. **Feasibility + connection lifecycle** — order ka poora flow
4. **Advanced search** — SRS explicitly maangta hai
5. Baaki (vendor / stock / retail / employee / reports)

## Sab se pehle kya karna chahiye

1. ~~**Registration**~~ — **DONE** (5.1, `/Orders/New` mein merge ho gayi)
2. ~~**Billing engine**~~ — **DONE** (5.2, tax + discount + payments + invoices)
3. **Feasibility + connection lifecycle** — order ka poora flow
4. **Search** — SRS explicitly maangta hai
5. Baaki (vendor/stock/retail/employee/reports)

---

## Seedha jawab

**Kitna add hua:** infrastructure ~90%, frontend ~89%.
**Kitna baki:** SRS ke features ~78% baki, Aptech docs ~85% baki.

Dhancha khara hai, aur ab do asli features bhi hain (registration, billing).
Ab tak jo bana wo asli DB par chalta hai — 46 arithmetic + 15 lifecycle checks
aur HTTP par CSRF + IDOR verified.

**Magar ye bhi sach hai:** dashboards, reports, profile, feedback — ye sab abhi
bhi `ApiService.cs` ke hardcoded mock se data le rahe hain. Sirf `/Orders/New`
aur `/Bills` asli data par hain.
