# TestSprite Test Register — NEXUS

Project `c0ab2de3-d588-4213-ba76-6a119dae1909` · environment `default` → `http://localhost:5199`  
48 tests, `plans.jsonl` se banaye gaye (specIndex = file ki line − 1).

Ye register Aptech ke **6.5 Unit Testing Check List** deliverable ke liye seedha  
use ho sakta hai.

---

# Latest status — 13 Oct 2026

**Dashboard: 40 passed · 6 failed · 3 blocked** (49 tests)  
Pehle: 38 passed · 6 failed · 5 blocked

## Jo fix ho kar pass ho gaye

| Test                               | Pehle   | Ab                     | Fix                                   |
| ---------------------------------- | ------- | ---------------------- | ------------------------------------- |
| TC-044 Customer profile update     | blocked | ✅ passed               | **Naya Edit Profile page**            |
| TC-031 Search by account id        | failed  | ✅ passed               | Sahi account ids `B042000000000001/2` |
| TC-047 Required fields block order | failed  | ✅ passed               | Wizard samajh kar step-wise test      |
| TC-040 Call metering               | failed  | ✅ passed               | Empty state ko valid maana            |
| TC-024 Product created             | SQL 500 | ✅ **product ban gaya** | Migration `EquipmentPreferredVendor`  |

## Jo ab bhi baqi hain

| Test                            | Natija  | Asli wajah                                                    |
| ------------------------------- | ------- | ------------------------------------------------------------- |
| TC-023 Vendor create            | failed  | ✅ **wajah mili** — implicit required (FIXED)                 |
| TC-025 PO → stock               | failed  | ✅ **wajah mili** — line regex `__i__` (FIXED)                |
| TC-028 Retail order             | failed  | ✅ **wajah mili** — client/server phone format (FIXED)        |
| TC-026 Outlet + staff           | failed  | 🔴 **Asli gap** — staff login banane ka UI nahi               |
| TC-024 preferred vendor display | failed  | 🔴 **Asli gap (FIXED)** — list mein Vendor column nahi tha    |
| TC-035 Stock adjust             | blocked | **Koi stock hi nahi** (`StockMovements` = 0)                  |
| TC-032 Survey queue             | failed  | Koi pending order nahi (order create fail hone ki wajah se)   |
| TC-049 Mobile sidebar           | blocked | TestSprite harness viewport resize nahi kar pata              |
| TC-001 (purana test)            | blocked | Delete ho gaya                                                |

---

## P0 — 14 tests · **14/14 PASSED** (12 Oct 2026)

| #  | Test ID                                | Test                                | Natija   |
| -- | -------------------------------------- | ----------------------------------- | -------- |
| 7  | `9e5c2134-6b3d-4f7b-ad1c-8d206a7b87bf` | Ghalat password reject, crash nahi  | ✅ passed |
| 8  | `16435f3b-2bce-447d-8b21-b9b8e142e7b4` | Admin → `/Admin/Dashboard`          | ✅ passed |
| 9  | `cbcab517-4234-4398-9d8a-13a874957a38` | Retail → `/Retail/Dashboard`        | ✅ passed |
| 10 | `fd0881c9-eda5-4662-a7a0-39592f53a93c` | Technical → `/Technical/Dashboard`  | ✅ passed |
| 11 | `498b37ac-3f00-4066-9b57-42d0314eeec5` | Accounts → `/Accounts/Dashboard`    | ✅ passed |
| 12 | `7ac429e5-668a-456f-8712-b79ebc1a44df` | Logout ke baad protected page block | ✅ passed |
| 13 | `1f796ad2-3ae4-4da5-9147-784fb12ff5dd` | Bina login protected page block     | ✅ passed |
| 14 | `85169815-4265-48e4-bbea-ebe28e5a1cf4` | Retail → admin screens refused      | ✅ passed |
| 15 | `f52cf75c-579f-400a-83c5-b3aebe2eb0b0` | Technical → admin screens refused   | ✅ passed |
| 16 | `f689b181-4a43-4ba9-bbc1-04a20b9f62f3` | Accounts → plan management refused  | ✅ passed |
| 17 | `7bbc18fd-29a8-4ceb-8f72-8bdde535c016` | Customer → staff screens refused    | ✅ passed |
| 39 | `4fc1a447-cced-44ee-954b-aedb1e5b9140` | Customer sirf apna data dekhe       | ✅ passed |
| 43 | `f04678b8-37fe-4391-85c4-6d9980ed9858` | SQL injection search mein           | ✅ passed |
| 44 | `0da43006-851a-4056-a8c5-0990f3b88e29` | XSS form field mein                 | ✅ passed |

