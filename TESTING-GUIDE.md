# NEXUS — Testing Guide (team ke liye)

Ye guide us bande ke liye hai jo testing karega. Har feature ke saath: **kya test
karna hai, kaise, kya expect karna hai, aur kya galat lagna chahiye.**

Aakhri update: 12 Oct 2026

> **Ye manual guide hai.** Automated testing ke liye **`TESTSPRITE-PLAN.md`** dekho —
> usme TestSprite ka setup, 49 end-to-end journeys, aur Aptech deliverable mein
> kya jama karna hai likha hua hai.
>
> Dono ek doosre ke replacement nahi:
> - **TestSprite** — user journeys (login, authorization, CRUD, error pages)
> - **Ye guide** — exact numbers (billing arithmetic, tax, discount tiers)

---

## 0. Pehle ye karwao — setup

```cmd
cd "C:\Users\Zain Ansari\source\repos\Zain-developer-ui\Naxus-internet-service-mangment"
"C:\Program Files\dotnet\dotnet.exe" build
"C:\Program Files\dotnet\dotnet.exe" run --urls http://localhost:5199
```

Phir browser: `http://localhost:5199`

**⚠️ Agar app chalti hui magar login POST fail ho jaye** — pehle SQL Server check
karo, password mat badlo. 12 Oct ko yehi hua tha: error
`Connection Timeout Expired ... [Post-Login] complete=13079` tha, aur SQL Server
theek chal raha tha. Ye **transient network timeout** tha, ghalat password nahi.

### Login accounts

| Role | Username | Password | Kahan jaata hai |
|---|---|---|---|
| Admin | `admin@nexus.example` | `Nexus@2027` | `/Admin/Dashboard` |
| Retail | `R0000000000001` | `Nexus@2026` | `/Retail/Dashboard` |
| Technical | `T0000000000001` | `Nexus@2026` | `/Technical/Dashboard` |
| Accounts | `F0000000000001` | `Nexus@2026` | `/Accounts/Dashboard` |
| Customer | `B042000000000001` | (register kar ke banao) | `/Customer/Dashboard` |

**Admin ka password `Nexus@2027` hai, `2026` nahi.** Baaki staff `2026`.

### DB counts (test se pehle aur baad mein)

```cmd
"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd" -S "DESKTOP-BD6OLPC\SQLEXPRESS" -U sa -P aptech -d NexusDb -Q "SET NOCOUNT ON; SELECT 'Users' T, COUNT(*) N FROM Users UNION ALL SELECT 'Customers',COUNT(*) FROM Customers UNION ALL SELECT 'Orders',COUNT(*) FROM ConnectionOrders UNION ALL SELECT 'Connections',COUNT(*) FROM Connections UNION ALL SELECT 'Bills',COUNT(*) FROM Bills UNION ALL SELECT 'Payments',COUNT(*) FROM Payments UNION ALL SELECT 'CallUsages',COUNT(*) FROM CallUsages;"
```

**Baseline:** Users 6 · Customers 2 · Orders 2 · Connections 1 · Bills 0 ·
Payments 0 · CallUsages 0

Test ke baad apne banaye rows **delete** karo — baseline par wapas lao.

---

## 1. Features jo POORE ho chuke hain — inhe test karo

### A. Authentication aur roles

| # | Test | Kaise | Expected |
|---|---|---|---|
| A1 | Galat password reject | Login: `admin@nexus.example` + `wrongpass` | Error "Invalid credentials", login nahi hota |
| A2 | Galat username ka same error | `nobody@x.com` + koi bhi | **Wahi** error (pata na chale user hai ya nahi) |
| A3 | 5 galat try → lockout | 5 dafa galat password | 6th par "account locked" |
| A4 | 5 roles ka apna dashboard | 5 accounts se login | Har ek apne dashboard par (table dekho) |
| A5 | Role galat page na khole | Customer se `/Admin/Dashboard` | 403 ya AccessDenied page |
| A6 | Guest protected page na khole | Logout kar ke `/Admin/Dashboard` | 302 → login page |
| A7 | Logout POST hai | Logout button dabao | Session khatam, wapas login |
| A8 | Password change | `/Profile` → change password | Naya password chalta hai, purana nahi |
| A9 | Same password reject | Naya = purana | Error |

