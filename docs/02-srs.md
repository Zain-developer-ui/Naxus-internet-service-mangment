# 02 — SRS (Software Requirements Specification)

Source: `NEXUS SERVICE MARKETING SYSTEM.doc` + `Project Specification - NEXUS SERVICE MARKETING SYSTEM.DOC`
(legacy OLE2 .doc files; readable text extract `.workbuddy-ai/tmp/*.clean.txt` mein hai)

Note: SRS **2007** ka hai (Aptech ka purana course content). Amounts **USD** mein hain.

---

## 1. Company

**Nexus Communication System** — telecom + internet services vendor.
Local area ka bara vendor, poore territory mein phaila hua.
Retail outlets har city mein khole hain taake customer office tak na jaye.

## 2. Connection types (2)

| Type | Shart |
|---|---|
| **Dial-Up** | Landline lena **compulsory** — aur wo bhi **isi vendor** ki |
| **Broadband** | Landline ki zaroorat nahi |

Equipment: **Modem** ya **Router** — vendor alag alag manufacturers se kharidta hai.

## 3. Feasibility check

Order milne ke **baad** hota hai. Area feasible ho tab hi connection milta hai.

Rules:
- Dial-Up customer ne **landline + internet dono** maanga → **dono** ka check
- Customer ke paas pehle se isi vendor ki landline hai aur ab internet maangta hai
  → sirf internet ka check
- Check kis liye: **distance** (company distance parameter dekhti hai),
  **server availability**, etc.

## 4. Roles (5) — login sab ka apna

### 4.1 Admin (Manager)
- Employee details maintain karna
- Stock / equipment
- Vendors + purchase lists
- Retail shops
- **Plans** aur **charges** — insert / update / delete / search

### 4.2 Accounts Department
- **Bill generate** karna (plan + equipment ke hisaab se charges calculate)
- Charges site par update karna — **sirf yehi kar sakte hain**
- Bill access website se ho

### 4.3 Technical People
- Orders track karna
- Order status update: **feasible hai ya nahi**, **connection mila ya nahi**
- **Naya connection banana** (feasible area mein)
- Connection **inactive** karna — **temporarily** ya **permanently**
- Equipment / product details maintain karna

### 4.4 Retail Outlet Employees
- **Order place** karna — **sirf yehi log order de sakte hain**
- Orders ki till-date list + status
- Connection details (kitne connections diye)
- Till-date billing details
- Till-date payment details
- Customer payment record karna

### 4.5 Customer
- Apna contact details dekhna
- Order place karna
- Order status track karna
- Bills dekhna

## 5. ID formats (ye paper mein important hai)

### Order ID — 11 digit alphanumeric
- Pehla character: `D` = Dial-Up, `T` = Telephone only, `B` = Broadband
- Baqi **10 digits** serial order mein
- Misal: pehla Dial-Up order → `D0000000001`

### Account ID — 16 digit
Connection milne ke baad assign hota hai.
- **1** character — connection type (`D` / `B` / `T`)
- **3** digits — city code (har city ka apna 3-digit numeric code)
- **12** digits — serial number
- Total = 1 + 3 + 12 = **16**

## 6. Charges — Plans

### 6.1 Security Deposit (withdrawal / cancellation par wapas)

| Connection | Deposit |
|---|---|
| Dial-Up | **325$** |
| Broadband | **500$** |
| Landline | **250$** |

### 6.2 Dial-Up Connection

**Hourly basis:**

| Hours | Price | Validity |
|---|---|---|
| 10 Hrs | 50$ | 1 month |
| 30 Hrs | 130$ | 3 months |
| 60 Hrs | 260$ | 6 months |

**Unlimited 28 Kbps:**

| Period | Price |
|---|---|
| Monthly | 75$ |
| Quarterly | 150$ |

**Unlimited 56 Kbps:**

| Period | Price |
|---|---|
| Monthly | 100$ |
| Quarterly | 180$ |

### 6.3 Broadband Connection

**Hourly basis:**

| Hours | Price | Validity |
|---|---|---|
| 30 Hrs | 175$ | 1 month |
| 60 Hrs | 315$ | 6 months |

**Unlimited 64 Kbps:**

| Period | Price |
|---|---|
| Monthly | 225$ |
| Quarterly | 400$ |

**Unlimited 128 Kbps:**

| Period | Price |
|---|---|
| Monthly | 350$ |
| Quarterly | 445$ |

### 6.4 Landline Connection

**Formula: Rental + Call charges**

| Plan | Rental | Validity | Local call | STD call | SMS |
|---|---|---|---|---|---|
| Unlimited | 75$ | 1 year | 55c/min | — | — |
| Monthly | 35$ | 1 month | 75c/min | — | — |
| STD Monthly | 125$ | 1 month | 70c/min | 2.25$/min | 1.00$/min |
| Half-Yearly | 420$ | (SRS mein "1 month" likha hai — **typo lagta hai**) | 60c/min | 2.00$/min | 1.15$/min |
| Yearly | (SRS mein **missing**) | 1 year | 60c/min | 1.75$/min | 1.25$/min |

> **Khula sawal:** Yearly plan ka rental SRS mein missing hai. Aur Half-Yearly ki
> validity "one month" likhi hai jo ghalat lagti hai — 6 months honi chahiye.

### 6.5 Service Tax

**12.24%** — poore generated bill par, customer se charge hota hai.

