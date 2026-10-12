# NEXUS — 4 Din Ka Plan

**Aaj:** 8 Oct 2026 · **Deadline:** 12 Oct 2026
**Target:** Aptech Project of the Month

Ye plan SRS ke against likha gaya hai — code padh kar, andaze se nahi.

---

## 1. Aaj ka scorecard (SRS ke against)

Purani matrix (`docs/03`) **5 Oct** ki thi aur kehti thi "64% MISSING, 2% DONE".
**Wo ab bilkul ghalat hai.** Uske baad Phase A/B/C, E, F, F1–F12, M1–M8 aur
P1–P8 sab ho gaye.

Aaj ka asli haal — SRS ke **53 functional requirements**:

| Status | Count | % |
|---|---|---|
| ✅ **DONE** | 53 | 100% |
| ⚠️ **PARTIAL** | 0 | 0% |
| ❌ **MISSING** | 0 | 0% |

**Yani SRS poora ho chuka hai.** 5 Oct ka 2% → 70% (10 Oct) → **100% (12 Oct)**.

Baqi sirf **Aptech deliverables (6.1–6.7)** hain — wo documentation hai, code nahi.

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

### ✅ DONE — 10 Oct (M1, M2, M3–M6, M7)

| # | Req | Kya hua |
|---|---|---|
| ~~M1~~ | **FR-121** | **Bill ke hisaab se connection status** — 15 din → Temporarily Inactive, 45 din → Permanently Inactive. `/Operations/Overdue` |
| ~~M2~~ | **FR-113** | **Advanced search** — 5 filters. `/Search/Advanced` |
| ~~M3~~ | **FR-51** | Retail orders till-date — `/Retail/Register` |
| ~~M4~~ | **FR-53** | Retail connection details till-date — `/Retail/Register` |
| ~~M5~~ | **FR-54** | Retail till-date billing — `/Retail/Register` |
| ~~M6~~ | **FR-55** | Retail till-date payment — `/Retail/Register` |
| ~~M7~~ | **FR-23** | **Purchase order workflow** — raise → receive → stock. `/Admin/PurchaseOrders` |

### ✅ MISSING — kuch nahi bacha

| # | Req | Kya hua |
|---|---|---|
| ~~M8~~ | **FR-96** | **Call charges** — `/Accounts/Calls` screen, tariff per plan, bill par lines. **DONE 10 Oct** |

### ✅ PARTIAL — ab poora ho gaya

| # | Req | Kya hua |
|---|---|---|
| ~~P1~~ | FR-06 | **Vendor se equipment purchase** — product par *Preferred Vendor* + PO workflow (M7) se juda. **DONE** |
| ~~P2~~ | FR-44 | **Technical equipment maintain** — Technical role ko stock view + adjust de diya; catalogue Admin-only raha. **DONE** |
| ~~P3~~ | FR-95 | **Landline rental + calls** — **DONE (M8)** |
| ~~P4~~ | FR-99 | **Discount advance + security deposit par** — `BillBreakdown.DiscountableSubTotal`; discount sirf Advance/Security/Installation lines par. **DONE** |
| ~~P5~~ | FR-100 | **Pehli payment ko billing mein adjust** — `ConnectionOrder.AdvanceAdjustedOn`; advance ek hi dafa adjust hota hai. **DONE** |
| ~~P6~~ | FR-101 | **Replacement charges** — `ConnectionCharge` table + `/Accounts/Charges` screen; pending charge agli bill par khud lag jata hai. **DONE** |
| ~~P7~~ | FR-122 | **Postpaid-only enforcement** — activation par guard: plan ka koi billable price na ho to installation complete nahi hoti. **DONE** |
| ~~P8~~ | FR-132 | **City-wise + year-wise records** — Reports page par do roll-up tables. **DONE** |

**Ab SRS ka koi PARTIAL item baqi nahi.**

---

## 3. Non-SRS — Aptech marking ke liye zaroori

| # | Kaam | Kyun zaroori | Waqt |
|---|---|---|---|
| ~~N1~~ | ~~**Security headers + CSP**~~ | **DONE 10 Oct** — 6 headers + CSP with per-request nonce | — |
| ~~N2~~ | ~~**Rate limiting**~~ | **DONE 10 Oct** — login par per-IP 10/min | — |
| ~~N3~~ | ~~**404 / 500 branded pages + global handler**~~ | **DONE 12 Oct** — `ErrorPage` view + `UseExceptionHandler` + `UseStatusCodePagesWithReExecute` | — |
| ~~N4~~ | ~~**Tests**~~ | **Nahi karenge** — bahar ka tool (TestSprite) use hoga. Neeche section 5A dekho | — |
| ~~N5~~ | ~~**Accessibility pass**~~ | **DONE 12 Oct** — skip link, `aria-current`, modal focus trap, `aria-invalid`/`aria-describedby`, reduced-motion | — |
| ~~N6~~ | ~~**CDN → local**~~ | **DONE 12 Oct** — Inter + Font Awesome + jQuery validation sab `wwwroot/lib` se | — |