---

## P1 — 14 tests · **12/14 passed, 2 failed** (12 Oct 2026)

| #  | Test ID                                | Test                               | Natija                                 |
| -- | -------------------------------------- | ---------------------------------- | -------------------------------------- |
| 0  | `54df36f0-8eb6-4c61-9705-517b491161ac` | Home page asli plans dikhaye       | ✅ passed                               |
| 1  | `176624f7-f618-4e11-a9dc-456575336dcb` | Plans page — teen connection types | ✅ passed                               |
| 3  | `7ddf27e8-b046-4ca2-b56c-3a90cac0e1e9` | Contact form submit                | ✅ passed                               |
| 5  | `94f65b01-2179-4e83-9d31-3af95adae5fe` | Order tracking — anjaan id         | ✅ passed                               |
| 6  | `22b3e182-31d4-4e87-a4a3-b8d952c07625` | Branded 404 page                   | ✅ passed                               |
| 18 | `2f89ae70-69b7-4a6d-a309-bc5385a64a9b` | Admin naya plan banaye             | ✅ passed                               |
| 23 | `c5c6b727-5045-4b10-b68c-235033413e78` | PO receive → stock barhe           | ✅ passed (737s)                        |
| 26 | `a4eec0ba-86a6-4be9-98ad-b7b8ee39f421` | Retail naya order place kare       | ⚠️ **passed magar order bana hi nahi** |
| 30 | `4bd66c82-25ee-4979-bce7-318adb88db40` | Survey queue + feasibility         | ❌ failed — queue khaali thi            |
| 34 | `40a95fd4-5b03-49eb-9d02-9b6d5f0fd0c6` | Monthly billing run                | ✅ passed                               |
| 35 | `162e8e6b-2569-4b22-9dcd-4cda15f84442` | Bill par service tax               | ✅ passed                               |
| 36 | `1c565a47-5d40-4068-ae55-24b0c7557deb` | Replacement charge raise           | ✅ passed                               |
| 40 | `bab714de-9fa0-4f78-8a93-0bb7184ab4da` | Customer connection + bills pages  | ✅ passed                               |
| 45 | `9f534a10-8e4f-43f5-aa41-2935d7f165ac` | Khaali order form block ho         | ❌ failed — 4-step wizard               |

### Dono failures — app ka bug NAHI, test design ka masla

**TC-032 (survey queue).** Queue khaali thi kyunke **koi pending order hi nahi hai**.  
DB mein sirf 2 orders hain, dono **terminal** states mein:  
`B0000000001 = FeasibilityFailed`, `B0000000002 = Completed` (dono 6 Oct ke).  
Page ne theek kaha "Nothing in the queue". Test ne ghalat maan liya ke pending  
orders mojood hain — **test dependency** ka masla hai.

**TC-047 (order validation).** `/Retail/NewOrder` ek **4-step wizard** hai  
(Service → Customer → Installation → Account). "Next" button `submit` nahi karta,  
`[data-step-next]` handler `forms.js:58-65` sirf **step aage barhata hai**.  
Validation sirf current panel ke `required` fields par lagti hai, aur fail hone par  
`NEXUS.toast('Please complete the current step.')` dikhata hai — jo 4 second mein  
khud gayab ho jata hai.

### ⚠️ TC-026 — false pass

Test **"passed"** hua magar **koi order create nahi hua** (DB mein abhi bhi 2 orders,  
2 customers — dono 6 Oct ke). Yani assertion kamzor thi: 4-step wizard par test  
poora submit karaye baghair hi pass ho gaya.

**Ye failure se bhi zyada khatarnak hai** — green test jo kuch karta hi nahi.  
Assertion sakht karni hogi: order **submit hone ke baad** order id format  
(`B` + 10 digits) aur DB mein naya row hona chahiye.

**Fix:** teenon tests (`26`, `30`, `45`) ke planSteps dobara likhne honge  
(`testsprite test plan put <test-id>`).

---

## P2 — 20 tests · **12 passed, 4 failed, 4 blocked** (12 Oct 2026)

