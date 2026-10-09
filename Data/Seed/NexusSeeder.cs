using Microsoft.EntityFrameworkCore;
using NEXUS.Common.Abstractions;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Organisation;

namespace NEXUS.Data.Seed;

/**
 * Reference data only - cities, plans, price tiers, discount slabs and roles.
 * Deliberately no customers, bills or orders: the demo creates those through
 * the app so nothing on screen is a number someone typed by hand.
 */
public static class NexusSeeder
{
    public static async Task SeedAsync(NexusDbContext db, IPasswordService hasher,
                                       CancellationToken ct = default)
    {
        await SeedCitiesAsync(db, ct);

        // Cities must reach the database before anything looks one up by code.
        await db.SaveChangesAsync(ct);

        await SeedPlansAsync(db, ct);
        await SeedBulkDiscountTiersAsync(db, ct);
        await SeedProductsAsync(db, ct);
        await SeedAdminAsync(db, hasher, ct);
        await SeedDemoOutletAsync(db, ct);

        // The outlet has to exist before a retail clerk can be posted to it.
        await db.SaveChangesAsync(ct);

        await SeedStaffAsync(db, hasher, ct);

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCitiesAsync(NexusDbContext db, CancellationToken ct)
    {
        if (await db.Cities.AnyAsync(ct)) return;

        db.Cities.AddRange(
            new City { Name = "Karachi", Region = "Sindh", Code = 21 },
            new City { Name = "Lahore", Region = "Punjab", Code = 42 },
            new City { Name = "Islamabad", Region = "Federal", Code = 51 },
            new City { Name = "Rawalpindi", Region = "Punjab", Code = 61 },
            new City { Name = "Faisalabad", Region = "Punjab", Code = 41 },
            new City { Name = "Multan", Region = "Punjab", Code = 91 },
            new City { Name = "Peshawar", Region = "Khyber Pakhtunkhwa", Code = 92 },
            new City { Name = "Quetta", Region = "Balochistan", Code = 81 });
    }

    /**
     * Rates come straight from the SRS package tables and are in USD, which is
     * the currency decision taken for this project.
     */
    private static async Task SeedPlansAsync(NexusDbContext db, CancellationToken ct)
    {
        if (await db.Plans.AnyAsync(ct)) return;

        var dialUp10 = new Plan
        {
            Code = "DU-10", Name = "Dial-Up 10 Hours", ConnectionType = ConnectionType.DialUp,
            HoursIncluded = 10, IsUnlimited = false, SecurityDeposit = 325m, SortOrder = 10,
            Description = "Ten hours of dial-up access per month over an existing landline."
        };
        var dialUp30 = new Plan
        {
            Code = "DU-30", Name = "Dial-Up 30 Hours", ConnectionType = ConnectionType.DialUp,
            HoursIncluded = 30, IsUnlimited = false, SecurityDeposit = 325m, SortOrder = 20,
            Description = "Thirty hours of dial-up access per month over an existing landline."
        };
        var dialUp60 = new Plan
        {
            Code = "DU-60", Name = "Dial-Up 60 Hours", ConnectionType = ConnectionType.DialUp,
            HoursIncluded = 60, IsUnlimited = false, SecurityDeposit = 325m, SortOrder = 30,
            Description = "Sixty hours of dial-up access per month over an existing landline."
        };
        var dialUp28 = new Plan
        {
            Code = "DU-UNL-28", Name = "Dial-Up Unlimited 28K", ConnectionType = ConnectionType.DialUp,
            SpeedKbps = 28, IsUnlimited = true, SecurityDeposit = 325m, SortOrder = 40,
            Description = "Unlimited dial-up access at 28 Kbps."
        };
        var dialUp56 = new Plan
        {
            Code = "DU-UNL-56", Name = "Dial-Up Unlimited 56K", ConnectionType = ConnectionType.DialUp,
            SpeedKbps = 56, IsUnlimited = true, SecurityDeposit = 325m, SortOrder = 50,
            Description = "Unlimited dial-up access at 56 Kbps."
        };

        var bb30 = new Plan
        {
            Code = "BB-30", Name = "Broadband 30 Hours", ConnectionType = ConnectionType.Broadband,
            HoursIncluded = 30, IsUnlimited = false, SecurityDeposit = 500m, SortOrder = 60,
            Description = "Thirty hours of dedicated broadband per month."
        };
        var bb60 = new Plan
        {
            Code = "BB-60", Name = "Broadband 60 Hours", ConnectionType = ConnectionType.Broadband,
            HoursIncluded = 60, IsUnlimited = false, SecurityDeposit = 500m, SortOrder = 70,
            Description = "Sixty hours of dedicated broadband per month."
        };
        var bb64 = new Plan
        {
            Code = "BB-UNL-64", Name = "Broadband Unlimited 64K", ConnectionType = ConnectionType.Broadband,
            SpeedKbps = 64, IsUnlimited = true, SecurityDeposit = 500m, SortOrder = 80,
            Description = "Unlimited broadband at 64 Kbps."
        };
        var bb128 = new Plan
        {
            Code = "BB-UNL-128", Name = "Broadband Unlimited 128K", ConnectionType = ConnectionType.Broadband,
            SpeedKbps = 128, IsUnlimited = true, SecurityDeposit = 500m, SortOrder = 90,
            Description = "Unlimited broadband at 128 Kbps."
        };

        var landline = new Plan
        {
            Code = "TEL-LINE", Name = "Telephone Line", ConnectionType = ConnectionType.Telephone,
            IsUnlimited = false, SecurityDeposit = 250m, SortOrder = 100,
            Description = "Residential landline with local and STD call billing."
        };

        var all = new[] { dialUp10, dialUp30, dialUp60, dialUp28, dialUp56, bb30, bb60, bb64, bb128, landline };
        db.Plans.AddRange(all);

        var monthlyOnly = new Dictionary<Plan, decimal>
        {
            [dialUp10] = 50m, [dialUp30] = 130m, [dialUp60] = 260m,
            [dialUp28] = 75m, [dialUp56] = 100m,
            [bb30] = 175m, [bb60] = 315m,
            [bb64] = 225m, [bb128] = 350m
        };

        foreach (var (plan, monthly) in monthlyOnly)
        {
            db.PlanPrices.Add(new PlanPrice
            {
                Plan = plan, Cycle = BillingCycle.Monthly, Amount = monthly, IsAvailable = true
            });
            db.PlanPrices.Add(new PlanPrice
            {
                Plan = plan, Cycle = BillingCycle.HalfYearly,
                Amount = Math.Round(monthly * 6m, 2), IsAvailable = true
            });
            db.PlanPrices.Add(new PlanPrice
            {
                Plan = plan, Cycle = BillingCycle.Yearly,
                Amount = Math.Round(monthly * 12m, 2), IsAvailable = true
            });
        }

        db.PlanPrices.Add(new PlanPrice
        {
            Plan = landline, Cycle = BillingCycle.Monthly, Amount = 15m, IsAvailable = true
        });
        db.PlanPrices.Add(new PlanPrice
        {
            Plan = landline, Cycle = BillingCycle.HalfYearly, Amount = 90m, IsAvailable = true
        });
    }

    private static async Task SeedBulkDiscountTiersAsync(NexusDbContext db, CancellationToken ct)
    {
        if (await db.BulkDiscountTiers.AnyAsync(ct)) return;

        var order = 0;
        foreach (var tier in BulkDiscountTiers.All)
        {
            db.BulkDiscountTiers.Add(new BulkDiscountTierEntity
            {
                MinConnections = tier.MinConnections,
                MaxConnections = tier.MaxConnections,
                Rate = tier.Rate,
                SortOrder = ++order
            });
        }
    }

    private static async Task SeedProductsAsync(NexusDbContext db, CancellationToken ct)
    {
        if (await db.EquipmentProducts.AnyAsync(ct)) return;

        db.EquipmentProducts.AddRange(
            new EquipmentProduct { Sku = "MDM-56K", Name = "56K Dial-Up Modem", Description = "External analogue modem for dial-up lines." },
            new EquipmentProduct { Sku = "RTR-BB-01", Name = "Broadband Router", Description = "Single-port broadband router supplied at installation." },
            new EquipmentProduct { Sku = "RTR-BB-04", Name = "Broadband Router (4 Port)", Description = "Four-port router for multi-device premises." },
            new EquipmentProduct { Sku = "CBL-RJ11-10", Name = "Telephone Cable 10m", Description = "RJ-11 cable for landline runs." },
            new EquipmentProduct { Sku = "CBL-RJ45-15", Name = "Network Cable 15m", Description = "Cat-5 patch cable for broadband installs." },
            new EquipmentProduct { Sku = "SPL-PHONE", Name = "Line Splitter", Description = "Splits a single line between telephone and modem." });
    }

    private static async Task SeedAdminAsync(NexusDbContext db, IPasswordService hasher, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Role == NexusRoles.Admin, ct)) return;