**N4 ke baare mein:** test project likhne ke bajaye **TestSprite** (AI testing SaaS)
use karne ka faisla hua. Wo app ko cloud se chalata hai aur generated
**Playwright + Python** test code export kar deta hai — yani Aptech ke
"Testing" deliverable ke liye chalne wala evidence mil jata hai, haath se
test likhne ke bajaye.

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

### Din 2 (10 Oct) — Retail views + procurement ✅ DONE

| Slot | Kaam | Status |
|---|---|---|
| Subah | **M3–M6 — Retail till-date views.** Orders list, connections, billing, payments. SRS Retail login ke liye ye chaar explicitly maangta hai. | ✅ `/Retail/Register` |
| Sham | **M7 — Purchase order workflow.** PO create → lines (product + qty + vendor) → receive → stock badhe. | ✅ `/Admin/PurchaseOrders` |

**Din 2 ka target:** Retail portal poora, aur Admin ka procurement loop band. — **PURA HUA.**

### Din 3 (10 Oct) — Billing ke baqi hisse + security ✅ DONE

| Slot | Kaam | Status |
|---|---|---|
| Subah | **M8 — Call metering.** Landline per-period minutes screen → bill par charges. | ✅ `/Accounts/Calls` |
| Dopahar | **P5, P6 — First payment adjust + Replacement charges.** | ✅ `AdvanceAdjustedOn` + `/Accounts/Charges` |
| Sham | **N1, N2 — Security headers, CSP, rate limiting.** | ✅ headers + nonce + 10/min |
| Sham | **N3 — 404/500 branded pages.** | ✅ `ErrorPage` + status-code re-execute |

**Din 3 ka target:** Billing 100%, security 100%. — **PURA HUA.**

### Din 3b (12 Oct) — SRS ke baqi PARTIAL + non-SRS ✅ DONE

| Slot | Kaam | Status |
|---|---|---|
| Subah | **P1, P2** — Vendor preferred link, Technical stock access | ✅ |
| Dopahar | **P4, P7, P8** — Discount basis, postpaid guard, city/year records | ✅ |
| Sham | **N5, N6** — Accessibility pass, CDN → local | ✅ |

**Yani SRS ke saare 53 requirements ab DONE ya intentionally-scoped hain.**

---

## 5A. Testing — TestSprite (N4 ka badal)

Test project haath se likhne ke bajaye **TestSprite** use karenge.

**Sabse zaroori baat:** TestSprite tests **cloud mein** chalata hai, aur uska CLI
`localhost` **reject** kar deta hai (`localhost`, `127.x`, `::1`, `0.0.0.0`,
RFC-1918 ranges). Uska apna wording:

> the cloud runner must be able to reach the URL over the public internet

To local app test karne ka **sirf ek raasta** hai: **MCP Server**, jo khud tunnel
kholta hai. CLI / Web Portal ke liye app publicly reachable honi chahiye.

| Surface | Localhost? | Note |
|---|---|---|
| Web Portal | ❌ | public URL chahiye |
| CLI | ❌ | local addresses pre-flight reject |
| **MCP Server** | ✅ | khud tunnel kholta hai; session-scoped, CI mein nahi |

**Setup:**
```
npm install -g @testsprite/testsprite-cli
testsprite setup
testsprite doctor
```

**Requirements:** Node **20.19+ / 22.13+ / 24+** (21.x aur 23.x **supported nahi**),
TestSprite account + API key, aur app chalti hui.

**PRD:** TestSprite spec-driven hai aur PRD "strongly recommended" kehta hai.
Aap ka **SRS document hi PRD hai** — ye bara advantage hai.

**Free tier:** $0, 150 credits/month, 1 environment, 30 din run history.
Uske baad Starter $19/mo (400 credits), Standard $39/mo, Pro $69/mo.

**⚠️ Demo se pehle ye teen cheezein dekh lena:**

1. **Login rate limit.** App mein per-IP 10/min aur per-account lockout hai.
   TestSprite "dozens of agents at once" chalata hai — cloud IPs se ek saath
   login karenge, aur test account **lock ho jayega**. Test run se pehle
   login rate limit barhao/hatao, aur test ke liye alag dedicated account banao.
2. **App chalti honi chahiye.** Tunnel sirf HTTP endpoint expose karta hai;
   SQL Server local hi rehta hai.
3. **Evidence.** Generated **Playwright + Python** code GitHub par export ho
   sakta hai — yani "Testing" deliverable mein sirf screenshot nahi, chalne
   wali test files daal sakte ho.

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