| Test ID                                | Test                               | Natija                                  |
| -------------------------------------- | ---------------------------------- | --------------------------------------- |
| `555dfa1b-019d-4a20-9181-67eab3298aba` | Services page load                 | ✅ passed                                |
| `f1faef6b-cafc-45ad-a794-77aa75e0ece8` | Khaali contact form block ho       | ✅ passed                                |
| `2439b3e0-42b3-429e-8edf-3bfd510bc1f3` | Admin plan edit kare               | ✅ passed                                |
| `ecce1149-73d5-4c98-a263-fd0dcbf4b749` | Call tariff sirf metered plans par | ✅ passed (743s)                         |
| `ff90e802-7b86-40d7-a2fb-b30049355a94` | Admin vendor banaye                | ❌ failed — required fields              |
| `671b52d3-840d-4263-b5c5-16f8f431ff6f` | Product + preferred vendor         | ❌ **failed — SQL error**                |
| `d72e167a-6ad8-4b5f-ac87-ef7c865af5fd` | Outlet + staff add                 | ⛔ blocked — sign-in                     |
| `3a02bdf1-19ca-46cc-8f74-ed017e86406a` | City add + stop serving            | ✅ passed (468s)                         |
| `75076b1b-1d3f-4bb1-b994-60013bcfa847` | Till-date register — 4 tabs        | ✅ passed                                |
| `4af1a4f9-a72b-4930-aaf1-91fa22456b27` | Advanced search filters            | ✅ passed                                |
| `7ab164a0-cab9-4d8b-b323-c4d71ce968d7` | Account id se search               | ❌ failed — ghalat account id            |
| `6637003f-84a0-41d2-8dc2-a1ce7574960b` | Connection status change           | ✅ passed                                |
| `b6d9982b-7861-477a-87a4-689c6fd76241` | Overdue policy screen              | ✅ passed                                |
| `21268d26-8e83-4dc5-bdbb-dde64d5a01da` | Stock adjust                       | ⛔ **blocked — SQL error**               |
| `2982a23c-23ed-437a-9e58-54f6cfd9dc9c` | Charge form validation             | ✅ passed                                |
| `7de2ea13-d34e-49fe-b6e2-56c32ef41e53` | Call metering screen               | ❌ failed — koi landline connection nahi |
| `197d217c-d2f9-4238-b333-3e5fcb5b1990` | Customer feedback submit           | ✅ passed (733s)                         |
| `257032fc-09a4-47b5-be9f-acf7cf722ff0` | Customer profile update            | ⛔ blocked — profile read-only hai       |
| `d21a3390-1b12-4074-b682-118613e69aa1` | Keyboard se skip link              | ✅ passed                                |
| `2351e515-852e-4c51-aa64-e4d1ed0e9e70` | Mobile sidebar open/close          | ⛔ blocked — sign-in                     |

---

# Asli findings

## 🔴 BUG-1 — `PreferredVendorId` column DB mein hi nahi tha (FIXED)

```
Microsoft.Data.SqlClient.SqlException: Invalid column name 'PreferredVendorId'.
```

`EquipmentProducts` table mein column mojood nahi tha, magar EF usay  
INSERT aur SELECT dono mein use kar raha tha. Is liye:

- **Naya equipment product save karna 500 deta tha** (TC-024)
- **Product edit page khulti hi nahi thi** (TC-035) — yani stock adjust bhi block

**Wajah:** P1 ke kaam mein `EquipmentProduct.PreferredVendorId` aur EF  
configuration add hui thi, **magar migration kabhi banayi/apply nahi gayi.**

**Fix (12 Oct):** `20261012155847_EquipmentPreferredVendor` — `int NULL` column,  
index, aur `Vendors` par FK (`SetNull`). Applied aur verified.

**Sabak:** domain class ya EF config badalne ke baad **migration banana aur  
apply karna lazmi hai**. Warna code aur schema alag ho jate hain, aur error  
sirf runtime par aata hai — us screen par jo us column ko chhooti hai.

## ⚠️ GAP-2 — Profile page read-only hai

`Views/Profile/Index.cshtml` mein **koi `<form>` nahi**, aur  
`ProfileController` mein sirf **`Index` action hai** — koi edit/POST nahi.

Yani user apna phone number ya address **badal hi nahi sakta**. Sidebar mein  
"My Profile" link mojood hai, magar wo sirf dikhata hai.