**Cross question:** *"Ek customer doosre customer ka bill dekh sakta hai?"* → **Nahi.**
`BillsController` claims se customer ID leta hai, URL ka ID ignore karta hai.

### B. Registration + order (ek flow)

| # | Test | Kaise | Expected |
|---|---|---|---|
| B1 | Naya customer | `/Orders/New` — form bharo | 302 → Customer Dashboard, login ho gaya |
| B2 | Account ID format | Naya customer banao, DB dekho | `B0420000000000XX` — **16 characters** (B + 042 city + 12 serial) |
| B3 | Order ID format | DB `ConnectionOrders` dekho | `B0000000003` — **11 characters** (B + 10 digits) |
| B4 | Duplicate CNIC block | Wahi CNIC dobara | Error "CNIC already registered" |
| B5 | Duplicate phone block | Wahi phone | Error |
| B6 | Duplicate email block | Wahi email | Error |
| B7 | **Dial-Up ko landline chahiye** (SRS rule V1) | Dial-Up plan select karo, landline khaali chhodo | Error — "landline required" |
| B8 | Telephone ko bhi landline chahiye | Telephone plan, landline khaali | Error |
| B9 | Broadband ko landline ki zaroorat nahi | Broadband, landline khaali | Chal jata hai |
| B10 | Plan filter type ke hisaab se | Broadband select karo | Sirf Broadband plans dikhein |
| B11 | Plan auto-select | Form kholo | Pehla plan khud selected |
| B12 | Order summary live update | Plan badlo | Right panel ka summary badle |
| B13 | Billing cycle POST hota hai | Cycle badal kar submit karo | DB mein wahi cycle save ho |

**Cross question:** *"Registration aur order alag hain?"* → **Nahi, ek transaction
hain.** `/Orders/New` teen tables likhta hai: `Users` → `Customers` → `ConnectionOrders`.

### C. Feasibility workflow

| # | Test | Kaise | Expected |
|---|---|---|---|
| C1 | Survey queue | `/Operations` | Sirf wo orders jahan kaam pending (4 statuses) |
| C2 | Distance pass | Order review → survey, distance 2 km | `FeasibilityPassed` |
| C3 | Distance fail | distance 8 km | `FeasibilityFailed` + reason |
| C4 | Landline missing | Dial-Up order, landline number khaali | Fail — "landline required" |
| C5 | Confirm → Install → Complete | Order review se teeno | Status badle: `Confirmed` → `Installing` → `Completed` |
| C6 | Complete se connection bane | Complete karo, `/Operations/Connections` | Nayi line `CON-B-000002` |
| C7 | Status history likhe | DB `ConnectionStatusHistory` | Har move ki row |

**Cross question:** *"Feasibility ka faisla kaun karta hai?"* → **Rules, officer nahi.**
`FeasibilityRules` distance aur landline check karta hai. Officer sirf numbers
daalta hai, result service nikalta hai.

### D. Connection lifecycle

| # | Test | Kaise | Expected |
|---|---|---|---|
| D1 | Pending → Active | `/Operations/Connections` → line kholo → Active | Status badla, `ActivatedOn` stamp |
| D2 | Active → Temp Inactive | Status change, reason likho | Badla, history mein row |
| D3 | Temp → Perm Inactive | Wahi | Badla |
| D4 | Perm → Active **na** ho | Perm Inactive se Active try karo | Error — move legal nahi |
| D5 | Reason ke baghair na ho | Reason khaali | Error "reason required" |
| D6 | Sirf valid moves dikhein | Line kholo | UI sirf legal moves dikhaye |

### E. **Overdue policy** (naya — SRS ka core rule)

| # | Test | Kaise | Expected |
|---|---|---|---|
| E1 | Screen khule | `/Operations/Overdue` | 200, policy explain kare |
| E2 | Koi overdue na ho | Normal haalat | "Every line is inside its payment window" |
| E3 | **Overdue bill banao** | neeche SQL dekho | Preview mein line dikhe |
| E4 | 15–44 din overdue | Bill `DueOn` = aaj − 20 din | Move: `Active → Temporarily Inactive` |
| E5 | 45+ din overdue | Bill `DueOn` = aaj − 60 din | Move: `Active → Permanently Inactive` |
| E6 | 14 din overdue | Bill `DueOn` = aaj − 14 din | **Kuch nahi** (grace ke andar) |
| E7 | Paid bill ignore ho | `AmountPaid = TotalAmount` | Kuch nahi |
| E8 | Draft bill ignore ho | `Status='Draft'` | Kuch nahi |
| E9 | Apply karo | Button dabao | Status badle, `DeactivatedOn` stamp |
| E10 | History likhe | DB `ConnectionStatusHistory` | Row with reason "Payment outstanding for N days..." |

