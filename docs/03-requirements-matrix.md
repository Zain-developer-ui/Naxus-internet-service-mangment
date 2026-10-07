# 03 — Requirements Traceability Matrix

Har SRS requirement ke against: code mein hai ya nahi, kitna, kahan.

Legend: **MISSING** = bilkul nahi · **FAKE** = dikhawa hai, kaam nahi karta ·
**PARTIAL** = kuch hissa hai · **DONE** = theek hai

Last audit: 5 Oct 2026

---

## A. Connection types

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-01 | Dial-Up connection type | PARTIAL | `OrderViewModel.ServiceType`, 6 files mein zikr. Lekin landline-compulsory rule **nahi** hai |
| FR-02 | Broadband connection type | DONE | Plans mein hai (lekin speeds SRS se match nahi) |
| FR-03 | Telephone-only connection type (`T`) | **MISSING** | 0 files |
| FR-04 | Dial-Up ke liye landline compulsory rule | **MISSING** | Koi validation nahi |
| FR-05 | Equipment: Modem / Router | **MISSING** | `AccountViewModel.RouterModel` sirf ek display string hai |
| FR-06 | Modem/Router vendor se purchase | **MISSING** | 0 files |

## B. Roles

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-10 | Admin login | **FAKE** | Dashboard view hai, poora static. Koi login nahi |
| FR-11 | Accounts login | **FAKE** | Static dashboard, 23 inline styles. Koi login nahi |
| FR-12 | Technical login | **FAKE** | Static dashboard, 16 inline styles. Koi login nahi |
| FR-13 | Retail outlet login | **FAKE** | Static dashboard. Koi login nahi |
| FR-14 | Customer login | **FAKE** | `AccountController.Login` — koi bhi password chalta hai |
| FR-15 | Employee credentials | **MISSING** | Employee entity hi nahi |
| FR-16 | Role-based authorization | **MISSING** | 0 files. Sab dashboards public |

## C. Admin functions

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-20 | Employee management | **MISSING** | 0 files (sirf 2 files mein lafzi zikr) |
| FR-21 | Stock management | **MISSING** | 0 files |
| FR-22 | Vendor management | **MISSING** | 0 files |
| FR-23 | Purchase list | **MISSING** | 0 files |
| FR-24 | Retail shop management | **MISSING** | 0 files |
| FR-25 | Plan CRUD by admin | **MISSING** | `PlansController` sirf read karta hai. `Admin/Settings.cshtml` static |
| FR-26 | Charges CRUD | **MISSING** | 0 files |

## D. Accounts functions

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-30 | Bill generate | **MISSING** | `BillsController` sirf mock list dikhata hai |
| FR-31 | Charges calculate (plan + equipment) | **MISSING** | 0 files |
| FR-32 | Bill website par update | **MISSING** | 0 files |
| FR-33 | Amount paid + due amount display | PARTIAL | `BillDetailsViewModel` mein fields hain, calculation mock |

## E. Technical functions

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-40 | Order status update (feasible/not) | **MISSING** | Static dashboard |
| FR-41 | Naya connection create | **MISSING** | 0 files |
| FR-42 | Connection temporarily inactive | **MISSING** | Status string hai, transition logic nahi |
| FR-43 | Connection permanently inactive | **MISSING** | Same |
| FR-44 | Equipment maintain | **MISSING** | 0 files |

## F. Retail functions

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-50 | Order place (sirf retail) | PARTIAL | `Retail/NewOrder.cshtml` form hai, submit sirf toast deta hai |
| FR-51 | Orders till-date list | **FAKE** | Static table |
| FR-52 | Order status view | PARTIAL | `Orders/Tracking` mock data par |
| FR-53 | Connection details till-date | **FAKE** | Static |
| FR-54 | Till-date billing | **FAKE** | Static |
| FR-55 | Till-date payment | **FAKE** | Static |
| FR-56 | Customer payment record | **MISSING** | 0 files |

## G. Customer functions

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-60 | Apna contact details dekhna | **FAKE** | `ProfileController` hardcoded "Ahmed Khan" |
| FR-61 | Order place karna | PARTIAL | `Orders/New` form, submit mock |
| FR-62 | Order status track | PARTIAL | Mock data |
| FR-63 | Bills dekhna | PARTIAL | Mock data |

