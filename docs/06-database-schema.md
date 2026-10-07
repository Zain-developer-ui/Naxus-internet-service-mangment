# 06 — Database Schema

Status: **DESIGN** (implementation pending — Phase 4)

Ye schema SRS requirements se banaya gaya hai. Kabhi bhi production code se
pehle ye file update karo, phir entity classes.

---

## Design decisions

| Faisla | Wajah |
|---|---|
| **Numeric PK (int identity)** | Order/Account IDs business identifiers hain, PK nahi. Inhe badla ja sakta hai; PK stable rehna chahiye. |
| **Employee = Identity user** | Har role ka login SRS maangta hai. `Employee` table ASP.NET Core Identity se link hoga (`AppUser`). |
| **Customer optional login** | SRS kehta hai customer apna bill/order dekh sake. Portal login `AppUser` se, `Customer` record se alag. |
| **City apni table** | SRS Account ID mein 3-digit city code maangta hai — is liye city ko first-class entity banaya. |
| **PlanPrice separate** | SRS mein ek hi plan ke multiple tiers hain (Monthly/Quarterly/Yearly + hourly). Plan + PlanPrice ki taraf behtar hai. |
| **Enum as string (convert)** | Status values DB mein human-readable rahen — debugging aur reporting asaan. |
| **Soft delete nahi** | SRS mein koi delete-abandon nahi. Connection "Permanently Inactive" hota hai, delete nahi. |

---

## Tables

### 1. `City`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `Code` | char(3) unique | SRS: Account ID ka 3-digit city code. `001`, `002` … |
| `Name` | nvarchar(80) | |
| `IsActive` | bit | Feasibility: city serve ho rahi hai ya nahi |

### 2. `AppUser` (Identity)
| Column | Type | Notes |
|---|---|---|
| `Id` | nvarchar(450) PK | Identity default |
| `UserName` / `Email` / `PhoneNumber` | — | Identity |
| `PasswordHash` | — | Identity (PBKDF2 hashing) |
| `FullName` | nvarchar(120) | |
| `EmployeeId` | int FK nullable | Employee ho to |
| `CustomerId` | int FK nullable | Customer ho to |
| `IsActive` | bit | Deactivated user login na kar sake |

> Ek `AppUser` sirf ek taraf se linked hoga — employee ya customer, dono nahi.
> CHECK constraint se enforce karo.

### 3. `Role` / `AppUserRole`
Identity ke built-in tables. Define kiye jaane wale roles:

| Role | SRS ke mutabiq |
|---|---|
| `Admin` | Sab kuch + plans/charges CRUD |
| `Accounts` | Bill generate + update |
| `Technical` | Order status, connection lifecycle, equipment |
| `Retail` | Order place, payment record |
| `Customer` | Apna data, order, bill |

> `Retail` role ko `RetailShopId` se bind karo (naya column `AppUser.RetailShopId`),
> taake ek outlet ka employee sirf apne outlet ka data dekhe.

### 4. `Employee`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `EmployeeCode` | varchar(20) unique | HR code |
| `FullName` | nvarchar(120) | |
| `Email` | nvarchar(160) | |
| `Phone` | varchar(20) | |
| `EmployeeType` | nvarchar(20) | Admin / Accounts / Technical / Retail |
| `RetailShopId` | int FK nullable | Retail employees ke liye zaroori |
| `JoinedOn` | date | |
| `IsActive` | bit | |

### 5. `RetailShop`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `ShopCode` | varchar(20) unique | |
| `Name` | nvarchar(120) | |
| `CityId` | int FK | |
| `Address` | nvarchar(250) | |
| `Phone` | varchar(20) | |
| `OpenedOn` | date | |
| `IsActive` | bit | |

### 6. `Vendor`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `Name` | nvarchar(120) | |
| `ContactPerson` | nvarchar(120) | |
| `Phone` | varchar(20) | |
| `Email` | nvarchar(160) | |
| `Address` | nvarchar(250) | |
| `IsActive` | bit | |

