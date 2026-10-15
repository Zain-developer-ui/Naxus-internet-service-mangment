# UI Fix Plan — 14 Oct 2026

Aap ne 12 cheezein batayin. Sab investigate kar liya (code padh kar, live test ke
baghair — server band hai). Har issue ki **asli wajah** neeche hai, phir fix ka plan.

---

## Findings — 12 issues ki asli wajah

| # | Aap ne kaha | Asli wajah | Yaqeen |
|---|---|---|---|
| 1 | Notification kaam nahi karta | **Notification feature hi nahi hai.** `_DashboardTopbar.cshtml:16` par bell sirf ek button hai — koi `data-*`, koi handler, koi panel nahi. `badge-dot` bhi **fake** hai (hamesha dikhta hai) | ✅ pakka |
| 2 | Message kaam nahi karta | Wahi — koi messaging feature nahi, envelope decorative hai | ✅ pakka |
| 3 | Setting change ke baad design kharab | `Views/Admin/Settings.cshtml:44` — `<div asp-validation-summary="ModelOnly" class="alert alert-danger reveal">` **hamesha** render hota hai → page par permanent **laal box** | ✅ pakka |
| 4 | Har input ke neeche warning icon | `forms.css:173` — `.field-error::before { content:'\f071' }` (warning triangle) + `.field-error{min-height:16px}`. Khaali hone par bhi icon chhapta hai | ✅ pakka |
| 5 | Validation theek + auto format | Client rules ab server se match karti hain, magar **input masking nahi hai** — phone/CNIC/landline ka dash khud nahi aata | ✅ pakka |
| 6 | Plan page ka design theek nahi | **Kaunsa page?** `/Plans` (public) ya `/Admin/Plans`? Live screenshot chahiye | ❓ |
| 7 | New order ka aakhri (password) step kharab | `NewOrder.cshtml` password field `.input-wrap` + `.toggle` + `.strength` use karta hai — **teenon classes sirf `.login-stars-page` / `.apply-form` ke andar styled hain**. NewOrder par koi style nahi → eye button aur wrapper bina style | ✅ pakka |
| 8 | Kuch sidebar links active nahi hote | Do wajahein: (a) links jin par `IsActive()` **hi nahi** — `/Orders/Tracking`, `/Account/Status` (customer), `/Feedback` (technical); (b) keys jo **kabhi set nahi hote** — `invoices`, `technical`, `billing` | ✅ pakka |
| 9 | Technical mein customer page 403 | Sidebar line 82 `/Retail/Search` link karta hai, magar `RetailController` `[Authorize(Retail, Admin)]` hai → Technical 403 | ✅ pakka |
| 10 | Technical mein Feedback 403 | Sidebar line 86 `/Feedback`, magar `FeedbackController` `[Authorize(Customer, Admin)]` → Technical 403 | ✅ pakka |
| 11 | Accounts/Technical mein active link ka background nahi badalta | Issue 8 wahi — `/Bills` par `SetActiveItem` **bilkul nahi** lagta; accounts ka "Overdue" key `invoices` use karta hai jo **kahin set nahi hota** | ✅ pakka |
| 12a | Accounts mein Customer Dues 403 | Issue 9 wahi — `/Retail/Search` accounts nav mein bhi link hai | ✅ pakka |
| 12b | Laal error box hamesha dikhta hai | **19 views** `asp-validation-summary` use karti hain. CSS mein `.validation-summary-valid { display:none }` **nahi hai** — `components.css:902` ka `:empty` kaam nahi karta kyunke ASP.NET andar ek chhupa `<li>` daal deta hai | ✅ pakka |

### Iske ilawa jo mila (aap ne nahi bataya)

- **Settings save hote hain magar apply kahin nahi hote.** `site.name`, `brand.primary`,
  `brand.accent`, `feature.*` toggles, `security.sessionMinutes`, `security.maxAttempts`
  — sab DB mein jaate hain, magar code mein inhein **koi nahi padhta**. Yani 17 settings
  mein se 15 **dead** hain.
- `feature.maintenance` (maintenance mode) enforce nahi hota.
- `security.sessionMinutes` cookie ke timeout se jurra nahi.

---

## Fix Plan

### Wave A — CSS + chhote markup fixes (sabse bara faida, kam risk)

| # | Kaam | Files | Fixes |
|---|---|---|---|
| A1 | Khaali validation summary chhupao | `components.css` / `forms.css` + `Admin/Settings.cshtml` | 3, 12b |
| A2 | Khaali `.field-error` ka icon chhupao | `forms.css` | 4 |
| A3 | `.input-wrap` / `.toggle` / `.strength` ko global karo | `forms.css` (ya naya block) | 7 |
| A4 | Sidebar: missing `IsActive` keys + controllers mein missing `SetActiveItem` | `_Sidebar.cshtml` + `BillsController`, `AdminPlansController`, `AccountsController`, `TechnicalController` | 8, 11 |
| A5 | 403 links theek karo | `_Sidebar.cshtml` + `RetailController` / `FeedbackController` | 9, 10, 12a |

**A5 ka faisla** (aap confirm karo):
- **Behtar:** `/Retail/Search` aur `/Search/Advanced` ko **saare staff** ke liye kholo.
  SRS kehta hai search "status of the order or the status of the connection" check karne
  ke liye hai — Technical aur Accounts ko wo chahiye bhi. Order *banana* Retail-only rahega.