**Test bill banane ka SQL** (connection id apne DB se lo):

```sql
INSERT INTO Bills (BillNumber,CustomerId,ConnectionId,PeriodStart,PeriodEnd,
  IssuedOn,DueOn,Status,SubTotal,DiscountAmount,TaxableAmount,ServiceTaxAmount,
  TotalAmount,AmountPaid,CreatedAt)
VALUES ('TEST-OVERDUE',15,14,
  DATEADD(day,-90,GETUTCDATE()), DATEADD(day,-60,GETUTCDATE()),
  DATEADD(day,-70,GETUTCDATE()), DATEADD(day,-60,GETUTCDATE()),
  'Issued',175.00,0,175.00,21.42,196.42,0,SYSUTCDATETIME());
```

> **Zaroori:** `Status='Issued'` likho, `Status=1` **nahi**. Poore project mein enums
> DB mein **strings** hain. Number likha to row ban jayegi magar code usay dhoond
> nahi payega.

**Test ke baad saaf karo:**
```sql
DELETE FROM ConnectionStatusHistory WHERE ConnectionId=14 AND ToStatus='PermanentlyInactive';
UPDATE Connections SET Status='Active', DeactivatedOn=NULL WHERE Id=14;
DELETE FROM Bills WHERE BillNumber='TEST-OVERDUE';
```

**Cross question:** *"Ye apne aap hota hai?"* → **Nahi, jaan boojh kar.** Line dead
karna wo tabdeeli hai jo operator pehle dekh le. Is liye preview screen hai —
wahi query preview aur apply dono chalati hai.

### F. Billing

| # | Test | Kaise | Expected |
|---|---|---|---|
| F1 | Bill generate | `/Bills` → generate (ya billing run) | Bill bane, `INV-2026-00001` |
| F2 | Tax 12.24% | Bill kholo | Service Tax line dikhe |
| F3 | Tax **discounted** amount par | Discount wala customer | Tax gross par nahi |
| F4 | Bulk discount tiers | 10+ connections | 25% / 50% / 75% / 100% |
| F5 | Boundary: 9 → 0% | 9 connections | Discount nahi |
| F6 | Boundary: 10 → 25% | 10 connections | 25% |
| F7 | Boundary: 50 → 100% | 50 connections | 100% (bill zero, tax negative nahi) |
| F8 | Payment record | Bill detail → payment | Status `PartiallyPaid` / `Paid` |
| F9 | Overpayment reject | Amount > balance | Error |
| F10 | Receipt number | Payment ke baad | `RCP-202610-00001` |
| F11 | CSRF | Token ke baghair POST | **400** |
| F12 | IDOR | Customer se doosre ka bill | 403 / AccessDenied |
| F13 | Cancel bill | Draft/issued cancel | Reason lazmi |

### G. Admin CRUD

| # | Test | Kya check karo |
|---|---|---|
| G1 | Plans | `/Admin/Plans` — create/edit/toggle/delete |
| G2 | Plan delete guard | Jis plan pe order ho, wo delete na ho — "retire karo" |
| G3 | Cities | `/Admin/Settings/Cities` — create/edit/toggle/delete |
| G4 | City code lock | Jis city mein customer ho, uska code lock ho |
| G5 | Cities "Stop serving" toggle | Off karo, phir **wapas on** karo — dono dafa toast |
| G6 | Discount slabs | `/Admin/Settings/Discounts` — create/edit |
| G7 | Slab overlap block | Do active slabs ek hi range | Error |
| G8 | Website settings | `/Admin/Settings` — kuch badlo, save | Save ho, reload par wahi value |
| G9 | Settings: checkbox | Feature on karo → save → reload | On rahe |
| G10 | Settings: checkbox off | Feature off karo → save → reload | Off rahe |
| G11 | Vendors | `/Admin/Vendors` — create/delete |
| G12 | Stock | `/Admin/Inventory` — issue/return |
| G13 | **Negative stock block** | 10 on hand, −999 nikalo | Error "Only 10 on hand" |
| G14 | Stock reason lazmi | Reason khaali | Error |
| G15 | Outlets + Staff | `/Admin/Organisation` |
| G16 | Employee double-link block | Ek login do employees se | Error |

