# 07 — API Contract

Status: **DESIGN** (implementation pending)

Ye dekhta hai ke backend aur frontend kaise baat karenge. Abhi `IApiService`
mock hai — ye contract usko replace karega.

---

## Faisla: kaunsa pattern

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| **A. Controllers se seedha EF Core** | Kam code, single app | Frontend aur DB couple, test mushkil | Nahi |
| **B. Minimal API `/api/*` + `IApiService` HTTP client** | Frontend abhi jo design maangta hai wahi milta, alag test ho sakta | Do layers, HTTP overhead | Nahi |
| **C. `IApiService` ka server-side implementation (seedha EF Core)** | `IApiService` design barkarar, koi HTTP overhead nahi, ek hi app | Interface method signatures ko theek karna padega | **Haan** |

**C chuna** — kyunki `IApiService` abstraction pehle se sab jagah use ho raha hai.
Sirf `ApiService` (mock) ki jagah `EfApiService` as a real implementation daalenge.
Business logic `Services/` layer mein rahegi, `EfApiService` sirf DTO mapping karega.

Agar baad mein alag API server chahiye ho, to `HttpApiService` likh kar DI mein swap
kar do — frontend ko pata bhi na chalega. Ye design ka poora maqsad yahi tha.

---

## `IApiService` — current vs required

### Abhi (mock)
```csharp
Task<List<ServiceViewModel>> GetServicesAsync();
Task<ServiceViewModel?> GetServiceAsync(int id);
Task<List<PlanViewModel>> GetPlansAsync(string? serviceType = null);
Task<PlanViewModel?> GetPlanAsync(int id);
Task<AccountViewModel?> GetCustomerAsync(string accountId);
Task<AccountViewModel?> GetCustomerByPhoneAsync(string phone);
Task<AccountViewModel?> GetCustomerByCnicAsync(string cnic);
Task<OrderViewModel?> GetOrderAsync(string orderId);
Task<bool> SubmitOrderAsync(OrderViewModel order);
Task<List<BillViewModel>> GetBillsAsync(string accountId);
Task<BillDetailsViewModel?> GetBillDetailsAsync(string billNo);
Task<bool> SubmitPaymentAsync(string billNo, decimal amount, string method);
Task<bool> SubmitFeedbackAsync(FeedbackViewModel feedback);
Task<CustomerDashboardViewModel> GetCustomerDashboardAsync(string accountId);
```

### Zaroori additions

```csharp
// --- Identity ---
Task<LoginResult> SignInAsync(string accountIdOrEmail, string password, bool remember);
Task SignOutAsync();

// --- Orders (retail/technical) ---
Task<PagedResult<OrderSummaryViewModel>> ListOrdersAsync(OrderFilter filter);
Task<string> CreateOrderAsync(NewOrderRequest request);       // returns OrderNumber

// --- Feasibility (technical) ---
Task RecordFeasibilityAsync(FeasibilityRequest request);

// --- Connections (technical) ---
Task<ConnectionViewModel?> GetConnectionByAccountIdAsync(string accountId);
Task<string> InstallConnectionAsync(int orderId);              // returns AccountId
Task ChangeConnectionStatusAsync(int connectionId, ConnectionStatus status, string reason);

// --- Billing (accounts) ---
Task<PagedResult<BillSummaryViewModel>> ListBillsAsync(BillFilter filter);
Task<string> GenerateBillAsync(int connectionId, DateOnly period);

// --- Payments (retail/accounts) ---
Task RecordPaymentAsync(int billId, decimal amount, PaymentMethod method, string reference);

// --- Admin CRUD ---
Task<PagedResult<VendorViewModel>> ListVendorsAsync(Paging p);
Task<int> SaveVendorAsync(VendorViewModel vendor);
Task DeleteVendorAsync(int id);

Task<PagedResult<EmployeeViewModel>> ListEmployeesAsync(Paging p);
Task<int> SaveEmployeeAsync(EmployeeViewModel employee);

Task<PagedResult<RetailShopViewModel>> ListRetailShopsAsync(Paging p);
Task<int> SaveRetailShopAsync(RetailShopViewModel shop);

Task<PagedResult<StockItemViewModel>> ListStockAsync(Paging p);
Task AdjustStockAsync(int stockItemId, int delta, string reason);

Task<int> SavePlanAsync(PlanViewModel plan);
Task SavePlanPriceAsync(int planId, PlanPriceViewModel price);

// --- Search (SRS advanced) ---
Task<PagedResult<SearchResultViewModel>> AdvancedSearchAsync(SearchFilter filter);

// --- Dashboard aggregate (role-wise) ---
Task<AdminDashboardViewModel> GetAdminDashboardAsync();
Task<AccountsDashboardViewModel> GetAccountsDashboardAsync();
Task<TechnicalDashboardViewModel> GetTechnicalDashboardAsync();
Task<RetailDashboardViewModel> GetRetailDashboardAsync();
```

