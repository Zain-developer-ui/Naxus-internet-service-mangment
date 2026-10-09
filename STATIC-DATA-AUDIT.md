# NEXUS — Static Data Audit

**Maqsad:** koi cheez static na bache. Har cheez DB se aani chahiye.

Aakhri audit: 8 Oct 2026. **Sab findings fix ho gaye** — neeche har ek ka record hai.

---

## Nateeja

| Severity | Findings | Fix |
|---|---|---|
| **HIGH** | 4 | ✅ sab fix |
| **MEDIUM** | 5 | ✅ sab fix |
| **LOW** | 4 | ✅ sab fix |

**Verify command** — kuch nahi aana chahiye:

```cmd
cd "C:\Users\Zain Ansari\source\repos\Zain-developer-ui\Naxus-internet-service-mangment"
findstr /s /i /n "NX12345678 Ahmed Khan Jan 2025 demo Live Chat" Views\*.cshtml wwwroot\js\*.js
```

---

## HIGH — sab fix ✅

### H1. `MyConnection.cshtml` — fake fallback model

**Tha:** `Model ?? new AccountViewModel { AccountId = "NX12345678", CustomerName = "Ahmed Khan", Plan = "Standard 15 Mbps" }`

**Ab:** `var acc = Model;` — fallback poora hata. Controller pehle se `NotFound()`
deta hai jab record na mile, to ye kabhi zaroorat nahi thi.

### H2. `MyConnection.cshtml` — hardcoded timeline

**Tha:** "08 Jan 2025 · 10:14 AM", "09 Jan 2025 · 02:30 PM", technician "Usman Ahmed"

**Ab:** `CustomerPortalService.BuildTimelineAsync` — `ConnectionStatusHistory` se
asli moves, `ConnectionOrder.CreatedAt` se order entry, aur `FeasibilityChecks` se
technician ka naam. Sab `ChangedAt` timestamps ke saath.

**Verified:** Bilal Ahmed Khan ke liye — "Order submitted 06 Oct 2026 · 13:08",
"Installation completed 06 Oct 2026", "Connection activated 06 Oct 2026 · 13:18".

### H3. `bills.js` — dead code + fake toasts

**Tha:** 148-line file, har page par load, jismein:
- `initPaymentForm()` — `form[data-payment-form]` dhoondta tha jo **kisi view mein nahi**
- "Payment processed successfully (demo)" toast
- "Invoice downloaded (demo)" toast
- File ka header: *"mock payment flow, invoice print/download stubs"*

**Ab:** **Poori file delete.** `_Layout.cshtml` se script tag bhi hata.

**Sabit:** File ke saare **9 selectors** kisi view mein nahi the, koi `NEXUS.*`
export bhi nahi tha, aur asli payment form `Views/Bills/Details.cshtml:174` par
hai jo seedha `POST /Bills/RecordPayment` karta hai. Yani file 100% dead thi.

**Verified:** `/js/bills.js` → **404**.

### H4. `MyConnection.cshtml` — hardcoded plan features + equipment

**Tha:** "Unlimited data (no caps, no throttling)", "Free Wi-Fi router included",
"NEXUS Router R2 (Wi-Fi 6)", "VDSL2 modem · Supports up to 100 Mbps".
Aur file khud kehti thi: *"Demo equipment information shown for reference."*

**Ab:** Dono plan aur catalogue se derive hote hain.

`BuildFeatures(plan, type)` — `Plan` ke apne columns se:
- `IsUnlimited` → "Unlimited usage with no hourly cap"
- `HoursIncluded` → "60 hours of usage included each cycle"
- `SpeedKbps` → "56 Kbps download speed"
- `ConnectionType` → "Runs over your existing NEXUS landline" ya "Dedicated broadband line - no landline needed"
- `Description` → plan ka apna description
- `SecurityDeposit` → "$500.00 security deposit, refundable on withdrawal"

`BuildEquipmentAsync(type)` — `EquipmentProducts` table se (6 live products).
Telephone line ko list milti hi nahi (usay data equipment chahiye nahi).

**Verified (Bilal, Broadband 60 Hours):** "60 hours of usage included each cycle",
"Dedicated broadband line - no landline needed", "$500.00 security deposit";
equipment — 56K Dial-Up Modem, Broadband Router, Broadband Router (4 Port),
Line Splitter, Network Cable 15m, Telephone Cable 10m.

---

## MEDIUM — sab fix ✅

