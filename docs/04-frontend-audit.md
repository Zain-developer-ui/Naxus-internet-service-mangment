# 04 — Frontend Audit

Audit date: **5 Oct 2026**
Method: code reading (app chalane ki zaroorat nahi thi — sab masle source mein saaf hain)

---

## A. TooTa hua (bugs)

### A1. `validation.js` missing thi — CRTICAL
`Views/Shared/_Layout.cshtml:51` `~/js/validation.js` load karta hai, lekin file
`wwwroot/js/` mein **mojood nahi thi** → har page par 404.

Bach gaya bas is liye ke validation ka asal code **`modal.js`** mein tha
(galat naam wali file). `_Layout` mein `modal.js` line 50 par hai, `validation.js`
line 51 — is liye `NEXUS.validation` define ho jati thi aur `forms.js` crash nahi karta.

**Asar:** 404 request har page load par, aur confusion (file ka naam kaam se match nahi).

### A2. `NEXUS.getUser()` / `saveUser()` kahin define nahi — CRITICAL
`Views/Account/Login.cshtml` in functions ko call karta hai:
- line 206: `NEXUS.getUser()`
- line 219–220: `NEXUS.saveUser(newUser)`
- line 225: `NEXUS.getUser()`

Lekin `wwwroot/js/` mein **koi bhi file** inhe define nahi karti.
`localStorage` / `sessionStorage` ka koi code hi nahi hai project mein.

**Asar:** Login form submit hone par ye code check karta hai
`if (window.NEXUS && NEXUS.saveUser)` — `NEXUS` mojood hai lekin
`NEXUS.saveUser` `undefined` hai, to `if` chup-chaap fail ho jata hai.
Phir line 225 `NEXUS.getUser()` **bina guard** call hoti hai → **TypeError** →
login ka toast aur baqi JS ruk jata hai.

### A3. CSS load order galat — HIGH (design ka asli masla)

`_Layout.cshtml` mein ye order hai:

```
15  site.css          ← design tokens yahan hain
...
29  home.css          ← 0 tokens, 99 hardcoded hex
30  public-pages.css  ← 104 vars (tokens use karta hai)
31  orders.css        ← 88 vars
32  dashboards-luxury.css ← 101 vars
```

CSS cascade mein **jo baad mein aata hai wo jeetta hai** (same specificity par).
`home.css` **pehle** aata hai, magar `public-pages.css` / `dashboards-luxury.css`
**baad** mein. Yani home.css ki styling baad wali files se override ho jati hai.

Aur `home.css` mein **0 design tokens** hain — sab kuch hardcoded:
```
30x #FFFFFF   13x #0A1F44   8x #1E6FE5   8x #1558B8
 7x #64748B    6x #38BDF8   4x #EEF2F7   4x #E8F1FD
```
Ye tokens ki values se match karte hain, lekin **duplicate** hain. Yani agar
brand color badla jaye to home.css bhool jayegi.

**Asar:** Homepage aur andar ke pages ka look inconsistent. Design "toota hua" lagta hai.

### A4. Duplicate CSS loads — MEDIUM

`_Layout` mein `home.css`, `public-pages.css`, `orders.css`, `pricing.css`,
`services.css`, `dashboards-luxury.css` pehle se load hote hain. Phir:

| View | Dobara load karta hai |
|---|---|
| `Home/Index.cshtml` | `pricing.css`, `services.css`, `home.css` |
| `Services/Index.cshtml` | `home.css`, `public-pages.css` |
| `Plans/Index.cshtml` | `home.css`, `pricing.css`, `public-pages.css` |
| `Orders/New.cshtml` | `orders.css`, `public-pages.css` |

Browser cached files ko dobara fetch nahi karta, lekin **cascade dobara apply**
hota hai — aur ye A3 ka masla barha deta hai.

