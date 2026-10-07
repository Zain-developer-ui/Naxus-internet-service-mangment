# 09 — Testing Checklist

NEXUS Service Marketing System. Har phase ke baad ye checklist chalani hai.
Status: `[ ]` = pending, `[x]` = pass, `[!]` = fail (issue number likho).

---

## 0. Pre-check — environment

| # | Check | Kaise | Status |
|---|---|---|---|
| 0.1 | Solution build hota hai | `dotnet build` — 0 errors | [x] |
| 0.2 | App start hota hai | `dotnet run`, port 5000/5001 sunta hai | [ ] |
| 0.3 | SQL Server reachable | connection string test | [ ] |
| 0.4 | Migrations pending nahi | `dotnet ef migrations list` | [ ] |
| 0.5 | Browser console clean | F12 → Console, koi 404/error nahi | [ ] |
| 0.6 | Network tab clean | koi external CDN request nahi (offline demo) | [ ] |

---

## 1. Asset & console health (har page par)

Ye cheezein har public + dashboard page par check karo. Ek bhi fail ho to page broken hai.

| # | Check | Expected |
|---|---|---|
| 1.1 | `validation.js` load | 200, koi 404 nahi |
| 1.2 | `site.js` load | 200, `window.NEXUS` defined |
| 1.3 | `NEXUS.getUser()` call safe | koi `TypeError` nahi jab user null ho |
| 1.4 | `NEXUS.saveUser()` / `clearUser()` | sessionStorage set/remove kar raha hai |
| 1.5 | `NEXUS.toast()` | toast render hota hai, throw nahi karta |
| 1.6 | Images | 0 missing, 0 external (images.unsplash/pravatar) |
| 1.7 | Fonts | local ya gracefully fallback |
| 1.8 | CSS order | general → specific, `responsive.css` sab se aakhir |

Pages to sweep: Home, Services, Plans, Coverage, Contact, About, Login, Register,
Account/Status, Orders/New, Orders/Tracking, Feedback, Profile, aur 5 dashboards
(Admin, Accounts, Technical, Retail, Customer).

---

## 2. Visual / design QA

| # | Check | Expected |
|---|---|---|
| 2.1 | Hardcoded hex | `home.css`, `login-stars.css` mein 0 |
| 2.2 | Tokens used | rang `var(--nexus-*)` se aa rahe hain |
| 2.3 | Text-on-dark contrast | `--nexus-text-invert` use hua, `--nexus-surface` nahi |
| 2.4 | No emoji in UI | 0 emoji views mein |
| 2.5 | No fake stats | 0 `data-count`, "1,245", "4.8", "42 Cities" |
| 2.6 | No fake testimonials | 0 jhooti names/companies |
| 2.7 | No navbar search bar | confirmed removed (user preference) |
| 2.8 | No gradients/glass/neon | sirf intentional, minimal |
| 2.9 | Avatars | initials-based, koi pravatar nahi |
| 2.10 | Star rating render | empty stars `star-off` ke saath dikhein |
| 2.11 | Responsive 360px | koi horizontal scroll nahi |
| 2.12 | Responsive 768 / 1024 / 1440 | layout sane |
| 2.13 | Print stylesheet | bill/report print sane |

---

## 3. Client-side validation

`validation.js` + MVC attributes. Har form par.

| # | Check | Expected |
|---|---|---|
| 3.1 | Required field khali | inline `.field-error`, submit block |
| 3.2 | Email format | invalid par error, valid par pass |
| 3.3 | Phone format | CNIC/phone pattern |
| 3.4 | Number range | min/max enforce |
| 3.5 | Error styling | `.input-error` class + red border |
| 3.6 | Error clear on input | type karte hi error hatta hai |
| 3.7 | Double submit block | button disable/spinner |
| 3.8 | Server error bhi inline | ModelState error ke saath `.field-error` |
| 3.9 | No JS → form still works | progressive enhancement |

