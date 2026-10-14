# NEXUS — TestSprite Testing Plan

Ye document batata hai ke NEXUS ko TestSprite se kaise test karna hai, aur
Aptech ke "Testing" deliverable mein kya jama karna hai.

`PLAN-4-DAYS.md` section 5A ka tafseeli version.

---

## 1. TestSprite kya hai — ek line mein

Ek AI testing service. Aap app ka URL dete ho, wo khud browser kholta hai,
real user ki tarah click/fill karta hai, aur pass/fail report deta hai.
Test likhne ki zaroorat nahi.

---

## 2. Sabse pehle ye samajh lo — tests kahan chalte hain

**TestSprite ke tests cloud mein chalte hain, aap ki machine par nahi.**

Iska matlab: un ka cloud runner aap ke app tak **public internet se** pohnchna
chahiye. Aap ka app `localhost:5199` par hai — wo wahan se reachable nahi.

Un ki docs ka apna wording:

> the cloud runner must be able to reach the URL over the public internet

Aur CLI ka behaviour:

> `test run` and `project create` pre-flight `--target-url` and **reject local
> addresses** (`localhost`, `127.x`, `::1`, `0.0.0.0`, link-local, and
> RFC 1918 ranges)

### Teen surfaces

| Surface | Kya hai | Localhost? | CI? |
|---|---|---|---|
| **Web Portal** | browser dashboard, PRD upload, schedules | ❌ | — |
| **CLI** | `npm i -g @testsprite/testsprite-cli` | ✅ **`testsprite tunnel` ke saath** | ✅ |
| **MCP Server** | IDE plugin, khud tunnel kholta hai | ✅ | ❌ |

**NEXUS ke liye: CLI + tunnel.** App local chalti hai aur SQL Server bhi local hai.
`testsprite tunnel` ek public URL banata hai jo TestSprite ke cloud runner tak
pohnchta hai. Tunnel sirf HTTP endpoint expose karta hai — DB wahi rehta hai.

CLI khud `--target-url` par localhost **reject** karta hai, magar **tunnel ka URL
localhost nahi hota** — is liye tunnel ke baad CLI normal kaam karta hai. Ye
MCP se behtar hai kyunke scriptable hai aur CI mein bhi chalega.

**Tunnel ko alag terminal mein chalta rehna hota hai** jab tak tests chal rahe hain.

---

## 3. Requirements

| Cheez | Detail |
|---|---|
| Node.js | **20.19+ ya 22.13+ ya 24+** — **21.x aur 23.x supported NAHI hain** |
| Account | TestSprite account + API key |
| App | `dotnet run` se chal rahi ho |
| PRD | SRS document — **ye already mojood hai** |

Aap ke machine par Node 22.22.2 aur 24.16.0 dono hain — dono theek hain.

---

## 4. Setup — step by step

### Step 1 — CLI install karo

```
npm install -g @testsprite/testsprite-cli
```

Agar global install nahi chahiye:

```
npx @testsprite/testsprite-cli --version
```

### Step 2 — Onboarding

```
testsprite setup
```

Ye API key maangega, verify karega, aur aap ke coding agent mein ek verification
skill install kar dega.

Non-interactive (CI ke liye):

```
testsprite setup --from-env --yes --no-agent
```

### Step 3 — Health check

```
testsprite doctor
```

Ye CLI version, Node runtime, active profile, credentials, live connectivity aur
skill installation — sab check karta hai. **Kuch bhi ajeeb lage to pehle ye chalao.**

### Step 4 — Tunnel kholo (localhost ke liye)

Alag cmd window kholo (ye chalta rehna hai):

```
testsprite tunnel
```

Ye ek public URL deta hai. **Us URL ko copy kar lo** — agli step mein wahi use hoga.

### Step 5 — App chalao

Doosri cmd window mein:

```
dotnet run --urls http://0.0.0.0:5199
```

`0.0.0.0` par bind karna zaroori hai, `localhost` par nahi — warna tunnel
app tak nahi pohnch payega.

### Step 6 — Project banao

`--local <port>` use karo. Runs khud tunnel khol lete hain, is liye alag tunnel
window ki zaroorat nahi:

```
testsprite project create --type frontend --name "NEXUS" --local 5199 --username admin@nexus.example --password Nexus@2027
```

