# NEXUS — Demo Guide (kal ke liye)

## 0. Pehle ye samajh lo (payment ka asli jawab)

**"Payment kaise pata chalta hai ke customer ne de diya?"**

NEXUS **billing management system** hai, payment gateway nahi. Payment
**counter par** hoti hai (retail outlet ya accounts office) — customer cash /
cheque deta hai, aur **staff system mein record karta hai**:

- `BillsController.RecordPayment` (Accounts / Admin / **Retail** — SRS: *"the
  details of the payments will be maintained by the employees of the retail
  outlets and by the accounts department"*)
- Staff bill kholta hai → amount + method (cash/cheque/bank/card) → **receipt
  number auto-generate** hoti hai → bill ka status badal jaata hai:
  `Issued → PartiallyPaid → Paid` (ya `Overdue` due date ke baad)
- Customer apne dashboard par paid status dekhta hai

Cheque par cheque number lazmi; partial payment chalti hai; cancelled/draft
bill par payment block.

---

## 1. Demo se pehle (setup)

1. Server chalao: `dotnet run` (build clean hona chahiye)
2. Login: `A0000000000001` / `Nexus@2027`
3. Demo data check (SQL):
   ```sql
   SELECT 'Users='+CAST(COUNT(*) AS varchar) FROM Users;
   SELECT 'Orders='+CAST(COUNT(*) AS varchar) FROM ConnectionOrders;
   SELECT 'Bills='+CAST(COUNT(*) AS varchar) FROM Bills;
   SELECT 'Settings='+CAST(COUNT(*) AS varchar) FROM SiteSettings;
   ```
   (baseline: Users 9, Orders 5, Bills 2, Settings 0)

## 2. Demo ka best tareeqa — live end-to-end flow

**Demo ka plan: ek naya customer order se payment tak — poora safar.**

### Step 1 — Walk-in order (Retail panel)
- Login: `R0000000000001` / `Nexus@2026` → `/Retail/Dashboard`
- **New Order** → 4-step wizard: Service → Customer → Installation → Account
- Zaroori: city = Karachi, **Broadband plan**, phone format `0300-1234567`
  (dash ke saath), password 8+ chars, terms accept
- Result: order id + account id milta hai (screen par message)

### Step 2 — Feasibility + installation (Technical panel)
- Login: `T0000000000001` / `Nexus@2026` → `/Technical/Dashboard`
- Survey Queue → naya order → **RecordCheck** (distance 2 km, capacity yes)
- **Confirm** (date) → **StartInstallation** → **CompleteInstallation**
- Connections → nayi connection `Pending` → **Activate** (Active karo —
  warna billing isay skip kar degi)

### Step 3 — Billing run (Accounts panel)
- Login: `F0000000000001` / `Nexus@2026` → `/Accounts/Dashboard`
- **Run Billing** → bill `Draft` banti hai
- `/Bills` → bill kholo → **Issue** → `Issued`

### Step 4 — Payment record (Accounts ya Retail)
- Bill details par payment form: amount + method → **Record**
- Receipt number milti hai, bill `Paid`/`PartiallyPaid`

### Step 5 — Customer dekhta hai (Customer panel)
- Naye customer account id se login → `/Customer/Dashboard`
- Connection, bill + paid status, track order (seedha uska order), Account
  Status (seedha uska data), **Upgrade plan** (request bhejo)

### Step 6 — Admin side (zyada features)
- `/Admin/Dashboard` → Orders register, Reports
- `/Admin/Plans` — plan CRUD (icon buttons), `/Admin/Settings` — site name
  change karke dikhao (navbar/footer turant badalta hai)
- `/Admin/PlanChanges` — customer ki upgrade request approve karo
- `/Admin/Organisation/Logins` — staff login banao (Admin role nahi ban sakta)
- `/Search/Advanced` — customer search

## 3. Demo ke tips (evaluation ke liye)

- **Screens par kuch bhi fake nahi** — har number DB se aata hai. Agar kuch
  khaali dikhe to "abhi koi record nahi" — ye sahi behaviour hai.
- **Notification bell** — live counts (surveys waiting, overdue bills, low
  stock). Click karo, dikhao.
- **Roles ka demo** — 4 panel logins se dikhao ke kaun kya dekhta hai
  (authorization alag hai har panel ka).
- **Settings screen** — site.name badal ke save → home page refresh → navbar
  badla hua. Ye impressive lagta hai.
- **Plan change** — naya feature hai, SRS mein hai ("customers try to change
  their plan"). Customer request → admin approve → agla bill naye plan par.

## 4. Judge ke sawal jo aa sakte hain (aur jawab)

**Q: Customer online payment kahan karta hai?**
A: Ye billing management system hai — payment counter par hoti hai, staff
record karta hai (SRS ke mutabiq retail + accounts dono maintain karte hain).
Online gateway SRS mein nahi thi.

**Q: Advance kya hota hai?**
A: Customer order waqt advance deta hai. Bill par advance sirf utna lagta hai
jitna period ka kharcha — baqi agli bill par carry forward. Negative bill
kabhi nahi banti.

**Q: Tax kya hai?**
A: 12.24% (`TaxConstants`) har bill par.

**Q: Discount ka naap kya hai?**
A: Bulk tiers — 10+ connections se 25%, 15-25 → 50%, 25-50 → 75%, 50+ → 100%.
Base = advance + security deposit.

**Q: Draft bill customer ko kyun nahi dikhti?**
A: Draft internal document hai — jab tak Issue na ho, customer ko nahi dikhti.

**Q: Password kaise secure hai?**
A: PBKDF2, 210,000 iterations, per-user salt (.NET Identity hasher).
Login par lockout (5 attempts), rate limiting.

**Q: Staff logins kaun banata hai?**
A: Admin `/Admin/Organisation/Logins` se — magar Admin role wahin se nahi
banti (seed hi admin banata hai).

## 5. Ek demo run ke liye quick reference

| Kya dikhana hai | Kahan |
|---|---|
| Order wizard | `/Retail/NewOrder` |
| Feasibility flow | `/Operations` |
| Billing run | `/Accounts/Dashboard` → Run Billing |
| Payment | `/Bills/Details/{id}` → Record Payment |
| Customer view | `/Customer/Dashboard` |
| Advanced search | `/Search/Advanced` |
| Reports | `/Reports` |
| Settings live apply | `/Admin/Settings` → site.name |
| Plan change | `/Customer/Upgrade` + `/Admin/PlanChanges` |
| Staff logins | `/Admin/Organisation/Logins` |

Poora technical flow: `SYSTEM-FLOW-GUIDE.md`.
