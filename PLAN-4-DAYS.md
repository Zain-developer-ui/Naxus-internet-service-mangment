# NEXUS — 4 Din Ka Plan

**Aaj:** 8 Oct 2026 · **Deadline:** 12 Oct 2026
**Target:** Aptech Project of the Month

Ye plan SRS ke against likha gaya hai — code padh kar, andaze se nahi.

---

## 1. Aaj ka scorecard (SRS ke against)

Purani matrix (`docs/03`) **5 Oct** ki thi aur kehti thi "64% MISSING, 2% DONE".
**Wo ab bilkul ghalat hai.** Uske baad Phase A/B/C, E, F, aur F1–F12 ho gaye.

Aaj ka asli haal — SRS ke **53 functional requirements**:

| Status | Count | % |
|---|---|---|
| ✅ **DONE** | 37 | 70% |
| ⚠️ **PARTIAL** | 8 | 15% |
| ❌ **MISSING** | 8 | 15% |

**Yani 70% SRS ho chuka hai, 30% baqi.** Pehle jo 2% tha wo ab 70% hai.

### Jo SRS ke against ab poora hai

| Group | Reqs | Status |
|---|---|---|
| Connection types (Dial-Up / Broadband / Telephone) | FR-01…04 | ✅ |
| 5 roles + login + authorization | FR-10…16 | ✅ |
| Admin: employee, stock, vendor, shop, plan CRUD | FR-20,21,22,24,25,26 | ✅ |
| Accounts: bill generate, charges, display | FR-30…33 | ✅ |
| Technical: order status, create connection, inactive | FR-40…43 | ✅ |
| Retail: order place, status view | FR-50, 52 | ✅ |
| Customer: contact, order, track, bills | FR-60…63 | ✅ |
| ID formats (order 11-digit, account 16-digit, city code) | FR-70…72 | ✅ |
| Feasibility (distance, capacity, landline rules) | FR-80…83 | ✅ |
| Billing: deposit, hourly/unlimited plans, 12.24% tax, bulk discount | FR-90…94, 97, 98 | ✅ |
| Search: order id, account id, due amount | FR-110…112 | ✅ |
| Lifecycle: Active / Temp / Perm Inactive | FR-120 | ✅ |
| Feedback | FR-130 | ✅ |

**SRS ke exact prices bhi mojood hain:** $50 / $130 / $260 / $75 / $175 / $315 —
plan names bhi SRS ke (Dial-Up 10/30/60 Hours, Unlimited 28K/56K, Broadband 64K/128K).

---

## 2. Kya baqi hai — 16 items

### ❌ MISSING (8) — SRS explicitly maangta hai

| # | Req | Kya chahiye | Waqt |
|---|---|---|---|
| M1 | **FR-121** | **Bill ke hisaab se connection status badle** — unpaid → Temporarily Inactive → Permanently Inactive. Enum hai, logic nahi. **SRS ka core rule hai.** | 3–4 ghante |
| M2 | **FR-113** | **Advanced search** — 5 filters: unique id, naam, connection type, date/period, contact number. Abhi ek text box hai. | 3–4 ghante |
| M3 | **FR-51** | Retail: **orders till-date list** (view nahi hai) | 2 ghante |
| M4 | **FR-53** | Retail: **connection details till-date** | 2 ghante |
| M5 | **FR-54** | Retail: **till-date billing** | 1–2 ghante |
| M6 | **FR-55** | Retail: **till-date payment** | 1–2 ghante |
| M7 | **FR-23** | **Purchase list / purchase order workflow** — entity mojood, UI nahi | 4–5 ghante |
| M8 | **FR-96** | **Call charges** — local/STD/SMS per minute. Arithmetic ready (`BillCalculator.CallLine`), input screen nahi | 3 ghante |

### ⚠️ PARTIAL (8) — thora kaam baqi