Forms: Login, Register, Account/Status search, Orders/New, Feedback, Profile edit,
Contact, aur har dashboard ka create/edit form.

---

## 4. Authentication

| # | Check | Expected |
|---|---|---|
| 4.1 | Anonymous dashboard access | `/Admin/Dashboard` → redirect login |
| 4.2 | Login valid creds | dashboard par redirect, cookie set |
| 4.3 | Login invalid creds | generic error, "user not found" leak nahi |
| 4.4 | Password hashing | DB mein hash, plaintext nahi |
| 4.5 | Logout | cookie clear, protected page block |
| 4.6 | Session expiry | timeout ke baad re-login |
| 4.7 | Return URL | login ke baad wapas usi page par |
| 4.8 | Remember-me | agar hai, sirf explicit |
| 4.9 | Lockout | N failed attempts → lock |
| 4.10 | Frontend sessionStorage | browser band → logout |

---

## 5. Authorization (5 roles)

Roles: Admin, Accounts, Technical, Retail, Customer.

| # | Check | Expected |
|---|---|---|
| 5.1 | Customer → Admin area | 403 |
| 5.2 | Retail → Accounts area | 403 |
| 5.3 | Technical → sirf apne assigned orders | filtered, dusra ID par 403 |
| 5.4 | Accounts → bills/payments only | 403 baaki par |
| 5.5 | Admin → sab | allowed |
| 5.6 | Direct URL guess | har role cross-check |
| 5.7 | IDOR | dusre customer ka order/bill ID → 404/403 |
| 5.8 | Menu visibility | role ke hisaab se links hide |
| 5.9 | Server-side enforce | sirf UI hide kaafi nahi, action par bhi |

---

## 6. Database / EF Core

| # | Check | Expected |
|---|---|---|
| 6.1 | Migrations apply | `dotnet ef database update` clean |
| 6.2 | Seed data | cities, plans, price tiers, bulk tiers |
| 6.3 | FK constraints | orphan insert fail |
| 6.4 | Unique constraints | duplicate AccountId fail |
| 6.5 | Cascade rules | delete par expected behaviour |
| 6.6 | Indexes | search/lookup columns par |
| 6.7 | Concurrency token | simultaneous edit → `DbUpdateConcurrencyException` handle |
| 6.8 | Transaction | order + lines atomic |
| 6.9 | Decimal precision | paisa `decimal(18,2)`, no float |
| 6.10 | Soft delete (agar hai) | queries filter karti hain |

---

## 7. Business logic (SRS se)

| # | Check | Expected |
|---|---|---|
| 7.1 | Dial-Up landline requirement | landline nahi → reject messaging |
| 7.2 | Order ID format | `D`/`T`/`B` + 10 digits |
| 7.3 | Account ID format | 1 letter + 3-digit city + 12-digit serial |
| 7.4 | Service Tax | 12.24% correct applied |
| 7.5 | Bulk discount tiers | 25 / 50 / 75 / 100% sahi slab |
| 7.6 | Security Deposit | 325 / 500 / 250$ per type |
| 7.7 | Package price | 10h/30h/60h/Unlimited correct |
| 7.8 | Broadband price | 30h/60h/Unlimited 64K/128K correct |
| 7.9 | Feasibility check | result order create se pehle |
| 7.10 | Currency | ek hi currency consistently (BLOCKER — dekho `02-srs.md`) |
| 7.11 | Bill total | lines + tax + discount = header total |
| 7.12 | Payment partial | balance sahi update |

---

## 8. Security