### A5. `new-connection.js` optional load
`Views/Orders/New.cshtml:437` par page ke andar `<script src="~/js/new-connection.js">`
hai. `_Layout` ke `<body>` ke end par scripts load hoti hain, phir
`@RenderSectionAsync("Scripts")`. Yani ye theek chalega, lekin pattern baqi
scripts se mukhtalif hai.

### A6. `csproj` vs comments — documentation jhoot
`Program.cs` (line 5–7) aur `ApiService.cs` ke header comments:
> *"NEXUS is a frontend-only project — no database, no authentication backend."*

Lekin `NEXUS.Services.csproj` mein:
```xml
Microsoft.EntityFrameworkCore.SqlServer  9.0.20
Microsoft.EntityFrameworkCore.Tools      9.0.20
Microsoft.EntityFrameworkCore.Design     9.0.20
```
**Asar:** Naya developer confuse hota hai. Commit message bhi kehta hai
"frontend ready" — yani team khud nahi janti.

---

## B. Design quality issues

### B1. Design system toot; hui hai
Do duniya hain:
- **Tokens wali** — buttons (52 vars), cards (113), forms (82), components (101),
  public-pages (104), orders (88), dashboards-luxury (101)
- **Hardcoded** — `home.css` (0 vars / 99 hex), `login-stars.css` (0 vars / 51 hex)

### B2. Inline styles ka bojh

| View | `style="` count |
|---|---|
| `Admin/Reports.cshtml` | **49** |
| `Accounts/Dashboard.cshtml` | 23 |
| `Retail/Dashboard.cshtml` | 18 |
| `Technical/Dashboard.cshtml` | 16 |
| `Home/Index.cshtml` | 5 |

`Reports.cshtml` mein 49 inline styles — ye practically ek CSS file ka kaam hai
jo galat jagah likha hua hai.

### B3. `!important` ka istemaal

| File | Count |
|---|---|
| `public-pages.css` | 9 |
| `site.css` | 6 |
| `orders.css` | 4 |
| `forms.css` | 3 |
| `animations.css` | 3 |

`!important` cascade ko zabardasti todta hai. Ye A3 wale order masle ki alamat hai.

### B4. External images — offline par tootegi

| Source | Kahan |
|---|---|
| `images.unsplash.com` | 8 images (Home, Login, Services, Plans) |
| `i.pravatar.cc` | avatars (Topbar, Profile, Testimonials) |
| CDN: Font Awesome, Chart.js, Google Fonts | har page |

**Asar:** demo/presentation mein internet slow ya band ho to poora look barbaad.
Competition judging offline ho sakti hai.

---

## C. User preference violations

| # | Masla | Kahan |
|---|---|---|
| C1 | **Fake statistics** — "1,245 Active Customers", "42 Cities", "15 Plans" | `Home/Index.cshtml:110,114,118` |
| C2 | **Fake rating** — "4.8★ rating", "Trusted Provider" | `Home/Index.cshtml:142` |
| C3 | **"1,245+ online"** fake live badge | `Home/Index.cshtml:86` |
| C4 | **Fake testimonials** — Bilal Khan, Sana Iqbal, Usman Ahmed | `Home/Index.cshtml:566,576,586` |
| C5 | **Fake claim** — "Join over 1,200 customers across 42 cities" | `Home/Index.cshtml:603` |
| C6 | **"Real feedback from NEXUS customers"** — jabke feedback fake hai | `Home/Index.cshtml:556` |
| C7 | **Dashboard topbar mein search bar** | `_DashboardTopbar.cshtml:7` |
| C8 | **"Why Thousands Choose NEXUS"** — jhoota claim | `Home/Index.cshtml:~350` |

Zain ke rules: *"Fake statistics, fake testimonials, fake logos — nahi"* aur
*"Navbar mein search bar — nahi"*.

---

## D. Security issues (backend ka kaam, lekin abhi se note)

### D1. Login kuch bhi accept karta hai
`AccountController.Login` (line 22–25):
```csharp
if (!ModelState.IsValid) return View(model);
TempData["Success"] = "Logged in successfully (demo).";
return RedirectToAction("Dashboard", "Customer");
```
Koi bhi 6+ char password chalta hai. Koi verification nahi.