## 7. Bulk / Corporate Discount

Retain customers ke liye scheme. Discount **advance** aur **security deposit** dono par:

| Connections | Discount |
|---|---|
| 10 – 15 | **25%** |
| 15 – 25 | **50%** |
| 25 – 50 | **75%** |
| 50 se ziyada | **100%** |

## 8. Connection Status

| Status | Matlab |
|---|---|
| **Active** | Chal raha hai |
| **Temporarily Inactive** | Aarzi band |
| **Permanently Inactive** | Hamesha ke liye band |

Bill generate hone par status depend karta hai. **Sirf postpaid** connections hain.

## 9. Search requirements

### 9.1 Simple search (2)
- **Order ID** se → order status
- **Account ID** se → connection status, due amount, status (active/temp/perm inactive)

### 9.2 Advanced search
Options:
- Unique ID
- Jis naam par order/connection hai
- Connection type
- Application date ya connection receive hone ki date / period
- Contact number (jo apply karte waqt diya)

## 10. Non-Financial requirements (verbatim nichor)

Database mein ye hold hona chahiye:
- Customers, employees
- Connection / plan details jo they provide karte hain
- Assigned connections
- Order details
- Retail showroom details
- Product / equipment details
- Billing details
- Payment details (billed amount ke against)

Employee details mein shamil hone chahiye: **Admin, Accounts, Technical, Retail outlet** —
har ek ko credentials milen taake apna kaam kar sake.

Retail login se:
- Orders placed till date
- Order status (feasible / connection mila ya nahi)
- Connection details
- Till-date billing details
- Till-date payment details

Baqi:
- Plans + charges admin maintain kare (insert/update/delete/search)
- Technical login → order status update, naya connection, inactive karna, equipment
- Plan ke hisaab se customer charge ho
- System time to time update ho — naye queries aur naye products ke sath

## 11. Financial requirements

- Bill **Accounts department** generate kare aur application mein update kare —
  website se access ho
- Payment details saaf likhi hon — **amount paid** aur **due amount**
- Charges + plan details admin maintain kare

## 12. Functional requirements (verbatim nichor)

- Creation, maintenance aur updating of database jisme ho:
  - Plans ki information
  - Retail stores ki details
  - Employees ki details
  - Customers, vendors ki details
  - Orders ki details
  - Products ki details
  - Customer ko diye gaye materials ki details
- Orders ke records maintain karna + **customer ka feedback collect** karna
- Billing calculate karna — customer ke **discount entitlement** aur **schemes**
  ko dekh kar, aur **pehle ki payment** ko adjust kar ke

## 13. Aptech ka process (doosri document se)

### Standards
- *"Every code block must have comments"* — har code block par comment
- Logic explain hona chahiye
- Proper documentation maintain karo
- Complete Project Report — synopsis + code + documentation

### Documentation checklist (project report mein ye sab chahiye)
1. Certificate of Completion
2. Table of Contents
3. Problem Definition
4. Customer Requirement Specification
5. Project Plan
6. **E-R Diagrams**
7. **Algorithms**
8. **GUI Standards Document**
9. **Interface Design Document**
10. Task Sheet
11. Project Review and Monitoring Report
12. **Unit Testing Check List**
13. Final Check List

### Deliverables
- **2 status emails** — har 10 din baad (agar project 30 din se kam ho to
  pehla 7–10 din mein, doosra end date se 3 din pehle)
- Status mail mein review document ho
- Submission ke waqt **feedback form** + documentation (soft copy)
- Email subject `STATUS:` / `DOUBT:` / `PROJECT SUBMISSION:` se shuru ho

### Suggested software (SRS ke zamaane ka)
Notepad/HTML editor, Dreamweaver/JavaScript, j2sdk1.4.1_02 / .Net / J2EE,
JSP/Servlets, EJB/Struts, JDBC, SQL Server 2000 / Oracle 9i / MS Access.

> Ye list 2007 ki hai. Hum **ASP.NET Core 8 + SQL Server** use kar rahe hain,
> jo `.Net` option ke andar aata hai.

---

## Khule sawal (SRS mein ambiguity)

1. **Yearly landline plan ka rental** missing hai
2. **Half-Yearly plan ki validity** "1 month" likhi hai — ghalat lagti hai
3. ~~SRS mein **USD** hai; implementation **PKR** use kar raha hai — kaunsa sahi hai?~~
   **RESOLVED (5 Oct 2026): USD rakhenge.** SRS hi source of truth hai. Frontend ke
   saare PKR symbols (`Rs. 1,200 / 2,000 / 3,500`) ko SRS ke USD plans se replace
   karna hai — dekho `docs/11-implementation-plan.md` ka Step 1.2.
4. Service tax **12.24%** — ye India ka VAT era tha; Pakistan ka tax rate alag hai?
   **Filhaal 12.24% hi rakhenge** — SRS mein yahi likha hai aur examiner SRS se
   check karega. Tax rate ek constant (`TaxConstants.ServiceTaxRate`) mein rakhenge
   taake baad mein ek line se badla ja sake.
5. Equipment charges (modem/router) ka koi rate SRS mein nahi — sirf
   "replacement charges if customer spoils it" ka zikr hai
   **Filhaal equipment pricing ko scope se bahar rakhenge** — sirf inventory
   track karenge (kis shop par kitna stock), price column nullable.