### H. Public pages

| # | Test | Expected |
|---|---|---|
| H1 | Home | 200, **koi fake number nahi** (1245 customers etc. nahi hone chahiye) |
| H2 | Plans | 10 asli plans DB se |
| H3 | Plan filter | Broadband / Dial-Up / Telephone |
| H4 | Services | 200, live rates |
| H5 | Order tracking | `/Orders/Tracking` — asli order id daalo | Timeline real status se |
| H6 | Account status | `/Account/Status` — account id | Asli data, due amount |
| H7 | Feedback | Submit karo | DB `Feedback` mein row |
| H8 | Contact form | Submit karo | DB mein row |
| H9 | Privacy | `/Home/Privacy` | **200** (404 nahi) |

### I. Reports

| # | Test | Expected |
|---|---|---|
| I1 | `/Reports` chaaron roles se | 200 sab ke liye |
| I2 | KPIs real | DB counts se match karein |
| I3 | Filters | Date range kaam kare |
| I4 | CSV export | `/Reports/ExportCustomers` — `text/csv` |
| I5 | Guest | 302 → login |

---

### J. Advanced search (SRS ke 5 filters)

Route: `/Search/Advanced` — chaaron staff roles ke liye.

| # | Test | Kaise | Expected |
|---|---|---|---|
| J1 | Khaali query | Page kholo, kuch na bharo | "Enter at least one filter" — poori book **nahi** |
| J2 | Order id se | `Id` = `B0000000001` | Wahi order |
| J3 | Account id se | `Id` = `B042000000000002` | Us customer ki line |
| J4 | Line number se | `Id` = `CON-B-000001` | Wahi line |
| J5 | Naam se | `Name` = `Bilal` | 1 order + 1 line |
| J6 | Type se | `Type` = Broadband | Sirf Broadband |
| J7 | Contact se | `Contact` = `0321` | Us number wale records |
| J8 | Date range — match | `From` = 2026-10-01, `To` = 2026-10-31 | Sab October ke |
| J9 | Date range — no match | `From` = 2026-01-01, `To` = 2026-01-31 | "Nothing matches those filters" |
| J10 | Kuch na mile | `Name` = `ZZZNobody` | "Nothing matches those filters" |
| J11 | Combobox: type + naam | Dono bharo | Sirf wo jo dono se match karein |
| J12 | Clear button | Filter lagao, phir Clear | Form khaali, results gayab |
| J13 | URL shareable | Filter lagao, URL copy, naye tab mein kholo | Wahi results |
| J14 | Guest | Logout kar ke kholo | 302 → login |
| J15 | Customer role | Customer se kholo | AccessDenied (403) |
| J16 | Sidebar | Page kholo | Sirf "Advanced Search" active |

**Cross question:** *"Date filter kis date par lagta hai?"* → **Dono par, apni apni.**
Orders `CreatedAt` (application date) par, lines `ActivatedOn` (received date) par.
Ye SRS ke *"date or period of application or received the connection"* ka seedha
matlab hai. Aur `To` date **poora din** cover karti hai.

### K. Retail till-date register

Route: `/Retail/Register` — Retail aur Admin ke liye.

| # | Test | Kaise | Expected |
|---|---|---|---|
| K1 | Page khule | Retail se login → sidebar "Till Date Register" | 4 sections: Orders / Connections / Billing / Payments |
| K2 | All time | Bina filter kholo | Sab records, header "All time" |
| K3 | Period filter | `From` = 2026-10-01, `To` = 2026-10-31 | Sirf October |
| K4 | Khaali period | `From` = 2026-01-01, `To` = 2026-01-31 | Har section apna "No … in this period" |
| K5 | Totals | Filter lagao | Orders placed / Lines activated / Billed / Collected update hon |
| K6 | Billed vs Collected | Ek bill ke saath | `Billed` total, `Collected` sirf paid, `outstanding` difference |
| K7 | Guest | Logout kar ke kholo | 302 → login |
| K8 | Technical role | Technical se kholo | AccessDenied (sirf Retail + Admin) |
| K9 | Sidebar | Page kholo | Sirf "Till Date Register" active (ek hi) |
| K10 | URL shareable | Filter lagao, URL copy, naye tab | Wahi result |