---

## Result envelope

Har method `Task<bool>` nahi dega — fail hone ki wajah bhi chahiye.

```csharp
public record Result<T>(bool Success, T? Value, string? Error, ErrorKind Kind)
{
    public static Result<T> Ok(T value) => new(true, value, null, ErrorKind.None);
    public static Result<T> Fail(string error, ErrorKind kind) => new(false, default, error, kind);
}

public enum ErrorKind
{
    None,
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    Unexpected
}
```

**Kyun:** `bool` return karne se controller ko pata nahi chalta ke user ko
"not found" dikhana hai ya "server error". Kind se status code aur message map hota hai.

---

## Validation strategy — 3 layers

SRS aur safety dono ke liye:

| Layer | Kahan | Kaam |
|---|---|---|
| **1. Client (JS)** | `validation.js` | Fauran feedback. Bypass ho sakta hai — is liye sirf UX. |
| **2. ViewModel attributes** | `Models/ViewModels/*.cs` | `[Required]`, `[RegularExpression]`, `[StringLength]` — `ModelState` validate karta hai |
| **3. Service layer** | `Services/` | Business rules jo attributes se nahi hoti: feasibility, duplicate CNIC, discount tier, plan active hai ya nahi, transition valid hai ya nahi |

**Rule:** Layer 3 hamesha chalti hai, chahe layer 2 pass ho jaye. Attributes
"shape" check karti hain, business rules nahi.

Misal — connection status transition:
```csharp
// PermanentlyInactive se wapas nahi ja sakte. Ye attribute se nahi hota.
private static readonly Dictionary<ConnectionStatus, ConnectionStatus[]> Allowed =
    new()
    {
        [ConnectionStatus.Active] = [ConnectionStatus.TemporarilyInactive,
                                     ConnectionStatus.PermanentlyInactive],
        [ConnectionStatus.TemporarilyInactive] = [ConnectionStatus.Active,
                                                  ConnectionStatus.PermanentlyInactive],
        [ConnectionStatus.PermanentlyInactive] = []
    };
```

---

## Error handling

### Global pipeline
1. `app.UseExceptionHandler` — unhandled exceptions
2. Custom `IExceptionHandler` — log full detail, user ko safe message
3. `ModelState` invalid → same view + errors (POST ke liye)
4. `Result.Fail` → `TempData["Error"]` + redirect, ya problem-details response

### Kya user ko dikhana hai

| Situation | User ko | Log mein |
|---|---|---|
| Validation fail | Field-level message | Debug only |
| Not found | "Record not found" | ID + user |
| Unauthorized | Login page | IP + attempted path |
| Forbidden | "You do not have access" | User id + role + path |
| Conflict | "Record was changed by someone else" | Concurrency detail |
| Unexpected | "Something went wrong. Reference: {id}" | Full stack + reference |

**Rule:** Kabhi bhi stack trace, SQL error, ya file path user ko na dikhe.
Reference id dikhao — support us se log dhoond sakta hai.

---

## Endpoint mapping (controller-wise)