Ye crash nahi hai — feature missing hai. SRS mein profile editing explicitly  
nahi maangi gayi, is liye ye **jaan-boojh kar chhoda gaya** ho sakta hai.  
Magar portfolio-grade project mein editable profile expected hoti hai.

## ✅ Jo failures test design ke the, app ke nahi

| Test                     | Wajah                                                                                                                                    |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| TC-023 vendor            | Form ke required fields poore nahi bhare gaye                                                                                            |
| TC-031 account id search | Ghalat id — `R0000000000001` **retail** ka hai. Customers ke ids `B042000000000001` / `B042000000000002` hain                            |
| TC-040 call metering     | Koi **landline** connection hi nahi hai (dono Broadband hain)                                                                            |
| TC-032 survey queue      | Koi pending order nahi — dono orders terminal states mein                                                                                |
| TC-047 order validation  | 4-step wizard; "Next" submit nahi karta                                                                                                  |
| TC-026, TC-049 blocked   | Sign-in par block — lockout nahi tha (`AccessFailedCount=0`), rate limit bhi nahi (log mein sirf 1 hit). Dobara chalane par pata chalega |

## 🗑️ TC-001 — purana test, delete karna hai

Pehla test (`9ee66dfd-906b-42a7-8fde-96cc12a3b5ae`) ghalat password ke saath


likha gaya tha. Wo ab bhi "blocked at sign-in" dikhata hai. Isay delete kar do:

```
testsprite test delete 9ee66dfd-906b-42a7-8fde-96cc12a3b5ae --confirm
```

---

## Commands

```cmd
testsprite test run --all --project c0ab2de3-d588-4213-ba76-6a119dae1909
testsprite test result <test-id>
testsprite test artifact get <run-id>
testsprite test diff <run-a> <run-b>
testsprite test flaky <test-id>
testsprite usage
```

---

# Extra batch — 22 tests (13 Oct 2026)

`plans-extra.jsonl` se banaye. **specIndex = file ki line − 1.**

## P0 — 8 tests

| # | Test ID | Test |
|---|---|---|
| 0 | `98aa5ff0-c7b6-49d0-b9d5-579735519df1` | **Naya visitor signup** — `/Orders/New` se register |
| 1 | `af09d429-a21a-4b61-8130-939ec1479ab4` | Naya customer login → apna dashboard |
| 3 | `e92127de-5e56-49e2-9956-fc2a04fada0d` | **Service tax 12.24%** verify |
| 4 | `8a7f607a-89d1-4692-9dc6-642745c608f4` | **Discount sirf advance + security par** |
| 5 | `c3f42073-1409-4c38-8f95-7fa7d2d5048f` | **Advance ek hi dafa** adjust ho |
| 14 | `80659c75-7c55-499f-8965-21ddf6dcb42b` | Ghalat current password reject |
| 17 | `9557c665-fd92-458e-ada5-e3c577f615c6` | Sidebar sirf role ke items |
| 20 | `0da1858d-9e8c-4115-9d20-7d4d1dad47a6` | Signed-out par chaaron dashboards block |

## P1 — 8 tests

| # | Test ID | Test |
|---|---|---|
| 2 | `0bb069be-4024-4a57-95de-9bd097661c3f` | Order id 11 char · account id 16 char |
| 6 | `df5c84f1-86df-49f8-b8f2-89ced3fbec6d` | One-off charge agli bill par aaye |
| 8 | `f182d37e-37b6-4840-8e51-98ab20fea945` | Advanced search filters |
| 9 | `fed35eac-71ac-4e78-9c67-f531beeece94` | Order tracking real order |
| 13 | `dce84dfc-5cce-4171-92ef-8efd0a2da35c` | Reports — city-wise + year-wise |
| 15 | `4e6ab4a7-66a5-4ce2-9ead-19a310cf6df5` | Password mismatch reject |
| 19 | `f7fc3816-96dd-47de-b497-f7730f482263` | Double submit → do records na banein |
| 21 | `8a288c9a-fdb5-41b7-a498-cf69ec77916d` | Unknown ids par saaf message |

## P2 — 6 tests

