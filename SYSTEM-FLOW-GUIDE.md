# NEXUS — Poora System Flow Guide

Ye document poore project ka flow samjhaane ke liye hai. Har feature kaunsa
controller/service manage karta hai, aur data ka safar kahan se kahan tak
jaata hai — order se bill tak.

---

## 1. Paanch panels (roles)

| Panel | Role | Entry | Kaam |
|---|---|---|---|
| Admin | Admin | `/Admin/Dashboard` | Sab kuch: plans, orders, inventory, vendors, POs, outlets & staff, staff logins, cities, discount slabs, settings, reports, plan changes |
| Retail | Retail | `/Retail/Dashboard` | Naya order (4-step wizard), till-date register, customer search, stock dekhna |
| Technical | Technical | `/Technical/Dashboard` | Survey queue, feasibility, installation start/complete, connections, overdue policy |
| Accounts | Accounts | `/Accounts/Dashboard` | Bills, payments, overdue, call metering, one-off charges, monthly billing run |
| Customer | Customer | `/Customer/Dashboard` | Apni connection, bills & payments, profile, feedback, plan change, order tracking |

**Login credentials (seed):**
| Role | Account ID | Password |
|---|---|---|
| Admin | `A0000000000001` | `Nexus@2027` |
| Retail | `R0000000000001` | `Nexus@2026` |
| Technical | `T0000000000001` | `Nexus@2026` |
| Accounts | `F0000000000001` | `Nexus@2026` |
| Customer | `B042000000000001` | (register karke banao) |

---

## 2. Bada flow — order se bill tak (end-to-end)

```
                        ┌──────────────────────────────────────────────┐
                        │ 1. ORDER BANTA HAI                            │
                        │    Retail ya public /Orders/New wizard        │
                        │    (customer account + order ek saath)       │
                        └──────────────────┬───────────────────────────┘
                                           ▼
                        ┌──────────────────────────────────────────────┐
                        │ 2. FEASIBILITY (Technical)                    │
                        │    /Operations → RecordCheck                  │
                        │    5 km se door → fail                        │
                        └──────────────────┬───────────────────────────┘
                                           ▼
                        ┌──────────────────────────────────────────────┐
                        │ 3. CONFIRM → installation schedule            │
                        │    StartInstallation → CompleteInstallation  │
                        └──────────────────┬───────────────────────────┘
                                           ▼
                        ┌──────────────────────────────────────────────┐
                        │ 4. CONNECTION banti hai (Pending)             │
                        │    ChangeStatus → Active (warna billing skip) │
                        └──────────────────┬───────────────────────────┘
                                           ▼
                        ┌──────────────────────────────────────────────┐
                        │ 5. BILLING RUN (Accounts)                     │
                        │    /Accounts RunBilling → bill Draft         │
                        │    Issue → Issued                             │
                        └──────────────────┬───────────────────────────┘
                                           ▼
                        ┌──────────────────────────────────────────────┐
                        │ 6. PAYMENT (Accounts)                         │
                        │    /Bills RecordPayment → Paid/PartiallyPaid  │
                        └──────────────────────────────────────────────┘
```

### Har step kaunsa controller/service

| Step | Route | Controller | Service |
|---|---|---|---|
| Order wizard | `/Orders/New` | `OrdersController.New` | `RegistrationService.RegisterAsync` |
| Walk-in order | `/Retail/NewOrder` | `RetailController.NewOrder` | `RegistrationService` (shopId attached) |
| Feasibility | `POST /Operations/RecordCheck` | `OperationsController` | `FeasibilityService` |
| Schedule confirm | `POST /Operations/Confirm` | `OperationsController` | `FeasibilityService` |
| Install start/complete | `POST /Operations/StartInstallation` / `CompleteInstallation` | `OperationsController` | `FeasibilityService` |
| Connection activate | `POST /Operations/ChangeStatus` | `OperationsController` | `ConnectionService` |
| Billing run | `POST /Accounts/RunBilling` | `AccountsController` | `BillingService.GenerateForPeriodAsync` |
| Bill issue | `POST /Bills/...` | `BillsController` | `BillingService.IssueAsync` |
| Payment | `POST /Bills/RecordPayment` | `BillsController` | `BillingService.RecordPaymentAsync` |
| One-off charge | `/Accounts/Charges` | `AccountsController` | `BillingService` (add/cancel) |

---

## 3. Bill kise banti hai (asli logic)

`BillingService.GenerateForPeriodAsync(start, end)`:

1. **Sirf `ConnectionStatus.Active` connections** (`CanBeBilled`) — pending
   lines skip.
