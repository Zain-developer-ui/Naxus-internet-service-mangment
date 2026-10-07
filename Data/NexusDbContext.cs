using Microsoft.EntityFrameworkCore;
using NEXUS.Domain;
using NEXUS.Domain.Billing;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Orders;
using NEXUS.Domain.Organisation;
using NEXUS.Domain.Procurement;

namespace NEXUS.Data;

public class NexusDbContext : DbContext
{
    public NexusDbContext(DbContextOptions<NexusDbContext> options) : base(options) { }

    public DbSet<City> Cities => Set<City>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RetailShop> RetailShops => Set<RetailShop>();

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();
    public DbSet<BulkDiscountTierEntity> BulkDiscountTiers => Set<BulkDiscountTierEntity>();
    public DbSet<EquipmentProduct> EquipmentProducts => Set<EquipmentProduct>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ConnectionOrder> ConnectionOrders => Set<ConnectionOrder>();
    public DbSet<FeasibilityCheck> FeasibilityChecks => Set<FeasibilityCheck>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<ConnectionStatusHistory> ConnectionStatusHistory => Set<ConnectionStatusHistory>();

    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillLine> BillLines => Set<BillLine>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Feedback> Feedback => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        ApplySoftDeleteFilters(b);
        ApplyConcurrencyTokens(b);
        ApplyAuditDefaults(b);

        b.ApplyConfigurationsFromAssembly(typeof(NexusDbContext).Assembly);
    }

    /**
     * No entity carries a global query filter. IsDeleted on AppUser, Customer
     * and Vendor is an audit flag only.
     *
     * A model-level filter cannot express what "closed" means per screen. A
     * deactivated customer still has to appear in the Accounts ledger, in
     * outstanding-dues reports and in the audit trail, so filtering at the model
     * level would silently drop those rows everywhere at once. A deactivated
     * login still has to resolve for lockout and audit queries. Services apply
     * the appropriate predicate per query instead.
     */
    private static void ApplySoftDeleteFilters(ModelBuilder b)
    {
        // Intentionally empty - see the comment above before adding a filter here.
    }

    private static void ApplyConcurrencyTokens(ModelBuilder b)
    {
        b.Entity<Bill>().Property(e => e.RowVersion).IsRowVersion();
        b.Entity<Connection>().Property(e => e.RowVersion).IsRowVersion();
        b.Entity<StockItem>().Property(e => e.RowVersion).IsRowVersion();
    }

    /** Defaults live here so a forgotten assignment in code cannot store zero. */
    private static void ApplyAuditDefaults(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes())
        {
            if (typeof(IAuditable).IsAssignableFrom(entity.ClrType))
            {
                b.Entity(entity.ClrType)
                    .Property(nameof(IAuditable.CreatedAt))
                    .HasDefaultValueSql("SYSUTCDATETIME()");
            }
        }
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Modified) continue;

            var isDeleted = entry.Property(nameof(ISoftDeletable.IsDeleted));
            if (isDeleted.IsModified && (bool)(isDeleted.CurrentValue ?? false))
            {
                entry.Entity.DeletedAt = now;
            }
        }
    }
}