| # | Check | Expected |
|---|---|---|
| 8.1 | CSRF | har POST par antiforgery token |
| 8.2 | SQL injection | EF param queries, koi raw concat nahi |
| 8.3 | XSS | Razor auto-encode; `Html.Raw` sirf verified |
| 8.4 | Overposting | `[Bind]` / separate ViewModel |
| 8.5 | Mass assignment | role field user se accept nahi |
| 8.6 | Sensitive data in logs | password/token nahi |
| 8.7 | Error disclosure | prod mein stack trace nahi |
| 8.8 | HTTPS | HSTS + redirect |
| 8.9 | Cookie flags | HttpOnly, Secure, SameSite |
| 8.10 | Rate limiting | login + public search par |
| 8.11 | File upload (agar hai) | type + size + renaming |
| 8.12 | Secrets | appsettings mein plaintext nahi, user-secrets |

---

## 9. API / service layer

| # | Check | Expected |
|---|---|---|
| 9.1 | `Result<T>` envelope | har call consistent shape |
| 9.2 | ErrorKind mapping | NotFound/Validation/Conflict sahi |
| 9.3 | Null handling | empty result crash nahi karta |
| 9.4 | Paging | `PagedResult<T>` sahi counts |
| 9.5 | Validation 3-layer | client + ModelState + service |
| 9.6 | Rollback | service fail → data adhoora nahi |

---

## 10. Error handling

| # | Check | Expected |
|---|---|---|
| 10.1 | 404 page | branded, navbar wapas |
| 10.2 | 500 page | branded, no stack trace |
| 10.3 | 403 page | branded |
| 10.4 | Exception middleware | log + friendly page |
| 10.5 | Validation summary | user ko samajh aaye |
| 10.6 | Empty state | "no records" message, blank table nahi |
| 10.7 | Network fail (fetch) | toast error, silent fail nahi |

---

## 11. Performance

| # | Check | Expected |
|---|---|---|
| 11.1 | N+1 queries | logs mein repeated queries nahi |
| 11.2 | `AsNoTracking` | read-only queries par |
| 11.3 | Paging server-side | sab rows load nahi |
| 11.4 | Indexes used | execution plan check |
| 11.5 | Static asset caching | cache headers set |
| 11.6 | CSS/JS minified | prod build |
| 11.7 | Images | lazy load + reasonable size |
| 11.8 | First paint | dashboard 2s se kam (local) |

---

## 12. Accessibility

| # | Check | Expected |
|---|---|---|
| 12.1 | Keyboard nav | sab interactive elements reachable |
| 12.2 | Focus visible | focus ring har element par |
| 12.3 | Alt text | har meaningful image par |
| 12.4 | Decorative images | `aria-hidden="true"` |
| 12.5 | Labels | har input ka `<label>` |
| 12.6 | Error announce | `aria-live` / associated |
| 12.7 | Contrast | WCAG AA (4.5:1) |
| 12.8 | Tables | `<th scope>` set |
| 12.9 | Skip link | main content tak |
| 12.10 | Headings | logical h1→h6 order |
| 12.11 | Icons-only buttons | `aria-label` |

---

## 13. Smoke test — demo se pehle (final)

Ye 20 steps ka script demo se pehle chalana hai. Sab pass chahiye.

1. `dotnet build` → 0 errors
2. App start → home page load
3. Home page: Console clean, images local
4. Register ek customer
5. Login karo
6. Naya order create (feasibility pass)
7. Order ID format check
8. Tracking page open
9. Toast shows correctly
10. Logout
11. Admin login
12. Admin dashboard: real counts (fake nahi)
13. Ek plan price edit → customer par reflect
14. Bill generate → tax + discount correct
15. Payment record → balance update
16. Customer dusre ka bill ID guess → 403/404
17. Technical role → sirf assigned orders
18. Feedback submit + star render
19. 360px responsive check
20. 404 page check

---

## Known gaps (jhooti umeed na dena)

Ye cheezein **abhi test nahi ho sakteen** kyunki feature nahi hai:

- DB-backed kuch bhi — DB nahi hai
- Authentication / authorization — implement nahi
- Billing calculations — logic nahi
- Feasibility engine — nahi
- Search — nahi
- API layer — nahi

Ye sab Phase 4 ke baad test honge. Abhi sirf sections 1, 2, 3 (partial) green hain.