        const string seedPassword = "Nexus@2026";

        var admin = new AppUser
        {
            AccountId = "A0000000000001",
            Email = "admin@nexus.example",
            FullName = "System Administrator",
            Role = NexusRoles.Admin,
            Phone = "111-NEXUS-1",
            PasswordHash = hasher.Hash(seedPassword),
            MustChangePassword = true,
            IsActive = true
        };

        db.Users.Add(admin);

        db.Employees.Add(new Employee
        {
            EmployeeCode = "EMP-0001",
            FullName = admin.FullName,
            Designation = "System Administrator",
            Phone = admin.Phone,
            User = admin,
            JoinedOn = DateTime.UtcNow.Date
        });
    }

    private static async Task SeedDemoOutletAsync(NexusDbContext db, CancellationToken ct)
    {
        if (await db.RetailShops.AnyAsync(ct)) return;

        var karachi = await db.Cities.FirstOrDefaultAsync(c => c.Code == 21, ct);
        if (karachi is null) return;

        db.RetailShops.Add(new RetailShop
        {
            Name = "NEXUS Retail Outlet - Clifton",
            OutletCode = "OUT-KHI-01",
            Address = "Block 5, Clifton, Karachi",
            Phone = "021-111-0001",
            CityId = karachi.Id
        });
    }

    /**
     * One sign-in per staff role.
     *
     * The system has four staff consoles but only the administrator existed,
     * so three of them could not be reached at all - the role-aware sidebar,
     * the authorisation attributes and every staff dashboard were unreachable
     * without hand-editing the database. These accounts make the whole thing
     * demonstrable. The password is fixed and documented so the demo is
     * repeatable; production would force a change on first sign-in.
     */
    private static async Task SeedStaffAsync(NexusDbContext db, IPasswordService hasher,
                                             CancellationToken ct)
    {
        const string seedPassword = "Nexus@2026";

        var outlet = await db.RetailShops.OrderBy(s => s.Id).FirstOrDefaultAsync(ct);

        var staff = new (string AccountId, string Email, string FullName, string Role,
                         string Code, string Designation, bool AtOutlet)[]
        {
            ("R0000000000001", "retail@nexus.example", "Sana Iqbal", NexusRoles.Retail,
                "EMP-R01", "Retail Outlet Clerk", true),
            ("T0000000000001", "technical@nexus.example", "Usman Ahmed", NexusRoles.Technical,
                "EMP-T01", "Senior Field Technician", false),
            ("F0000000000001", "accounts@nexus.example", "Hina Raza", NexusRoles.Accounts,
                "EMP-F01", "Accounts Officer", false)
        };

        foreach (var (accountId, email, fullName, role, code, designation, atOutlet) in staff)
        {
            if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.AccountId == accountId, ct))
                continue;

            var user = new AppUser
            {
                AccountId = accountId,
                Email = email,
                FullName = fullName,
                Role = role,
                Phone = "021-111-0001",
                PasswordHash = hasher.Hash(seedPassword),
                MustChangePassword = false,
                IsActive = true
            };

            db.Users.Add(user);

            db.Employees.Add(new Employee
            {
                EmployeeCode = code,
                FullName = fullName,
                Designation = designation,
                Phone = user.Phone,
                Email = email,
                User = user,
                ShopId = atOutlet ? outlet?.Id : null,
                JoinedOn = DateTime.UtcNow.Date
            });
        }
    }
}