**Ho gaya.** Project ID:

```
c0ab2de3-d588-4213-ba76-6a119dae1909
```

CLI ne kya kaha:

> Local project: TestSprite will reach `http://localhost:5199` only through a
> tunnel from this machine.
> Portal runs of this project stay blocked (free) until you set a public URL.

Yani **tunnel runs chalti hain, Portal runs nahi** (Free plan). Humen tunnel hi
chahiye, to ye theek hai.

### Step 7 — Plan likho

```
testsprite test scaffold
```

Ye ek schema-correct starter plan deta hai. Usme `projectId` set karo, phir
test cases likho. Validate offline karo (free, no network, no credits):

```
testsprite test lint --plan-from plan.json
```

### Step 8 — Test banao aur chalao

```
testsprite test create --project c0ab2de3-d588-4213-ba76-6a119dae1909 --plan-from plan.json
```

```
testsprite test run <test-id>
```

Ya poora project ek saath (loopback environments ek hi tunnel share karte hain):

```
testsprite test run --all --project c0ab2de3-d588-4213-ba76-6a119dae1909
```

### Step 9 — Failure bundle padho

```
testsprite test artifact get <run-id>
```

Har failure ek bundle deta hai: failing step, screenshots, DOM snapshot, test
source, root cause hypothesis, aur suggested fix. Fix karo, rerun karo.

### Kaam ke commands jo help se mile

| Command | Kaam |
|---|---|
| `test lint --plan-from x.json` | Plan offline validate — **free, no credits** |
| `test scaffold` | Schema-correct starter plan |
| `test plan` | AI se plan generate karo (V3) |
| `test create-batch` | JSONL se ek saath kai FE tests |
| `test result <id>` | Natija dekho |
| `test result <id> --history` | Purane runs |
| `test steps <id>` | Latest run ke steps |
| `test diff <run-a> <run-b>` | Do runs compare — **regression pata chalta hai** |
| `test flaky <id>` | Bar bar chala kar stability check (auto-heal OFF) |
| `test open <id>` | Dashboard mein kholo |
| `test cancel <run-id>` | Run rok do (Ctrl-C sirf detach karta hai) |
| `usage` | Credits dekho |

**Exit codes:** `0` pass, `1` fail/blocked, `7` timeout (resume: `test wait <run-id>`),
`11` rate limited, `12` credits khatam.

### Billing

FE rerun = **0.5 credits**. Fresh FE run bhi credits leta hai. Free plan mein
**150** hain — is liye pehle 2–3 tests par try karo, phir barhao.

**Aur ek baat:** CLI kehta hai "Local environments: dev servers can take a minute
per page through the tunnel." Yani tests **dheeme** chalenge — normal hai.

---

## 5. ⚠️ Demo se pehle ye teen cheezein zaroor dekhna

### 5.1 Login rate limit — ye test run todega

App mein login par **per-IP 10/min** fixed window hai, aur **per-account
lockout** bhi. TestSprite "dozens of agents at once" chalata hai — cloud IPs se
ek saath login karenge.

Natija: rate limit lag jayega, account lock ho jayega, aur poora test run fail.

**Karna kya hai:**
- Test run se pehle login rate limit **temporarily hatao ya barhao**
- Test ke liye **alag dedicated account** banao — `admin@nexus.example` ko
  test mein mat jhonko
- Agar lock ho jaye:
  ```sql
  UPDATE Users SET AccessFailedCount=0, LockoutEnd=NULL WHERE Email='...';
  ```

### 5.2 Database timeout — ye is machine par already hua hai

12 Oct ko login POST fail hua tha, aur wajah password nahi thi:

```
Connection Timeout Expired. The timeout period elapsed during the post-login phase.
[Pre-Login] initialization=510; handshake=1110; [Post-Login] complete=13079;
```

SQL Server chal raha tha aur `Users` mein 6 rows thin — yani ye **transient**
tha, connection leak nahi. Connection string `DESKTOP-BD6OLPC\SQLEXPRESS` par
hai aur handshake ~1 second le raha hai, to server doosri machine par lagta hai.

