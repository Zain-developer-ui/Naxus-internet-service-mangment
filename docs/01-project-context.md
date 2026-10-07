# 01 — Project Context

## Kya hai ye

Aptech **eProject** — NEXUS Service Marketing System.
Ek telecom + internet service vendor ka **online management system**.

Team member ne frontend bana diya hai (commit: *"frontent redy just some inprovments"*).
Zain (aur uski team) ko baqi ka kaam karna hai.

## Deliverable kya hai

Ek web application jisme:
- Customer online connection order kar sake
- Retail outlet order place kar sake
- Technical team installation handle kare
- Accounts department bill generate kare
- Admin sab kuch manage kare
- Public user order / connection status check kar sake

## Technical stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core **8.0** MVC |
| Views | Razor (.cshtml) |
| Root namespace | `NEXUS` |
| Frontend CSS | Vanilla CSS (18 files, design tokens in `site.css`) |
| Frontend JS | Vanilla JS (10 files, `window.NEXUS` namespace) |
| Icons | Font Awesome 6.5.1 (CDN) |
| Charts | Chart.js 4.4.1 (CDN) |
| Fonts | Inter (Google Fonts CDN) |
| DB (planned) | SQL Server + EF Core 9.0.20 |
| Lib folder | bootstrap, jquery, jquery-validation, jquery-validation-unobtrusive |

## Folder structure (asli)

```
Naxus-internet-service-mangment/
  Controllers/        13 files
  Models/
    ErrorViewModel.cs
    Services/Api/     IApiService.cs, ApiService.cs  (abhi MOCK)
    ViewModels/       10 files
  Views/              36 .cshtml
    Account/   Admin/   Accounts/   Bills/   Customer/
    Feedback/  Home/    Orders/     Plans/   Profile/
    Retail/    Services/ Technical/ Shared/
  wwwroot/
    css/   18 files
    js/    10 files
    lib/   bootstrap, jquery, jquery-validation(-unobtrusive)
  docs/                (ye folder — project ka dimagh)
  .workbuddy-ai/memory/  (session memory)
```

## Controllers jo abhi hain

| Controller | Actions | Kaam |
|---|---|---|
| `HomeController` | Index, About, Contact, Error | Public pages |
| `AccountController` | Login, Status | Login + public status check |
| `ServicesController` | Index | Services list |
| `PlansController` | Index, Details | Plans |
| `OrdersController` | New, Tracking | Order form + tracking |
| `CustomerController` | Dashboard, MyConnection | Customer portal |
| `BillsController` | Index, Details | Bills |
| `ProfileController` | Index | **Hardcoded** profile |
| `FeedbackController` | Index | Feedback form |
| `AdminController` | Dashboard, Reports, Settings | Admin — **koi data nahi, static** |
| `AccountsController` | Dashboard | **Static** |
| `TechnicalController` | Dashboard | **Static** |
| `RetailController` | Dashboard, NewOrder, Search | **Static** |

## Abhi ki haalat — sach

Ye **frontend scaffold** hai, working app nahi:

1. **Koi database nahi.** `ApiService.cs` poora MOCK hai — har method hardcoded
   demo data return karta hai. Lekin `csproj` mein EF Core SqlServer refs
   mojood hain, aur `Program.cs` mein comment likha hai "no database" — ye
   comment **galat** hai aur confusion phelata hai.
2. **Koi authentication nahi.** Login koi bhi 6+ char password accept karta hai,
   phir Customer dashboard par bhej deta hai. Koi session, koi cookie, koi role check.
3. **Koi authorization nahi.** Admin/Technical/Accounts/Retail dashboards
   **public URLs** hain — koi bhi khole.
4. **Data hardcoded.** "NX12345678" har jagah pass hota hai. Customer ka naam
   "Ahmed Khan" controller mein chipka hua hai.
5. **Kuch views static hain.** Admin/Reports mein 49 inline styles hain, page
   mein dummy tables hain — koi real data source nahi.

## Team ki ghalat-fahmi

Header comments aur commit message kehte hain "frontend only".
Reality: csproj mein EF Core hain, aur poora SRS backend maangta hai.
Yani kaam sirf frontend ka nahi — **backend from scratch** banana hai.

## Zain ki preferences (hard rules)

- Code **human-written** lagna chahiye — koi emoji nahi, koi filler/narrating
  comment nahi, koi boilerplate docblock nahi. Comment sirf genuine
  non-obvious "why" par, aur kam.
- **Fake statistics / testimonials / logos — bilkul nahi.**
- Excessive gradients, glassmorphism, neon, flashy animation — nahi.
- Default Bootstrap / student-project look — nahi. **Portfolio-grade** chahiye.
- **Navbar mein search bar — nahi.**
- Data loss se bohat darte hain — **actual DB counts** se tasalli do, alfaz se nahi.
- Terminal: `cmd.exe` (Git Bash nahi). Shell commands cmd syntax mein.
- Zaban: **Roman Urdu**. Chhote, seedhe jawab. Sawaal ka jawab do, process
  narration nahi.

## Related documents

- `02-srs.md` — SRS ka poora nichor
- `03-requirements-matrix.md` — requirement vs implementation
- `04-frontend-audit.md` — jo masle mile
- `05-frontend-decisions.md` — kya fix kiya aur kyun
- `06-database-schema.md` — DB design
- `07-api-contract.md` — API endpoints
- `08-backend-progress.md` — backend ka kaam
- `09-testing-checklist.md` — test cases
- `10-changelog.md` — kab kya badla