### 7. `EquipmentProduct`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `Name` | nvarchar(120) | e.g. "NEXUS Router R2" |
| `ProductType` | nvarchar(20) | Modem / Router / CPE |
| `VendorId` | int FK | |
| `UnitCost` | decimal(10,2) | |
| `ReplacementCharge` | decimal(10,2) | SRS: customer ne kharab kiya to |
| `IsActive` | bit | |

### 8. `StockItem`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `EquipmentProductId` | int FK | |
| `RetailShopId` | int FK nullable | Kitne kis outlet par |
| `QuantityOnHand` | int | |
| `ReorderLevel` | int | |
| `UpdatedOn` | datetime2 | |

### 9. `PurchaseOrder` / `PurchaseOrderLine`
Admin vendor se equipment mangwata hai.
| `PurchaseOrder` | |
|---|---|
| `Id` | int PK |
| `VendorId` | int FK |
| `OrderedOn` | date |
| `ReceivedOn` | date nullable |
| `Status` | nvarchar(20) | Draft / Ordered / Received / Cancelled |
| `TotalAmount` | decimal(12,2) |

| `PurchaseOrderLine` | |
|---|---|
| `Id` | int PK |
| `PurchaseOrderId` | int FK |
| `EquipmentProductId` | int FK |
| `Quantity` | int |
| `UnitCost` | decimal(10,2) |

### 10. `Plan`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `Code` | varchar(20) unique | e.g. `DU-10H`, `BB-U128` |
| `Name` | nvarchar(120) | |
| `ConnectionType` | nvarchar(10) | `D` / `B` / `T` (SRS) |
| `BillingModel` | nvarchar(20) | Hourly / Unlimited / LandlineRental |
| `SpeedKbps` | int nullable | Unlimited plans ke liye |
| `MonthlyHours` | int nullable | Hourly plans ke liye |
| `ValidityDays` | int | Hourly plans ki validity |
| `LocalCallRate` | decimal(8,4) nullable | Landline: per minute |
| `StdcallRate` | decimal(8,4) nullable | |
| `SmsRate` | decimal(8,4) nullable | |
| `SecurityDeposit` | decimal(10,2) | 325 / 500 / 250 |
| `IsActive` | bit | Admin delete ki jagah ye |

### 11. `PlanPrice`
Ek plan ke multiple billing periods.
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `PlanId` | int FK | |
| `BillingPeriod` | nvarchar(20) | Monthly / Quarterly / HalfYearly / Yearly / OneTime |
| `Amount` | decimal(10,2) | |
| `EffectiveFrom` | date | Price history rakhne ke liye |
| `EffectiveTo` | date nullable | null = current |

> **Khula sawal:** SRS mein Yearly landline ka rental missing hai. Migration se
> pehle confirm karo.

### 12. `Customer`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `AccountId` | char(16) unique nullable | Connection milne par assign |
| `FullName` | nvarchar(120) | |
| `Cnic` | varchar(15) unique | SRS ka example `35202-1234567-1` (PK NIC) |
| `Phone` | varchar(20) | |
| `Email` | nvarchar(160) nullable | |
| `Address` | nvarchar(250) | |
| `CityId` | int FK | |
| `Area` | nvarchar(120) | SRS feasibility area |
| `PostalCode` | varchar(10) | |
| `IsBulkAccount` | bit | Corporate scheme ke liye |
| `CreatedOn` | datetime2 | |