## H. ID formats

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-70 | Order ID: `D`/`T`/`B` + 10 digit | **MISSING** | Code `NX-ORD-1001` format use karta hai — **SRS se match nahi** |
| FR-71 | Account ID: 1 letter + 3 digit city + 12 serial | **MISSING** | Code `NX12345678` (10 char) — **SRS se match nahi** |
| FR-72 | City code (3 digit per city) | **MISSING** | 0 files |

## I. Feasibility

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-80 | Feasibility check workflow | **MISSING** | 2 files mein lafzi zikr, koi logic nahi |
| FR-81 | Dial-Up + landline → dono ka check | **MISSING** | 0 files |
| FR-82 | Landline same vendor → sirf internet check | **MISSING** | 0 files |
| FR-83 | Distance / server availability parameter | **MISSING** | 0 files |

## J. Billing

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-90 | Security Deposit (325/500/250$) | PARTIAL | `SecurityDeposit = 450m` PKR — SRS se match nahi |
| FR-91 | Dial-Up hourly plans (10/30/60 hrs) | **MISSING** | Code mein hourly tiers nahi |
| FR-92 | Dial-Up unlimited 28/56 Kbps | **MISSING** | 0 files |
| FR-93 | Broadband hourly 30/60 hrs | **MISSING** | 0 files |
| FR-94 | Broadband unlimited 64/128 Kbps | **MISSING** | 0 files |
| FR-95 | Landline rental + call charges | **MISSING** | 0 files |
| FR-96 | Call charges (local/STD/SMS per min) | **MISSING** | 0 files |
| FR-97 | Service Tax 12.24% | **MISSING** | 0 files |
| FR-98 | Bulk/Corporate discount 25/50/75/100% | **MISSING** | 0 files |
| FR-99 | Discount advance + security deposit par | **MISSING** | 0 files |
| FR-100 | Pehli payment ko billing mein adjust | **MISSING** | 0 files |
| FR-101 | Replacement charges (customer ne equipment kharab kiya) | **MISSING** | 0 files |

## K. Search

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-110 | Order ID se order status | PARTIAL | Mock data par |
| FR-111 | Account ID se connection status | PARTIAL | `Account/Status` — mock |
| FR-112 | Due amount dikhana | PARTIAL | Mock |
| FR-113 | Advanced search (ID/naam/type/date/contact) | PARTIAL | `Retail/Search.cshtml` UI hai, logic nahi |

## L. Status lifecycle

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-120 | Active / Temp Inactive / Perm Inactive | PARTIAL | Strings hain, transition logic nahi |
| FR-121 | Bill ke hisaab se status change | **MISSING** | 0 files |
| FR-122 | Sirf postpaid connections | PARTIAL | Koi prepaid code nahi, lekin enforcement bhi nahi |

## M. Other

| Req ID | Requirement | Status | Kahan / Detail |
|---|---|---|---|
| FR-130 | Customer feedback collect | PARTIAL | Form hai, submit sirf log karta hai |
| FR-131 | System time-to-time update | N/A | Ongoing requirement |
| FR-132 | City-wise records | **MISSING** | 0 files |

---

## Summary

| Status | Count | % |
|---|---|---|
| **MISSING** | 34 | ~64% |
| **FAKE** | 8 | ~15% |
| **PARTIAL** | 16 | ~30% |
| **DONE** | 1 | ~2% |
| **N/A** | 1 | — |
| **Total** | 53 | |

**Nateeja:** SRS ka ~64% backend bilkul nahi hai, aur ~15% sirf dikhawa hai.
Sirf 1 requirement poora hua hai.

## Sab se bara gap — priority order

1. **Database + domain models** — kuch bhi real nahi hai
2. **Authentication + Authorization** — 5 roles, sab public hain
3. **ID generation** — Order ID aur Account ID formats SRS se match nahi karte
4. **Plans & charges** — poora pricing SRS se match nahi karta (PKR vs USD)
5. **Bill generation + tax + discount**
6. **Feasibility workflow**
7. **Admin CRUD** — vendor, stock, employee, retail shop
8. **Connection lifecycle**

## Do bara mismatch — Zain se poochna hai

1. **Currency:** SRS mein USD ($), code mein PKR (Rs. 1,200 / 2,000 / 3,500).
   Kaunsa rakhna hai? PKR rakhna ho to SRS ke amounts convert karne parenge
   ya naye rates daalne parenge.
2. **Plans ka structure:** SRS ke plans **hourly + Kbps unlimited** hain.
   Code ke plans **BASIC/STANDARD/PREMIUM Mbps** hain — ye kisi aur source se
   aaye hain. Inko SRS ke mutabiq dobara banana padega.