| # | Test ID | Test |
|---|---|---|
| 7 | `b41da8ca-3b16-403a-9f7e-7bb0f3f7b192` | Billed charge cancel na ho sake |
| 10 | `47812438-4c5a-4884-927c-610f620dd260` | Plan active/inactive reversible |
| 11 | `f362a34c-627d-4058-8653-02b1fee89093` | City stop/start |
| 12 | `463faa39-3f03-4966-9f59-f5b06c2afb96` | Discount slabs list + edit |
| 16 | `c6a9b5cf-5efe-41aa-b046-1dd15641e265` | Password form empty block |
| 18 | `8c6d827d-85d9-48c0-bf68-4fbb086ede00` | Topbar account menu open/close |

## Chalane ka tareeqa

**Har run se pehle cleanup** (warna duplicate naam se fail honge):

```cmd
sqlcmd -S DESKTOP-BD6OLPC\SQLEXPRESS -U sa -P aptech -d NexusDb -i testsprite-cleanup.sql
```

**P0 wave:**

```cmd
testsprite test run 98aa5ff0-c7b6-49d0-b9d5-579735519df1 af09d429-a21a-4b61-8130-939ec1479ab4 e92127de-5e56-49e2-9956-fc2a04fada0d 8a7f607a-89d1-4692-9dc6-642745c608f4 c3f42073-1409-4c38-8f95-7fa7d2d5048f 80659c75-7c55-499f-8965-21ddf6dcb42b 9557c665-fd92-458e-ada5-e3c577f615c6 0da1858d-9e8c-4115-9d20-7d4d1dad47a6
```

---

## Test accounts

| Account                       | Password     |
| ----------------------------- | ------------ |
| `admin@nexus.example`         | `Nexus@2027` |
| `retail@nexus.example`        | `Nexus@2026` |
| `technical@nexus.example`     | `Nexus@2026` |
| `accounts@nexus.example`      | `Nexus@2027` |
| `ayesha.siddiqui@example.com` | `Nexus@2026` |
| `bilal.khan@example.com`      | `Nexus@2026` |

## ⚠️ Data mutation

Ye tests **data banate hain** — naye plans, orders, vendors, products, outlets,  
bills, charges. Demo se pehle cleanup karna padega, ya DB ka backup restore karo.

---

# 13 Oct — P0 wave (naye 8 tests): 5 passed · 3 blocked

| Test                            | Status  | Asli wajah                                                    |
| ------------------------------- | ------- | ------------------------------------------------------------- |
| `98aa5ff0` New visitor signup   | passed  | 🔴 **FALSE PASS** — neeche dekho                              |
| `af09d429` New customer login   | blocked | `/Account/Login` → `ERR_EMPTY_RESPONSE` (tunnel drop)         |
| `e92127de` Service tax 12.24%   | blocked | Invoice par **Discount row hi nahi** — test ki ghalti         |
| `8a7f607a` Discount base        | blocked | Wahi                                                          |
| `c3f42073` Advance ek dafa      | passed  | ✅ asli                                                        |
| `80659c75` Wrong password       | passed  | ✅ asli                                                        |
| `9557c665` Sidebar role items   | passed  | ✅ asli                                                        |
| `0da1858d` Signed-out dashboards| passed  | ✅ asli                                                        |

## 🔴 `98aa5ff0` — false pass, `test steps` se pakda

```
step 6   fill '0300-1112223' → Next
step 10  fill '0300-1112223' → Next     ← phir se
step 12  fill '0300-1112223' → Next     ← phir se
step 14  fill '0300-2223334' → Next     ← agent ne doosra number try kiya
step 16  fill '0300-9990001' → Next     ← teesra
step 21/22/25/26   Next → Next → Next → Next
```

Wizard **phone field par phansa raha** — aur test phir bhi "passed" likh diya.

**DB saboot:** `Customers` 3 (koi naya nahi), `Users` 7 (7th meri probe hai).
Aaj ka **sirf ek order** bana — mera probe `B0000000003` (15:20:01).

**Sabak:** `testsprite test steps <test-id>` **passed runs ke liye bhi** chalta hai,
aur yahi false pass pakadne ka tareeqa hai. Status par akela bharosa mat karo.

## `af09d429` — ERR_EMPTY_RESPONSE

App log mein us waqt **koi entry nahi** — request app tak pohanchi hi nahi (tunnel
drop). Us waqt `98aa5ff0` 738 second se wahi tunnel par chal raha tha. **Transient.**

## `e92127de` + `8a7f607a` — test ki ghalti, app ka bug nahi

`Views/Bills/Details.cshtml` line 90:

```cshtml
@if (bill.DiscountAmount > 0)
{
    <div class="totals-row discount">...</div>
}
```