### 13. `ConnectionOrder`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `OrderNumber` | char(11) unique | SRS: `D0000000001` |
| `CustomerId` | int FK | |
| `PlanId` | int FK | |
| `RetailShopId` | int FK nullable | Kis outlet ne place kiya |
| `PlacedByUserId` | nvarchar(450) FK | Retail employee |
| `ConnectionType` | nvarchar(10) | D / B / T |
| `NeedsLandline` | bit | SRS: Dial-Up ke liye |
| `HasExistingLandline` | bit | SRS: same vendor ki landline |
| `Status` | nvarchar(30) | Placed / UnderFeasibility / Feasible / NotFeasible / Installed / Cancelled |
| `PlacedOn` | datetime2 | |
| `PreferredInstallDate` | date nullable | |
| `PreferredTimeSlot` | nvarchar(30) | |
| `EstimatedTotal` | decimal(12,2) | |
| `Notes` | nvarchar(500) | |

### 14. `FeasibilityCheck`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `OrderId` | int FK | |
| `CheckedByUserId` | nvarchar(450) FK | Technical |
| `CheckedOn` | datetime2 | |
| `DistanceMetres` | int nullable | SRS: distance parameter |
| `ServerCapacityOk` | bit | |
| `LandlineFeasible` | bit nullable | Dial-Up + landline ke liye |
| `InternetFeasible` | bit | |
| `Result` | nvarchar(20) | Feasible / NotFeasible |
| `Remarks` | nvarchar(500) | |

### 15. `Connection`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `AccountId` | char(16) unique | SRS format |
| `CustomerId` | int FK | |
| `OrderId` | int FK | |
| `PlanId` | int FK | |
| `ConnectionType` | nvarchar(10) | D / B / T |
| `CityId` | int FK | |
| `EquipmentProductId` | int FK nullable | Modem/Router |
| `SerialNumber` | varchar(40) unique | 12-digit serial portion |
| `Status` | nvarchar(25) | Active / TemporarilyInactive / PermanentlyInactive |
| `ActivatedOn` | datetime2 | |
| `DeactivatedOn` | datetime2 nullable | |
| `DeactivationReason` | nvarchar(250) nullable | |

> `AccountId` = 1 (type) + 3 (city code) + 12 (serial). Serial generation
> race-safe honi chahiye — sequence + transaction.

### 16. `ConnectionStatusHistory`
Audit trail — SRS kehta hai status change hota hai.
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `ConnectionId` | int FK | |
| `FromStatus` | nvarchar(25) | |
| `ToStatus` | nvarchar(25) | |
| `ChangedByUserId` | nvarchar(450) FK | |
| `ChangedOn` | datetime2 | |
| `Reason` | nvarchar(250) | |

### 17. `Bill`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `BillNumber` | varchar(20) unique | |
| `ConnectionId` | int FK | |
| `BillingPeriod` | date | Month start |
| `BillDate` | date | |
| `DueDate` | date | |
| `RentalAmount` | decimal(12,2) | |
| `CallCharges` | decimal(12,2) | Landline |
| `InstallationCharges` | decimal(12,2) | |
| `SecurityDeposit` | decimal(12,2) | |
| `PreviousDues` | decimal(12,2) | |
| `DiscountAmount` | decimal(12,2) | |
| `ReplacementCharges` | decimal(12,2) | |
| `SubTotal` | decimal(12,2) | computed |
| `ServiceTaxRate` | decimal(5,4) | 0.1224 from SRS |
| `TaxAmount` | decimal(12,2) | |
| `TotalAmount` | decimal(12,2) | |
| `Status` | nvarchar(20) | Due / Paid / Overdue |
| `GeneratedByUserId` | nvarchar(450) FK | Accounts only |

### 18. `BillLine`
Itemised breakdown — SRS "itemised invoice" maangta hai.
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `BillId` | int FK | |
| `Description` | nvarchar(200) | |
| `Quantity` | decimal(10,3) | e.g. call minutes |
| `UnitPrice` | decimal(10,4) | |
| `LineTotal` | decimal(12,2) | |