- **Ya:** links hata do.

### Wave B — features

| # | Kaam | Fixes |
|---|---|---|
| B1 | **Input auto-format** — phone `0300-1234567`, CNIC `42101-1234567-1`, landline `021-1234567` khud dash lagaye | 5 |
| B2 | **Real notifications panel** — role ke hisaab se asli kaam dikhaye: overdue bills (Accounts), survey queue (Technical), low stock (Retail/Admin). Fake `badge-dot` hata do, asli count lagao | 1 |
| B3 | **Message button hata do** — messaging SRS mein nahi hai, fake rakhna behtar nahi | 2 |
| B4 | Plan page design | 6 (confirm karne ke baad) |
| B5 | Dead settings: ya wire karo (brand colour + site name + session timeout), ya screen se hata do | extra |

### Wave C — verification

| # | Kaam |
|---|---|
| C1 | Paanchon panel ka UI + workflow live test |
| C2 | Jo tests is change se tootenge unhein dobara chalao (login, order, billing, settings) |
| C3 | TestSprite ke jo tests ab bhi fail/blocked hain unhein dobara |

---

## Panel map — kaun sa panel kya karta hai

| Panel | Role | Entry | Kya karta hai |
|---|---|---|---|
| **Admin** | Admin | `/Admin/Dashboard` | Sab kuch: plans, orders, inventory, vendors, purchase orders, outlets & staff, staff logins, cities, discount slabs, settings, reports |
| **Retail** | Retail | `/Retail/Dashboard` | Naya order (4-step wizard: Service → Customer → Installation → Account), till-date register, customer search, stock dekhna |
| **Technical** | Technical | `/Technical/Dashboard` | Survey queue, feasibility check, installation start/complete, connections, overdue policy |
| **Accounts** | Accounts | `/Accounts/Dashboard` | Bills, payments, overdue, call metering, one-off charges, monthly billing run |
| **Customer** | Customer | `/Customer/Dashboard` | Apni connection, bills & payments, profile, feedback, order tracking |

**Sidebar role ke hisaab se badalta hai** — `_Sidebar.cshtml` mein `User.SidebarRole()`
se. Yani ek role doosre ka page nahi khol sakta (403) — aur **wahi issues 9, 10, 12a
ki jarr hai**: sidebar mein wo links hain jo us role ke liye allowed nahi.

---

## Aap ke jawab (14 Oct)

1. **Plan page = admin wala** → `/Admin/Plans`
2. **403** Retail aur Technical panels mein tha
3. **Notifications** — topbar icon par click karne se asli panel khule

---

## Status — kya ho chuka

| # | Kaam | Status |
|---|---|---|
| A1 | Validation summary chhupao | ✅ `forms.css` mein rule + `Admin/Settings.cshtml` + `Feedback/Index.cshtml` ka bare alert theek |
| A2 | `.field-error` ka icon chhupao | ✅ `forms.css` — ab `:not(:empty)` par hi dikhta hai |
| A3 | `.input-wrap` / `.toggle` / `.strength` global | ✅ `forms.css` mein add (pehle sirf login page par thi) |
| A4 | Sidebar active keys | ✅ `SetActiveItem` ab **kai keys** le sakta hai; `BillsController`, `TechnicalController`, `AccountsController`, `OrdersController.Tracking`, `AccountController.Status` theek |
| A5 | 403 links | ✅ `RetailController` ab `StaffRoles` (Search sab staff ke liye), order lena Retail-only raha |
| B1 | Input auto-format | ✅ `forms.js` — dashes **server ke apne `data-val-regex-pattern`** se derive hote hain |
| B2 | Asli notifications | ✅ naya `INotificationService` + topbar panel + CSS + JS toggle |
| B3 | Message button | ✅ hata diya |
| B4 | Admin plan page design | ⏳ **live dekhna baqi** |
| B5 | Dead settings | ⏳ pending |

**Build: 0 errors, 0 warnings.**

### Notifications kya dikhati hain (role ke hisaab se)

| Role | Notification |
|---|---|
| Admin / Technical | "Surveys waiting" → `/Operations` |
| Admin / Accounts | "Overdue bills" → `/Bills?status=Overdue` |
| Admin / Accounts | "Charges not billed yet" → `/Accounts/Charges` |
| Admin / Retail | "Stock running low" → `/Admin/Inventory` |
| Customer | "Bills to pay" → `/Bills` |

Sab **asli rows se count** hoti hain — koi stored message nahi, is liye badge kabhi
ghalat nahi ho sakta. Kuch na ho to panel kehta hai "Nothing needs your attention".

---

## ⏳ Jo baqi hai

1. **Admin plan page** — live screenshot ke baghair pata nahi chalega ke "design kharab"
   se aap ka matlab kya hai (10 columns hain, overflow ho sakta hai)
2. **Dead settings** — brand colour / site name / session timeout wire karna
3. **Paanchon panel ka live UI + workflow test**
4. TestSprite ke jo tests in changes se mutasir honge (login, order, settings)

---

## Server

**App abhi band hai** (build ke liye band karni pari). Wave A + B1–B3 ka code tayyar hai —
live verify karne ke liye app chalu karni hogi. Jab aap kaho, chalu kar do, main
screenshot lekar dekh lunga aur plan page theek kar dunga.