**Iska matlab:** test run ke waqt DB reachable hona chahiye, warna har test
"Blocked" dega aur aap ko lagega app tooti hui hai. **Pehle `testsprite doctor`
aur ek manual login se confirm karo ke sab chal raha hai.**

### 5.3 Test accounts

| Role | Email | Password |
|---|---|---|
| Admin | `admin@nexus.example` | `Nexus@2027` |
| Retail | `retail@nexus.example` | `Nexus@2027` |
| Technical | `technical@nexus.example` | `Nexus@2027` |
| Accounts | `accounts@nexus.example` | `Nexus@2027` |

Sab ka password **`Nexus@2027`** hai. Seeder file mein `Nexus@2026` likha hai —
wo **purani** value hai, DB mein nahi chalti.

---

## 6. Kya test karna hai — NEXUS ke liye concrete list

Ye list SRS ke against hai. Har item ek end-to-end journey hai, jo TestSprite
khud drive kar sakta hai.

### 6.1 Public / anonymous

| # | Journey | Pass ka matlab |
|---|---|---|
| 1 | Homepage khulti hai, featured plans DB se aate hain | Fake numbers nahi, real plans |
| 2 | `/Plans` par teen connection types ke plans | Dial-Up / Broadband / Telephone sab |
| 3 | `/Services` page load | — |
| 4 | `/Home/Contact` form submit → success | Contact message DB mein |
| 5 | `/Orders/Tracking` par ghalat order id | Saaf error, crash nahi |
| 6 | Ghalat URL | Branded 404, default IIS page nahi |
| 7 | `/Orders/New` se naya customer register | Order + account id bane |

### 6.2 Auth / authorization — yahan sabse zyada bugs milte hain

| # | Journey | Pass ka matlab |
|---|---|---|
| 8 | Ghalat password se login | Error message, crash nahi |
| 9 | Har role apne dashboard par | Admin → `/Admin/Dashboard` etc. |
| 10 | **Retail user `/Admin/Plans` kholne ki koshish** | **403 ya redirect — 200 nahi** |
| 11 | **Technical user plan edit karne ki koshish** | **Ruk jaye** |
| 12 | **Customer `/Reports` kholne ki koshish** | **Ruk jaye** |
| 13 | Logout → protected page | Wapas login par |

**10, 11, 12 sabse ahem hain.** Ye authorization ke real tests hain, aur
evaluator yehi poochta hai.

### 6.3 Admin

| # | Journey | Pass ka matlab |
|---|---|---|
| 14 | Naya plan banao (Dial-Up, 10 Hours, $50) | Plan list mein aaye |
| 15 | Plan edit karo, tariff sirf metered line par | Broadband par tariff field na dikhe |
| 16 | Naya vendor banao | List mein aaye |
| 17 | Naya equipment product, preferred vendor set karo | Product par vendor save ho |
| 18 | Purchase order: raise → receive → stock | Stock barhe |
| 19 | Outlet + staff add karo | Employee list mein aaye |
| 20 | City add karo, phir "stop serving" | Dobara on ho sake |
| 21 | Discount slab edit | Save ho |

### 6.4 Retail

| # | Journey | Pass ka matlab |
|---|---|---|
| 22 | Naya order place karo | Order id 11-digit bane |
| 23 | Order status dekho | Timeline real status se |
| 24 | Till-date register | Orders / connections / billing / payments chaar tabs |
| 25 | Advanced search — 5 filters | Filter kaam kare |
| 26 | Search by account id | Sahi customer mile |

### 6.5 Technical / Operations

| # | Journey | Pass ka matlab |
|---|---|---|
| 27 | Survey queue — feasibility run | Distance/capacity rules lagen |
| 28 | Feasibility fail ho to connection na bane | Guard kaam kare |
| 29 | Installation complete karo | Connection Active ho |
| 30 | Overdue policy page | 15 din → Temp Inactive, 45 din → Perm Inactive |
| 31 | Stock adjust karo | Quantity badle |

### 6.6 Accounts / Billing — **yahan sabse zyada value hai**