2. Har connection ki ek hi bill per period (duplicate guard).
3. Bill lines:
   - **Rental** — plan ki monthly price (PlanPrices table se)
   - **Advance** — `min(AdvanceRemaining, charges)` — poora nahi lagta,
     baqi next bill par carry forward
   - **Security deposit** — plan ke hisaab se
   - **One-off charges** — `BilledOnBillId == null` wali pending charges
   - **Discount** — bulk tiers (10+ connections se shuru; default pe 0)
   - **Tax** — 12.24% (`TaxConstants`)
4. Bill **Draft** banti hai → Accounts `Issue` karta hai → customer ko dikhti hai.
5. Status: `Draft → Issued → PartiallyPaid → Paid` (ya `Overdue` due date ke baad).

**Payment:** `RecordPaymentAsync` — amount > 0, cheque needs cheque number,
partially pay ho sakti hai, cancelled/draft bill par payment nahi.

**Customer view:** sirf **non-Draft** bills (draft internal hai — 14 Oct ko fix).

---

## 4. Order mein kaunsa data kahan jaata hai

`RegistrationService.RegisterAsync(model)`:
- Customer row (account id `B` + 15 digits)
- User row (login) — same request mein
- ConnectionOrder (`B` + 10 digits) — status `AwaitingFeasibility`
- Feasibility ke baad Connection (`CON-B-...`)

Rules: plan type = connection type, DialUp/Telephone pehli registered landline
maangta hai, CNIC + phone unique, city serviced honi chahiye.

---

## 5. Naya feature — Plan change (Upgrade)

Customer apni connection ka plan change **request** karta hai, admin approve
karta hai (customer seedha switch nahi kar sakta — deposit/speed confirm
karni hoti hai):

1. Customer: `/Customer/Upgrade` — usi connection type ke plans dikhte hain
2. Request `PlanChangeRequests` table mein `Pending`
3. Admin: `/Admin/PlanChanges` (sidebar) → Approve/Reject
4. Approve → connection ka `PlanId` badal jaata hai + status history note;
   agli bill naye plan par banti hai

---

## 6. Roles ka authorization map

| Controller | Roles |
|---|---|
| `AdminController` | Admin only |
| `AdminSettingsController`, `AdminPlansController`, `InventoryController`, `OrganisationController`, `PurchaseOrdersController`, `VendorsController` | Admin |
| `OperationsController` | Technical, Admin, Accounts |
| `AccountsController` | Accounts, Admin |
| `BillsController` | Customer, Accounts, Admin |
| `RetailController` | Retail, Admin |
| `SearchController` | saare staff (Admin/Accounts/Technical/Retail) |
| `CustomerController` | Customer (apna data), staff ?id= se |
| `OrdersController.New` | public (feature.registration gate) |
| `OrdersController.Tracking` | Customer, Retail, Technical, Admin |
| `AccountController.Status` | public lookup; signed-in customer ko apna status seedha |
| `FeedbackController` | Customer, Admin (feature.feedback gate) |

**IDOR protection:** `CustomerController.ResolveCustomerId` — customer sirf
apni id dekhta (`User.CustomerId()`), staff `?id=` de sakta hai.

---

## 7. Baaki features kaun manage karta hai

| Feature | Kahan |
|---|---|
| Notifications (topbar bell) | `NotificationService` — live counts (surveys, overdue, charges, low stock, bills due) |
| Settings (site name, brand, features) | `/Admin/Settings` → `SiteSettingsCatalog` + `_Layout` |
| Staff logins | `/Admin/Organisation/Logins` — Admin role yahan se nahi banta |
| Discount slabs | `/Admin/Settings/Discounts` |
| Reports | `/Reports` — city-wise, year-wise, customers |
| Search | `/Search/Advanced` (staff) — customers/orders/connections |
| Forgot password | `PasswordResetService` — email (smtp on) ya mobile token |
| Call metering | `/Accounts/Calls` |
| Advance carry-forward | `ConnectionOrder.AdvanceApplied` / `AdvanceRemaining` |

---

## 8. Aaj ke fixes (14-15 Oct)

1. Admin dashboard crash (`IsPending` EF-translate) — fixed
2. Dead settings wired (site.company, brand.logo, feature.feedback, feature.autoInvoice)
3. `Services/Details` dead action removed
4. Staff login se Admin role nahi bana sakte (form + service guard)
5. Customer tracking: single order → seedha detail + connection card
6. Retail Register ke 403 links theek (order → Tracking; bill/connection plain text)
7. Customer dashboard ke fake bill no/amount hatae (draft ab customer ko nahi dikhta)
8. Signed-in customer ko Track/Status par apna data seedha
9. **Plan change feature poora naya** (entity + migration + service + customer form + admin approve)