| # | Req | Kya baqi |
|---|---|---|
| P1 | FR-06 | Vendor se **equipment purchase** — vendor CRUD hai, purchase link nahi (M7 se juda) |
| P2 | FR-44 | **Technical equipment maintain** — abhi Admin/Retail ke paas hai |
| P3 | FR-95 | **Landline rental + calls** — rental hai, calls nahi (M8 se juda) |
| P4 | FR-99 | **Discount advance + security deposit par** — abhi subtotal par |
| P5 | FR-100 | **Pehli payment ko billing mein adjust** |
| P6 | FR-101 | **Replacement charges** — customer ne equipment kharab kiya. Line item hai, charge lagane ka flow nahi |
| P7 | FR-122 | **Postpaid-only enforcement** — koi prepaid code nahi, magar rule bhi nahi |
| P8 | FR-132 | **City-wise + year-wise records** |

---

## 3. Non-SRS — Aptech marking ke liye zaroori

| # | Kaam | Kyun zaroori | Waqt |
|---|---|---|---|
| N1 | **Security headers + CSP** | Marking sheet mein "Security" hai. Abhi `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, CSP — koi nahi | 2 ghante |
| N2 | **Rate limiting** | Login brute-force. Abhi sirf per-account lockout | 1 ghanta |
| N3 | **404 / 500 branded pages + global handler** | Abhi default error | 2 ghante |
| N4 | **Tests** | Koi test project nahi. Marking mein "Testing" hai | 4 ghante |
| N5 | **Accessibility pass** | Skip link, focus rings, ARIA, `prefers-reduced-motion` | 2 ghante |
| N6 | **CDN → local** | Demo offline ho to Font Awesome tootegi | 1 ghanta |

---

## 4. Aptech deliverables — ye 100% baqi hain

| # | Deliverable | Status |
|---|---|---|
| 6.1 | **ER Diagram** | ❌ — schema ready hai (23 tables), diagram banani hai |
| 6.2 | **Algorithms document** | ❌ — 8 algorithms likhne hain |
| 6.3 | **GUI Standards Document** | ❌ — tokens + components ready hain, doc banani hai |
| 6.4 | **Interface Design Document** | ❌ — 59 screens ka layout |
| 6.5 | **Unit Testing Check List** | ⚠️ draft (`docs/09`) |
| 6.6 | **Project Report + Synopsis** | ❌ |
| 6.7 | **2 status emails** | ❌ |

**Ye 7 cheezein project of the month ke liye sab se zyada weight rakhti hain** —
inmein se ek bhi missing ho to grade kat jata hai.

---

## 5. 4-din ka plan

### Din 1 (9 Oct) — SRS ke core missing features ✅ DONE

| Slot | Kaam | Status |
|---|---|---|
| Subah | **M1 — Bill → connection status lifecycle.** Overdue bill → Temporarily Inactive (X din baad) → Permanently Inactive. `ConnectionStatusHistory` mein likhe. Manual override bhi. | ✅ DONE |
| Sham | **M2 — Advanced search.** 5 filters: id, naam, type, date range, contact. Server-side, paged. Retail + Admin dono se. | ✅ DONE |

**Din 1 ka target:** SRS ka core rule (bill-based status) + jo SRS ne explicitly
maanga (advanced search) — dono chal rahe hon. **Dono ho gaye.**

Saath mein static data ke 13 findings bhi fix ho gaye (S1) — dekho
[[NEXUS - Static Data Audit]].

### Din 2 (10 Oct) — Retail views + procurement

| Slot | Kaam |
|---|---|
| Subah | **M3–M6 — Retail till-date views.** Orders list, connections, billing, payments. SRS Retail login ke liye ye chaar explicitly maangta hai. |
| Sham | **M7 — Purchase order workflow.** PO create → lines (product + qty + vendor) → receive → stock badhe. `PurchaseOrders` + `PurchaseOrderLines` tables ready hain, khaali hain. |

**Din 2 ka target:** Retail portal poora, aur Admin ka procurement loop band.

### Din 3 (11 Oct) — Billing ke baqi hisse + security

| Slot | Kaam |
|---|---|
| Subah | **M8 — Call metering.** Landline per-period minutes enter karne ki screen → `BillCalculator.CallLine` se charges. |
| Dopahar | **P5, P6 — First payment adjust + Replacement charges.** |
| Sham | **N1, N2, N3 — Security headers, CSP, rate limiting, 404/500 pages.** |

**Din 3 ka target:** Billing 100%, security 100%.

### Din 4 (12 Oct) — Deliverables + polish

| Slot | Kaam |
|---|---|
| Subah | **6.1 ER Diagram** + **6.2 Algorithms** |
| Dopahar | **6.3 GUI Standards** + **6.4 Interface Design** + **6.5 Test checklist** |
| Sham | **6.6 Report + Synopsis** + **6.7 2 emails** + demo rehearsal |

**Din 4 ka target:** Har deliverable ready, demo bina error.

---

## 6. Agar waqt kam pare — kya chhodna hai

Priority order (upar wala pehle):

1. **Aptech deliverables (6.1–6.7)** — ye na hon to grade kat jayega
2. **M1 (bill → status)** — SRS ka core rule
3. **N1 (security headers + CSP)** — marking sheet mein hai
4. **M2 (advanced search)** — SRS explicitly maangta hai
5. **M3–M6 (retail views)** — SRS Retail ke liye maangta hai
6. **N3 (404/500)** — chhota, achha lagta hai
7. **M7 (purchase orders)** — bara kaam, magar SRS mein hai
8. **M8 (call charges)** — arithmetic ready, input screen chahiye
9. **P1–P8** — inmein se sirf P5, P6 dikhne wale hain
10. **N4 (tests), N5 (a11y), N6 (CDN)** — agar waqt bache

**Sab se bara risk:** Din 4 mein saare deliverables ek saath. Behtar hai har din
1 ghanta deliverables ko do — warna aakhri din panic hoga.

---

## 7. "Project of the Month" ke liye extra

Ye cheezein already strong hain — inhe **demo mein dikhana**:

| Cheez | Kyun standout |
|---|---|
| 23 tables, EF Core Code First | Real architecture, mock nahi |
| 5 roles, poora authorization | Har portal ka apna login |
| Billing engine (46 arithmetic checks) | Tax + discount + tiers — asli arithmetic |
| Race-safe ID generators | Concurrency handle |
| Feasibility rules engine | Faisla code karta hai, officer nahi |
| Real-time order tracking | Timeline real status se |
| 24 skills se banaya gaya design | Portfolio-grade UI |

**Extra features jo add kar sakte hain (agar waqt bache):**

| Feature | Faayda | Waqt |
|---|---|---|
| **PDF invoice download** | Demo mein bohot achha lagta hai | 3 ghante |
| **Audit trail view** | Admin dekh sake kis ne kya badla | 2 ghante |
| **Dashboard CSV/Excel export** | Reports mein already CSV hai, extend karo | 1 ghanta |
| **Notification bell** (in-app) | Overdue bills, naye orders | 3 ghante |
| **Bulk customer import (CSV)** | Bara data demo karne ke liye | 3 ghante |

---

## 8. Honest risk

**4 din mein 16 SRS items + 7 deliverables + 6 non-SRS = bohot hai.**

Realistic: agar din mein 8–10 ghante kaam ho, to **~12 items** ho sakte hain, saare 29 nahi.

Is liye **Section 6 ka priority order follow karo** — jo na ho sake wo honestly
ledger mein likho. Aadha kaam dikhane se behtar hai poora kam, saaf.

**Jo bilkul nahi karna:**
- Bug chhupa kar aage barhna
- Fake data daal kar "kaam karta hai" dikhana
- Deliverables aakhri raat mein jaldi jaldi likhna

---

## Related

- [[NEXUS - Status Report]]
- [[NEXUS - Progress]]
- `TASK-LEDGER.md` (workspace root)