| # | Journey | Pass ka matlab |
|---|---|---|
| 32 | Monthly billing run chalao | Bills banein |
| 33 | Bill par 12.24% tax | Arithmetic sahi |
| 34 | **Bulk discount sirf advance + security deposit par** | Rental par discount **na** lage |
| 35 | Discount tier — 12 lines → 25% | Tier sahi |
| 36 | First payment adjust | Sirf **ek dafa** adjust ho |
| 37 | Replacement charge raise karo | Pending list mein aaye |
| 38 | Agli bill bane to charge khud lag jaye | Line item mein aaye |
| 39 | Call metering entry | Bill par call charges |
| 40 | Payment record karo | Bill status update ho |

**34 aur 36 sabse ahem hain** — ye do business rules hain jo SRS mein explicit
hain aur aasani se toot sakte hain.

### 6.7 Customer portal

| # | Journey | Pass ka matlab |
|---|---|---|
| 41 | Customer login → apna dashboard | Sirf apna data |
| 42 | My Connection page | Apni line ki details |
| 43 | Bills & payments | Apni bills |
| 44 | Feedback submit → success modal | DB mein save ho |
| 45 | Profile update | Save ho |

### 6.8 Accessibility / responsiveness (bonus)

| # | Journey | Pass ka matlab |
|---|---|---|
| 46 | Keyboard se sirf Tab dabao | Skip link pehle focus mein aaye |
| 47 | Modal khulne par Tab | Focus modal ke andar rahe, bahar na jaye |
| 48 | Mobile width (390px) par navbar | Hamburger kaam kare |
| 49 | Mobile par sidebar | Overlay se band ho |

---

## 7. Aptech deliverable mein kya jama karna hai

Ye teen cheezein TestSprite se nikal sakti hain — aur teenon strong evidence hain:

| Cheez | Kahan se | Kis kaam ki |
|---|---|---|
| **Test plan** | Plan review screen | "Unit Testing Check List" (6.5) |
| **Run report** | Har run ke baad | Passed / Failed / Blocked ka record |
| **Generated test code** | GitHub export | **Playwright + Python** — chalne wali files |

**Generated code sabse qeemti hai.** Yani aap ke Testing deliverable mein sirf
screenshots nahi honge — actual runnable test files hongi. Ye evaluator ke liye
bohat different cheez hai.

Screenshots ke saath har failure ka **DOM snapshot aur video** bhi milta hai.

---

## 8. Pricing

| Plan | Price | Credits/mo | Environments |
|---|---|---|---|
| **Free** | $0 | 150 | 1 |
| Starter | $19/mo (pehla month free) | 400 | 1 |
| Standard | $39/mo | 800 | 3 |
| Pro | $69/mo | 1,600 | Unlimited |

Free tier mein: 1 environment, 30 din run history, CLI + IDE plugin + MCP,
GitHub PR auto-test 1 repo.

**Try karne ke liye free kaafi hai.** 150 credits se upar wali list ke core
journeys (6.2 auth, 6.6 billing) cover ho jane chahiye.

---

## 9. Agar TestSprite na chale — fallback

| Masla | Hal |
|---|---|
| Tunnel ban na raha | App ko publicly deploy karo, phir CLI/Portal |
| Credits khatam | `TESTING-GUIDE.md` ke 70 manual test cases |
| Cloud runner login nahi kar pa raha | Rate limit hatao, dedicated account banao |
| DB reachable nahi | SQL Server check karo — `testsprite doctor` se pehle manual login |

**Fallback already mojood hai:** `TESTING-GUIDE.md` mein ~70 manual test cases
hain, jo aap ka team member chala sakta hai. TestSprite us ka replacement nahi,
**upar ka hissa** hai.

---

## 10. Honest expectation

- **Achha chalega:** auth/authorization journeys, form validation, CRUD screens,
  error pages. Ye deterministic hain aur TestSprite inhein aasani se drive karta hai.
- **Mushkil ho sakta hai:** billing arithmetic ke exact values. TestSprite
  "kya user ye kar sakta hai" verify karta hai — "tax theek 12.24% hai" nahi.
  **Billing ke numbers ke liye `TESTING-GUIDE.md` ke manual cases hi behtar hain.**
- **Sabse bara risk:** rate limit + DB timeout. Dono ka ilaaj section 5 mein hai.

---

## Related

- `PLAN-4-DAYS.md` — section 5A (chhota version)
- `TESTING-GUIDE.md` — ~70 manual test cases (fallback + billing arithmetic)
- `TASK-LEDGER.md` — overall progress