### D2. Sab dashboards public hain
`Admin/Dashboard`, `Technical/Dashboard`, `Accounts/Dashboard`, `Retail/Dashboard` —
koi `[Authorize]` attribute nahi. URL type karo, dashboard khul jata hai.

### D3. Hardcoded identity har jagah
- `CustomerController.cs:17,23` → `"NX12345678"`
- `BillsController.cs:13` → `"NX12345678"`
- `ProfileController.cs:18` → `"NX12345678"`, `"Ahmed Khan"`
- `OrdersController.cs:22,23,28` → `"NX-ORD-1003"`, `"NX-ORD-1001"`

### D4. `Program.cs` mein auth pipeline nahi
```csharp
app.UseRouting();
app.UseAuthorization();   // <- auth ke bina authorization ka koi matlab nahi
```
`UseAuthentication()` nahi hai, koi cookie scheme registered nahi.

### D5. Search param validation
`AccountController.Status` `accountId` / `phone` / `cnic` seedha API ko deta hai.
Abhi mock hai, lekin real DB aane par sanitization zaroori hai.

---

## E. Ko poori tarah static views (no data source)

| View | Lines | Inline styles | Note |
|---|---|---|---|
| `Admin/Reports.cshtml` | 681 | 49 | Sab dummy |
| `Home/Index.cshtml` | 629 | 5 | Marketing page |
| `Admin/Dashboard.cshtml` | 517 | ~10 | Dummy tables |
| `Account/Status.cshtml` | 514 | ~8 | Mock search |
| `Orders/Tracking.cshtml` | 508 | ~5 | Mock |
| `Plans/Details.cshtml` | 482 | ~3 | Mock |
| `Services/Index.cshtml` | 473 | ~3 | Mock |
| `Admin/Settings.cshtml` | 448 | ~6 | Static form |
| `Retail/Search.cshtml` | 442 | ~4 | UI hai, logic nahi |
| `Orders/New.cshtml` | 436 | ~5 | Multi-step form |
| `Accounts/Dashboard.cshtml` | 393 | 23 | Dummy |
| `Customer/Dashboard.cshtml` | 382 | ~5 | Mock |
| `Technical/Dashboard.cshtml` | 380 | 16 | Dummy |
| `Bills/Index.cshtml` | 371 | ~4 | Mock |
| `Customer/MyConnection.cshtml` | 365 | ~5 | Mock |
| `Retail/Dashboard.cshtml` | 342 | 18 | Dummy |

**Total frontend: ~10,700 lines** (Views + CSS + JS). Achha kaam hai — bas
design consistency aur real data chahiye.

---

## F. Positive cheezein (ye achhi hain, barqarar rakho)

- Sidebar ka role-based structure (`_Sidebar.cshtml`) saaf hai — 5 roles handle karta hai
- `IApiService` abstraction mojood hai — backend plug karne ke liye ye sahi design hai
- ViewModels alag hain (`Models/ViewModels/`) — strongly typed
- `ValidationAttributes` ViewModels par lagaye huye hain (server-side validation
  ke liye ready)
- `[ValidateAntiForgeryToken]` POST actions par mojood hai (CSRF protection)
- Toast system (`site.js`) achha likha hua hai
- Design tokens (`site.css`) waqai poori tarah define hain

---

## Summary — priority

| Priority | Kaam | Effort |
|---|---|---|
| **P0** | A1 validation.js, A2 getUser crash | 15 min |
| **P0** | A3 CSS load order + home.css tokens | 45 min |
| **P1** | A4 duplicate CSS, B4 external images | 30 min |
| **P1** | C1–C8 fake data hatana | 30 min |
| **P2** | B2 inline styles, B3 `!important` | 1–2 hrs |
| **P2** | A6 comments theek karna, D1–D5 security | backend phase |
