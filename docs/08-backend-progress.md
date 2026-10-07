# 08 — Backend Progress

Status: **NOT STARTED** (planning complete)

Ye file backend ka kaam track karti hai. Har step complete hone par update karo.

---

## Poora plan — 9 areas

Zain ne ye areas maange the. Inko phases mein toda hai taake har phase
independently test ho sake.

### Phase 4.0 — Foundation (sab se pehle)

| # | Kaam | Kyun pehle |
|---|---|---|
| 4.0.1 | Currency ka faisla (PKR ya USD?) | Plan prices, bills sab is par depend karte hain |
| 4.0.2 | Connection string + User Secrets setup | DB connect hone se pehle |
| 4.0.3 | `Program.cs` ke galat comments theek karo | Comments kehte hain "no database" jabke EF Core mojood hai |
| 4.0.4 | `DbContext` + Identity setup | Sab kuch iske upar bana hai |
| 4.0.5 | Service layer folder structure | `Services/` banao — business logic Controllers mein nahi |

### Phase 4.1 — Database

| # | Kaam | Detail |
|---|---|---|
| 4.1.1 | Entity classes | `docs/06-database-schema.md` ke mutabiq, 21 tables |
| 4.1.2 | Fluent API config | Unique indexes, relationships, decimal precision |
| 4.1.3 | Migrations | 6 migrations (schema doc mein plan hai) |
| 4.1.4 | Seed reference data | Cities, plans (SRS rates), discount tiers, roles |
| 4.1.5 | ID sequence generators | Order ID, Account ID 12-digit serial — race-safe |

### Phase 4.2 — Authentication

| # | Kaam | Detail |
|---|---|---|
| 4.2.1 | Identity setup | `AppUser` custom columns (EmployeeId, CustomerId, RetailShopId) |
| 4.2.2 | Login page wire karo | `AccountController.Login` ko real `SignInManager` se replace |
| 4.2.3 | Logout | Abhi logout sirf `/Account/Login` ka link hai — real POST + sign out |
| 4.2.4 | Password policy | Min 8 chars, Identity complexity on |
| 4.2.5 | Lockout | 5 failed attempts = 15 min lock |
| 4.2.6 | Cookie config | `HttpOnly`, `Secure`, `SameSite=Lax`, sliding expiry |

### Phase 4.3 — Authorization

| # | Kaam | Detail |
|---|---|---|
| 4.3.1 | 5 roles seed karo | Admin, Accounts, Technical, Retail, Customer |
| 4.3.2 | `[Authorize]` har controller par | Fallback policy = authenticated |
| 4.3.3 | Role-based sidebar | `_Sidebar.cshtml` ab `ViewData` par hai — real role se aana chahiye |
| 4.3.4 | Ownership checks | Customer sirf apna data — service layer mein |
| 4.3.5 | Role-based topbar | `ViewData["UserRole"]` hardcoded hai — claims se aana chahiye |
| 4.3.6 | 403 page | Forbidden ke liye alag page (login par redirect nahi) |

### Phase 4.4 — Validation

| # | Kaam | Detail |
|---|---|---|
| 4.4.1 | Saare ViewModels check | Kuch attributes missing hain |
| 4.4.2 | Server-side business rules | Feasibility, duplicate CNIC, plan active, status transition |
| 4.4.3 | Client + server sync | Same error messages dono jagah |
| 4.4.4 | Multi-step form validation | `Orders/New` — step ke andar validate, submit par sab |

### Phase 4.5 — Security

| # | Kaam | Detail |
|---|---|---|
| 4.5.1 | Security headers | nosniff, X-Frame-Options, CSP, Referrer-Policy |
| 4.5.2 | Rate limiting | `Account/Status` aur login par |
| 4.5.3 | HTTPS enforcement | HSTS production mein |
| 4.5.4 | Anti-forgery | Sab POST par (abhi bhi hai) |
| 4.5.5 | Input sanitization | Search params, file upload validation |
| 4.5.6 | File upload safety | Type/size check, `wwwroot` ke bahar store |
| 4.5.7 | Audit fields | Kaun kya badla — har mutation par |
| 4.5.8 | Secrets | User Secrets dev, env vars prod |
| 4.5.9 | Logging hygiene | PII kabhi log na ho |