### 19. `Payment`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `BillId` | int FK | |
| `Amount` | decimal(12,2) | |
| `PaidOn` | datetime2 | |
| `Method` | nvarchar(25) | Cash / Card / BankTransfer / JazzCash |
| `ReceivedByUserId` | nvarchar(450) FK | Retail ya Accounts |
| `Reference` | varchar(60) nullable | Transaction id |
| `Notes` | nvarchar(250) | |

### 20. `BulkDiscountTier`
SRS ki corporate scheme.
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `MinConnections` | int | 10 |
| `MaxConnections` | int nullable | 15 (null = open-ended) |
| `DiscountPercent` | decimal(5,2) | 25 / 50 / 75 / 100 |
| `AppliesTo` | nvarchar(30) | Advance / SecurityDeposit / Both |

### 21. `Feedback`
| Column | Type | Notes |
|---|---|---|
| `Id` | int PK | |
| `CustomerId` | int FK nullable | |
| `ConnectionId` | int FK nullable | |
| `Category` | nvarchar(60) | |
| `Message` | nvarchar(1000) | |
| `OverallRating` | tinyint | 1–5 |
| `ServiceRating` | tinyint | |
| `InstallationRating` | tinyint | |
| `SupportRating` | tinyint | |
| `SubmittedOn` | datetime2 | |
| `ReviewedByUserId` | nvarchar(450) FK nullable | Admin |
| `ReviewNotes` | nvarchar(500) | |

---

## Relationships (asli)

```
City 1─n RetailShop, Customer, Connection
Vendor 1─n EquipmentProduct
EquipmentProduct 1─n StockItem, PurchaseOrderLine
RetailShop 1─n Employee, StockItem, ConnectionOrder

AppUser 1─1 Employee   (optional)
AppUser 1─1 Customer   (optional)

Customer 1─n ConnectionOrder 1─1 FeasibilityCheck
ConnectionOrder 1─1 Connection
Connection  1─n Bill 1─n BillLine
Bill        1─n Payment
Connection  1─n ConnectionStatusHistory
Plan        1─n PlanPrice
Plan        1─n ConnectionOrder, Connection
```

## Indexes (performance)

| Table | Index | Kyun |
|---|---|---|
| `Customer` | `AccountId` unique | SRS: search by Account ID |
| `Customer` | `Cnic` unique | Duplicate customer rokne ke liye |
| `Customer` | `Phone` | Advanced search |
| `ConnectionOrder` | `OrderNumber` unique | SRS: search by Order ID |
| `ConnectionOrder` | `(Status, PlacedOn)` | Dashboard filtering |
| `Connection` | `AccountId` unique | Search |
| `Connection` | `(Status, CityId)` | City-wise reporting (SRS) |
| `Bill` | `(ConnectionId, BillingPeriod)` unique | Ek mahine mein ek bill |
| `Bill` | `(Status, DueDate)` | Overdue query |
| `Payment` | `(BillId, PaidOn)` | Statement |
| `ConnectionStatusHistory` | `(ConnectionId, ChangedOn)` | Timeline |

## Concurrency

| Cheez | Approach |
|---|---|
| Order serial (`D0000000001`) | DB sequence per connection type |
| Account serial (12 digit) | DB sequence per city |
| Bill number | DB sequence |
| Concurrent bill generation | Unique constraint + retry |
| Stock deduction | `rowversion` + transaction |

---

## Migrations plan

1. `InitialIdentity` — Identity tables
2. `CoreDomain` — City, Employee, RetailShop, Vendor, EquipmentProduct, StockItem
3. `CustomersAndOrders` — Customer, ConnectionOrder, FeasibilityCheck
4. `ConnectionsAndBilling` — Connection, Bill, BillLine, Payment, history
5. `PlansAndDiscounts` — Plan, PlanPrice, BulkDiscountTier
6. `SeedReferenceData` — cities, plans (SRS rates), discount tiers, roles

> Seed data SRS se aana chahiye, **invented nahi**. Currency ka faisla
> (`03-requirements-matrix.md` dekho) pehle hona zaroori hai.