**Cross question:** *"Retail register order kab dikhata hai, line kab?"* → Order
`CreatedAt` par — jis din customer ne apply kiya. Line `ActivatedOn` par — jis din
activate hui. Dono alag column hain is liye ek hi period mein order aa sakta hai
aur line nahi (ya ulta).

### L. Purchase orders (vendor se equipment mangwana)

Route: `/Admin/PurchaseOrders` — sirf Admin.

| # | Test | Kaise | Expected |
|---|---|---|---|
| L1 | List khule | Admin → sidebar "Purchase Orders" | Orders + totals (on record / awaiting / received / committed) |
| L2 | Naya PO | "New Purchase Order" → vendor + 2 lines → Raise | 302 → Details, number `PO-2026-000N` |
| L3 | Numbering | Do PO banao | Serial barhe, duplicate nahi |
| L4 | Vendor zaroori | Blank vendor chhor ke submit | Field error "Pick an active vendor." |
| L5 | Khaali lines | Bina line submit | "Add at least one line to the order." |
| L6 | Duplicate line | Ek product do lines par | "'…' appears on more than one line." |
| L7 | Line total | Qty × cost | Line amount + order total live update |
| L8 | Cost auto-fill | Product chuno | Unit cost catalogue se bhar jaye |
| L9 | Receive | Details → outlet chuno → Receive | Stock badhe, lines 100%, status "Received" |
| L10 | Stock ledger | Receive ke baad stock screen | Movement row `Received on PO-…` |
| L11 | Double receive | Dobara Receive | Error "already been received", stock **nahi** badhe |
| L12 | Received cancel | Received PO cancel karo | Error "Received stock cannot be un-done…" |
| L13 | Cancel | Ordered PO cancel karo | Status "Cancelled" |
| L14 | Cancelled receive | Cancelled PO receive karo | Error "A cancelled order cannot be received." |
| L15 | Filter | Status / vendor filter | Sirf matching rows |
| L16 | Guest | Logout kar ke kholo | 302 → login |

**Cross question:** *"Stock kab barhta hai?"* → Sirf **receive** par. Order raise
karna sirf commitment hai — jab tak maal nahi aata, shelf par kuch nahi jata.

### M. Call metering (landline call charges)

Route: `/Accounts/Calls` — sirf Accounts role.

Pehle ek Telephone line honi chahiye (plan "Telephone Line", jo
`Local - monthly` tariff ke saath seed hota hai). Koi line na ho to form chhup
jata hai aur "No metered lines are available" message aata hai — ye bhi L/M0
test hai.

| # | Test | Kaise | Expected |
|---|---|---|---|
| M0 | Koi meterable line nahi | Bina Telephone connection kholo | Form chhupa, saaf message |
| M1 | Page khule | Accounts → sidebar "Call Metering" | Reading form + readings table |
| M2 | Reading save | Line + period + minutes → Save | 302, row table mein, badge "Pending" |
| M3 | Tariff preview | Line chuno | Tariff box `Local - monthly` dikhaye |
| M4 | Same period dobara | Wahi line + period, nayi minutes | Row **replace** ho, duplicate **na** bane |
| M5 | Khaali line | Line select na karo | "Pick a landline." |
| M6 | Saare minutes 0 | Teeno 0 chhor ke save | "Enter at least one minute figure." |
| M7 | Ulta period | End < Start | "period end cannot be before its start." |
| M8 | Ghair-number | Minutes mein `abc` | Binder error, form wapas (data save na ho) |
| M9 | Bill par charges | Reading save karo, phir us period ka bill banao | Bill par "Local calls" line, 120 min × $0.75 = **$90.00** |
| M10 | Billed lock | DB mein `IsBilled=1` karo, page reload | Badge "Billed", koi Remove button nahi |
| M11 | Delete | Unbilled row → Remove | 302, row gayab |
| M12 | Guest | Logout kar ke kholo | 302 → login |
| M13 | Tariff set | Admin → Plans → Telephone plan Edit | "Call Tariff" dropdown, 5 rates |
| M14 | Tariff save | Tariff badal ke save | Save ho, plan list par wapas |
| M15 | Tariff meter-only | Broadband plan par tariff chuno | Field **chhup** jaye; save par DB mein null |