### M1. `MyConnection.cshtml` — fake service health

**Tha:** "All systems operational", "Excellent", "Stable", "Verified" — koi
monitoring system hi nahi hai.

**Ab:** Card **"Your connection"** ban gaya, jo real fields dikhata hai —
connection type, speed, line status, billing status. Plus `/Customer/Dashboard`
par bhi "Service Health" ko **"Your connection"** se replace kiya.

### M2. `Customer/Dashboard.cshtml` — fake fallback dates

**Tha:** `?? "25 Oct 2025"`, `?? "12 Jan 2025"`, `?? "25 Sep 2025"`, `?? "Sep 2025"`

**Ab:** `?? "—"` — dash imaandar hai, fake date nahi.

### M3. `Customer/Dashboard.cshtml` — "Excellent" / "Stable"

**Ab:** Hata diye. Unki jagah real fields (`acc.Status`, `acc.Speed`).

### M4. `dashboard.js` — "in this demo"

**Tha:** `'That action is not available in this demo.'`

**Ab:** `'That shortcut is not wired up yet.'`

### M5. `Account/Status.cshtml` — purana placeholder

**Tha:** `placeholder="NX12345678"` (purana format)

**Ab:** `placeholder="B042000000000001"` (SRS ka 16-digit format)

---

## LOW — sab fix ✅

### L1. `charts.js` — stale comment

**Tha:** *"and falls back to built-in demo data"* — 8 Oct ko fake fallback hata
diya gaya tha, comment purana reh gaya.

**Ab:** *"A chart with no series is not drawn at all rather than filled with
invented data."*

### L2. `Home/Contact.cshtml` — fake "Live Chat"

**Tha:** "Live Chat" card with **"Live Now"** badge aur "Start Chat" button jo
sirf demo toast dikhata tha. Aur 3 aur jagah "live chat" ka zikr tha.

**Ab:** Card **"Send a Message"** ban gaya — form par le jata hai (`#contact-form`),
jo asli hai aur DB mein save karta hai. Demo chat JS hata. Baaki teen mentions
bhi theek kiye (hero card, FAQ answer, home features list).

### L3. `Home/Contact.cshtml` — placeholder naam

`placeholder="e.g. Ahmed Khan"` — sirf input hint hai, data nahi. Rehne diya.

### L4. `MyConnection.cshtml` — 40+ inline styles

**Ab:** `MyConnection` poora rewrite hua — inline styles ki jagah classes
(`banner-identity`, `banner-avatar`, `badge-ghost`, `stat-value.compact`,
`inline-actions`, `dash-section`, `dash-heading`, `dash-grid-*`).

Naye CSS classes `dashboards-luxury.css` mein add kiye.

---

## Bonus — jo is audit mein nikla

### Dead view-model fields

- `AccountViewModel.TechnicianName` — **bilkul dead** (koi set nahi karta, koi
  use nahi karta). **Hata diya.**
- `AccountViewModel.RouterModel` — service set nahi karta tha, magar
  `Account/Status.cshtml` use karta hai. **Ab `EquipmentProducts` ke pehle product
  se set hota hai.**

### Naye view-model fields (real data ke liye)

```csharp
public int? PlanId { get; set; }                                    // "View full plan" link
public IReadOnlyList<string> PlanFeatures { get; set; }             // plan se derive
public IReadOnlyList<AccountTimelineEntry> Timeline { get; set; }   // status history se
public IReadOnlyList<AccountEquipmentRow> Equipment { get; set; }   // catalogue se
public bool HasPlanFeatures / HasTimeline / HasEquipment            // sections toggle
```

`BillingStatus` ab bills se compute hota hai (pehle khaali reh jata tha):
koi unpaid bill ho to "Payment due", warna "Up to date".

---

## Jo cheezein THEEK hain (static hone chahiye)

| Cheez | Kyun theek |
|---|---|
| FAQ content | Static help text hai, data nahi |
| Home page ka copy | Marketing text |
| Privacy policy | Legal text |
| Error messages | Fixed strings |
| Tax rate 12.24% | `TaxConstants` mein, SRS se |
| Bulk discount tiers | DB mein hain (`BulkDiscountTiers`) |
| Equipment descriptions | DB mein hain (`EquipmentProducts`) |

---

## Related

- `TESTING-GUIDE.md` — team ke liye test plan
- `PLAN-4-DAYS.md` — baqi kaam
- `TASK-LEDGER.md` — poora record