### Phase 4.6 — API / Service layer

| # | Kaam | Detail |
|---|---|---|
| 4.6.1 | `EfApiService` likho | `ApiService` (mock) ki jagah, `docs/07-api-contract.md` ke mutabiq |
| 4.6.2 | `Result<T>` envelope | `bool` ki jagah error kind |
| 4.6.3 | Service classes | Order, Billing, Connection, Plan, Vendor, Employee, Stock |
| 4.6.4 | DI registration | Interface ke against register karo |
| 4.6.5 | AutoMapper ya manual mapping | Manual prefer — explicit rehta hai |

### Phase 4.7 — Error handling

| # | Kaam | Detail |
|---|---|---|
| 4.7.1 | Global exception handler | `IExceptionHandler` + reference id |
| 4.7.2 | User-friendly error pages | 404, 403, 500 |
| 4.7.3 | Structured logging | Serilog ya built-in, correlation id ke sath |
| 4.7.4 | Validation summary UI | Field errors + top-level summary |

### Phase 4.8 — Performance

| # | Kaam | Detail |
|---|---|---|
| 4.8.1 | Pagination | Sab list queries par |
| 4.8.2 | Indexes | Schema doc ke mutabiq |
| 4.8.3 | `AsNoTracking` | Read-only queries |
| 4.8.4 | Caching | Plans, cities, services (rarely change) |
| 4.8.5 | Bundle + minify CSS/JS | 18 CSS + 10 JS files abhi alag load hote hain |
| 4.8.6 | CDN → local | Font Awesome, Chart.js, Google Fonts |
| 4.8.7 | Image optimization | 18 images download ho chuki hain — resize + WebP |
| 4.8.8 | Response compression | Gzip/Brotli |
| 4.8.9 | Query profiling | N+1 pakdo, EF logging on in dev |

### Phase 4.9 — Accessibility

| # | Kaam | Detail |
|---|---|---|
| 4.9.1 | Semantic HTML | `<nav>`, `<main>`, `<section>` — kuch jagah missing |
| 4.9.2 | Form labels | Har input par `<label for>` |
| 4.9.3 | ARIA | Icons par `aria-hidden`, buttons par `aria-label` |
| 4.9.4 | Keyboard navigation | Sidebar, modals, dropdowns, multi-step form |
| 4.9.5 | Focus visible | Focus rings har interactive element par |
| 4.9.6 | Color contrast | WCAG AA — muted text check karo |
| 4.9.7 | Skip link | "Skip to content" |
| 4.9.8 | Live regions | Toast `aria-live` (abhi hai), form errors announce hon |
| 4.9.9 | Screen reader test | NVDA ya Narrator se ek pass |

### Phase 5 — SRS features jo nahi hain

`docs/03-requirements-matrix.md` dekho — 34 MISSING items.

Priority order:
1. ID formats (Order ID, Account ID) — foundation
2. Plans + charges (SRS rates) — billing depend karta hai
3. Bill generation + tax + discount
4. Feasibility workflow
5. Connection lifecycle
6. Admin CRUD (vendor, stock, employee, retail shop)
7. Advanced search
8. Bulk/corporate discount

---

## Rozana kaam — yahan update karo

| Date | Kya kiya | Files |
|---|---|---|
| 5 Oct 2026 | Phase 4 plan banaya. Abhi implementation shuru nahi. | `docs/06`, `docs/07`, `docs/08` |

---

## Notes

- **Currency ka faisla pending hai.** SRS USD kehta hai, existing frontend PKR.
  Ye pehla kaam hai — warna plans aur bills ka koi number bharosa ke qabil nahi.
- **`IApiService` design barkarar rakhna hai.** Frontend usi par depend karta hai.
  Swap `ApiService` → `EfApiService` sirf DI mein hona chahiye.
- **`Program.cs` aur `ApiService.cs` ke comments galat hain** ("frontend-only,
  no database"). Pehle din theek karo — naya developer dhoka kha jata hai.
- **Views mein 910 inline styles hain.** Ye Phase 2 ka baqi kaam hai, backend se
  pehle ya sath mein karo.