Discount row **sirf tab** dikhta hai jab discount ho. `INV-2026-00001` par
`DiscountAmount = 0` kyunke tier `customer.LiveConnectionCount` se aata hai aur wo
**1** hai — pehla slab **10** se shuru hota hai. Yani **bilkul sahi behaviour**.

Tier table (`Common/Constants/BulkDiscountTiers.cs`, DB table `BulkDiscountTiers`)
seeded hai: 10-15→25%, 15-25→50%, 25-50→75%, 50+→100% — SRS ke mutabiq.

**10+ connections UI se banana practical nahi**, is liye tests dobara likhe:
`steps-tax.json`, `steps-discount.json`, aur naya test `plans-discount-slab.jsonl`.

## Corrected commands

```cmd
testsprite test plan put e92127de-5e56-49e2-9956-fc2a04fada0d --steps testsprite-fixes/steps-tax.json
testsprite test plan put 8a7f607a-89d1-4692-9dc6-642745c608f4 --steps testsprite-fixes/steps-discount.json
testsprite test create-batch --plans plans-discount-slab.jsonl
```

| Naya test                     | ID                                     |
| ----------------------------- | -------------------------------------- |
| Bulk discount slabs (SRS tiers) | `61851289-3bd6-4802-b77a-c0b80df0c2bd` |

---

# 14 Oct — Do gaps band ho gaye

## ✅ TC-026 — Staff login banane ka UI (gap band)

**Masla tha:** employee form ka "Staff Login" dropdown sirf **unlinked** logins
dikhata hai, aur chaaron seed logins pehle se linked the — to dropdown **khaali**
tha. Naya employee add karna namumkin tha.

**Ab:** `/Admin/Organisation/Logins` — naya screen.

| Kya | Kahan |
|---|---|
| List + toggle | `Views/Organisation/StaffLogins.cshtml` |
| Create / edit form | `Views/Organisation/StaffLoginEdit.cshtml` |
| Controller | `OrganisationController` → `StaffLogins`, `CreateStaffLogin`, `EditStaffLogin`, `ToggleStaffLogin` |
| Service | `OrganisationService` → `StaffLoginsAsync`, `GetStaffLoginAsync`, `SaveStaffLoginAsync`, `SetStaffLoginActiveAsync` |
| Sidebar | "Staff Logins" link |

**Account id:** role letter + 13 digits — `A` Admin, `F` Accounts, `T` Technical,
`R` Retail. Table se max+1, counter se nahi.

**Verify:** probe ne `T0000000000002` banaya → us se sign-in → `/Technical/Dashboard`,
aur employee dropdown mein `Probe Operator — Technical (T0000000000002)` aa gaya.

## ✅ Negative bill — advance carry-forward (fix)

**Masla tha:** `INV-2026-00001` ka total **-123.46**. Poora advance (500) ek hi bill
par lag jata tha, chahe period ka kharcha 390 ho.

**Ab:** advance sirf utna absorb karta hai jitna period ka kharcha hai; baqi order par
bacha rehta hai aur agli bill par lagta hai.

| Kya | Kahan |
|---|---|
| `AdvanceApplied` + `AdvanceRemaining` | `Domain/Orders/ConnectionOrder.cs` |
| Capping logic | `BillingService.ApplyAdvanceAsync` |
| Schema | migration `20261014101417_AdvanceApplied` |
| Purana data | migration `20261014101913_AdvanceAppliedBackfill` |

**Verify (asli flow se — Technical install → activate → Accounts billing run):**

```
INV-2026-00002
  Rental  Broadband 30 Hours - rental (1 month)    175.00
  Advance Advance paid at connection              -175.00
  SubTotal 0.00 · Discount 0.00 · Taxable 0.00 · Tax 0.00 · Total 0.00

Order 28 (B0000000005): QuotedAmount 500 · AdvanceApplied 175 · Remaining 325
```

**Baqi:** purana `INV-2026-00001` (-123.46) DB mein mojood hai — purani logic ka
nishan. Connection 14 `PermanentlyInactive` hai, is liye billing run usay dobara
nahi banayegi. Saaf karna ho to:

```cmd
sqlcmd -S DESKTOP-BD6OLPC\SQLEXPRESS -U sa -P aptech -d NexusDb -Q "DELETE FROM BillLines WHERE BillId=6; DELETE FROM Bills WHERE Id=6;"
```