| Controller | Action | Method | Role required |
|---|---|---|---|
| `Account` | `Login` GET/POST | — | Anonymous |
| `Account` | `Logout` POST | — | Authenticated |
| `Account` | `Status` GET/POST | — | Anonymous (2 min rate limit) |
| `Services` | `Index` | — | Anonymous |
| `Plans` | `Index`, `Details` | — | Anonymous |
| `Orders` | `New` GET/POST | — | `Retail` only (SRS) |
| `Orders` | `Tracking` | — | Anonymous (Order ID know ho to) |
| `Customer` | `Dashboard`, `MyConnection` | — | `Customer` |
| `Bills` | `Index` | — | `Customer` / `Accounts` |
| `Bills` | `Details` | — | `Customer` (own) / `Accounts` |
| `Profile` | `Index`, `Edit` | — | `Customer` |
| `Feedback` | `Index` GET/POST | — | `Customer` |
| `Admin` | `Dashboard`, `Reports`, `Settings` | — | `Admin` |
| `Admin` | `Vendors` CRUD | — | `Admin` |
| `Admin` | `Employees` CRUD | — | `Admin` |
| `Admin` | `RetailShops` CRUD | — | `Admin` |
| `Admin` | `Stock` list/adjust | — | `Admin` |
| `Admin` | `Plans` CRUD | — | `Admin` |
| `Accounts` | `Dashboard` | — | `Accounts` |
| `Accounts` | `Bills` generate/list | — | `Accounts` |
| `Accounts` | `Payments` record | — | `Accounts` |
| `Technical` | `Dashboard` | — | `Technical` |
| `Technical` | `Feasibility` record | — | `Technical` |
| `Technical` | `Install` | — | `Technical` |
| `Technical` | `ConnectionStatus` change | — | `Technical` |
| `Retail` | `Dashboard`, `Search` | — | `Retail` |
| `Retail` | `NewOrder` | — | `Retail` |
| `Retail` | `Payments` record | — | `Retail` |

---

## Security per endpoint

| Cheez | Kaise |
|---|---|
| **Authorization** | `[Authorize(Roles = "...")]` har controller/action par. Default deny — `FallbackPolicy` set karo taake bhoolne par bhi protected ho. |
| **CSRF** | `[ValidateAntiForgeryToken]` har POST par (abhi bhi hai, rakho) |
| **Ownership** | Customer sirf apna data. Service layer mein check: `connection.CustomerId == currentUser.CustomerId`. Role check kaafi nahi. |
| **Mass assignment** | Kabhi `[Bind]` entity par nahi. Sirf ViewModel accept karo. |
| **Rate limiting** | `Account/Status` par IP-based (2 min window). Login par account-based lockout. |
| **IDOR** | Har lookup par current user ka scope lagao, sirf ID se na dhoondo |
| **Audit** | Kaun bill generate karta hai, kaun status badalta hai — `GeneratedBy`, `ChangedBy` columns bharna **zaroori** |
| **HTTPS** | `UseHttpsRedirection` + HSTS (abhi sirf production mein hai — sahi hai) |
| **Headers** | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, CSP, `Referrer-Policy` |
| **Passwords** | Identity default (PBKDF2, 100k iterations). Minimum length 8, complexity on. |
| **Secrets** | Connection string `appsettings.Development.json` mein **nahi**. User Secrets (dev) + env vars (prod). |
| **Logging** | Password, CNIC, card number **kabhi** log na karo |

---

## Paging result

```csharp
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

**Kyun:** SRS mein "till date" lists hain (saare orders, saare bills). Bila
pagination, ye queries saalon mein hazaron rows laayengi.

---

## Performance notes (api level)

| Cheez | Approach |
|---|---|
| List queries | `.AsNoTracking()` — read-only par change tracking bekaar |
| Dashboard counts | `.CountAsync()` alag, `ToList().Count` nahi |
| Charts | Aggregate SQL (`GROUP BY`) — poore rows memory mein na lao |
| N+1 | `.Include()` ya projection (`Select`) — loop mein query nahi |
| Pagination | `Skip/Take` server-side. Client-side filter **nahi** |
| Timeouts | Default 30s. Reports par explicit longer timeout |