**Cross question:** *"Call charges bill par kaise aate hain?"* → Accounts
exchange ka meter reading enter karti hai; us line ke plan ka tariff
(`CallTariffLabel`) utha kar `minutes × rate` ka line banta hai. Reading na ho
to bill sirf rental dikhati hai — guessed figure **nahi** ghadti.

**Cross question:** *"Local-monthly mein STD calls kyun nahi dikhte?"* → Us
tariff mein STD rate `0` hai (SRS), aur `0` rate wali line jaan-boojh kar skip
hoti hai. Galat rate se charge karna se behtar hai line na banana.

### N. Security headers + rate limiting

| # | Test | Kaise | Expected |
|---|---|---|---|
| N1 | Headers | Kisi bhi page par browser DevTools → Network → Response Headers | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy` |
| N2 | CSP | Response header `Content-Security-Policy` | `default-src 'self'`, `script-src` mein nonce, `frame-ancestors 'none'` |
| N3 | Nonce match | Inline `<script>` ka `nonce` vs header ka nonce | **Dono bilkul same** |
| N4 | External scripts | `<script src="…">` | Nonce **nahi** hona chahiye |
| N5 | Login throttle | Login par 11+ baar ghalat password try karo | 10 ke baad "Too many sign in attempts" error |
| N6 | Throttle reset | Ek minute ruk kar dobara try | Normal login chale |

**Cross question:** *"Nonce kya hai aur kyun?"* → Har request par ek random
token banta hai jo CSP header aur asli inline scripts dono mein hota hai.
Attacker ka inject kiya hua `<script>` token guess nahi kar sakta, is liye
browser usay block kar deta hai — lekin hamare apne scripts chalte rehte hain.

**Cross question:** *"`style-src` mein `'unsafe-inline'` kyun hai?"* → Kyunke
kuch purane views `style=""` attributes use karte hain (Status page, Tracking
page). Ye ek jaana-boojha gap hai; inline styles saaf hone par isay bhi hata
denge. `script-src` par `'unsafe-inline'` **nahi** hai — wahan sirf nonce chalta hai.

## 2. Cross questions — jo examiner pooch sakta hai

### Architecture

**Q: Database kaunsi hai?**
SQL Server 2022 Express, DB `NexusDb`, 24 tables. EF Core 8 **Code First** —
C# entities se tables banti hain, `Data/Migrations/` mein migrations hain.

**Q: Kitni tables hain?**
24 (plus EF ka `__EFMigrationsHistory`). Users, Customers, ConnectionOrders,
Connections, ConnectionStatusHistory, FeasibilityChecks, Plans, PlanPrices,
Bills, BillLines, Payments, CallUsages, Cities, BulkDiscountTiers,
EquipmentProducts, StockItems, StockMovements, Vendors, PurchaseOrders,
PurchaseOrderLines, Employees, RetailShops, Feedback, SiteSettings.

**Q: Enums kaise store hote hain?**
**Strings** mein (`HasConversion<string>()`). Yani `Status` column mein `'Active'`
jata hai, `1` nahi. Isi liye raw SQL se test data daalte waqt string likhna parta hai.

### Security

**Q: Password kaise store hota hai?**
.NET 8 ka `PasswordHasher` — PBKDF2, **210,000** iterations, per-user salt. Plain
text kahin nahi.

**Q: CSRF se kaise bachate ho?**
Har POST par `[ValidateAntiForgeryToken]` — **50** actions par. Token ke baghair
400 aata hai (test karke dikhao).

**Q: SQL injection?**
Poora EF Core use hota hai — parameterised queries. Koi raw SQL string nahi
(migration ke ilawa).

**Q: Authorization kaise?**
5 roles: Admin, Accounts, Technical, Retail, Customer. Har dashboard controller
par `[Authorize(Roles=...)]` — **17** attributes, **0** `AllowAnonymous`.

**Q: IDOR (doosre ka data)?**
`BillsController` aur `CustomerController` claims se customer ID lete hain, URL ka
ID ignore karte hain. Customer A customer B ka record nahi khol sakta.

**Q: Session kaise?**
Cookie auth — `NEXUS.Auth`, `HttpOnly` (JS padh nahi sakta), `SameSite=Lax`,
60 minute, no sliding.

**Q: Brute-force (password guessing) rok kyun?**
Login par per-IP rate limit — **10 attempts / minute**. Limit cross hone par
`429`. Test: 11+ baar ghalat password maaro (section N, test N5).

**Q: XSS ka kya?**
CSP header `script-src` mein sirf `'self'` aur ek per-request nonce. Injected
`<script>` block ho jata hai kyunke usay nonce nahi milta. Razor by default
HTML encode bhi karta hai.

**Q: Clickjacking?**
`X-Frame-Options: DENY` + CSP `frame-ancestors 'none'` — site kisi iframe mein
load nahi hoti.

### Business logic

**Q: Order ID ka format?**
11 characters — pehla letter type (`D` dial-up, `T` telephone, `B` broadband) +
10 digits serial. Jaise `B0000000001`.

**Q: Account ID ka format?**
16 characters — 1 letter type + **3 digit city code** + **12 digit serial**.
Jaise `B042000000000001` (B = broadband, 042 = Lahore).

**Q: Tax kitna?**
12.24% — SRS ke mutabiq. Aur **discounted amount par** lagta hai, gross par nahi.

**Q: Bulk discount kaise?**
Customer ke **live connections** count se: 10-15 → 25%, 15-25 → 50%,
25-50 → 75%, 50+ → 100%. Discount customer ke poore account par.

**Q: Feasibility mein kya check hota hai?**
Distance (km), server capacity, aur Dial-Up/Telephone ke liye landline ka hona.

**Q: Connection status kab badalta hai?**
Do tareeqe: (1) officer manually change kare, (2) **bill ke overdue hone par** —
15 din baad suspend, 45 din baad close. Har move `ConnectionStatusHistory` mein likhi jati hai.

**Q: Currency kaunsi?**
USD ($) — SRS ke mutabiq. Security deposit: Dial-Up $325, Broadband $500,
Landline $250.

### Common traps (examiner ye pakad sakta hai)

**Q: Ye number static hai ya DB se?** → Page kholo, DB count badlo, reload karo.
Number badalna chahiye. (Customer Dashboard par kuch static cheezein hain — audit
file dekho.)

**Q: Do roles ek page kholein to kya hoga?** → Customer `/Admin/Dashboard` →
AccessDenied. Dikhao.

**Q: Form ke baghair POST karo?** → 400 (CSRF). Dikhao.

**Q: Galat data daalo?** → Server-side validation error, crash nahi.

---

## 3. Bug report ka format

Jab bug mile to aise likho — warna fix karne wala guess karega:

```
Feature:      Overdue policy
Page/URL:     /Operations/Overdue
Steps:        1. ... 2. ... 3. ...
Expected:     ...
Actual:       ...
Screenshot:   (agar ho)
DB counts:    (test se pehle/baad)
```

---

## 4. Jo cheezein ABHI test na karo (kaam baqi hai)

Ye features abhi **adhoori** hain — inmein bug mile to wo naya nahi hai:

| Feature | Kya baqi |
|---|---|
| First payment adjust | Pehli payment billing mein adjust nahi hoti |
| Replacement charges | Line item hai, charge lagane ka flow nahi |
| Technical equipment maintain | Abhi Admin/Retail ke paas hai, Technical ke paas nahi |
| City + year wise records | Filing structure nahi hai |

**Poori list:** `STATIC-DATA-AUDIT.md` aur `PLAN-4-DAYS.md`

> **Note:** Customer My Connection, Advanced search, Overdue policy, **Retail
> till-date register (section K)**, **purchase orders (section L)**,
> **call metering (section M)** aur **security headers + rate limiting
> (section N)** ab **poori tarah kaam karte hain** — inhe test karo.

---

## Related

- `PLAN-4-DAYS.md` — kya banana baqi hai
- `STATIC-DATA-AUDIT.md` — kahan kahan static data hai
- `TASK-LEDGER.md` — poora kaam ka record
