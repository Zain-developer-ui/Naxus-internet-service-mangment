using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Orders;

namespace NEXUS.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");

        b.HasIndex(c => c.AccountId).IsUnique().HasDatabaseName("UX_Customers_AccountId");

        // The SRS forbids a second account against the same national ID.
        b.HasIndex(c => c.Cnic).IsUnique().HasDatabaseName("UX_Customers_Cnic");

        b.HasIndex(c => c.Phone).HasDatabaseName("IX_Customers_Phone");
        b.HasIndex(c => c.FullName).HasDatabaseName("IX_Customers_Name");

        b.Property(c => c.SecurityDepositPaid).HasColumnType("decimal(18,2)");
        b.Property(c => c.PrimaryConnectionType).HasConversion<string>().HasMaxLength(20);

        b.HasOne(c => c.City)
            .WithMany(ci => ci.Customers)
            .HasForeignKey(c => c.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(c => c.User)
            .WithOne(u => u.Customer)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(c => c.RegisteredAtShop)
            .WithMany()
            .HasForeignKey(c => c.ShopId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Ignore(c => c.LiveConnectionCount);
    }
}

public class ConnectionOrderConfiguration : IEntityTypeConfiguration<ConnectionOrder>
{
    public void Configure(EntityTypeBuilder<ConnectionOrder> b)
    {
        b.ToTable("ConnectionOrders");

        b.HasIndex(o => o.OrderId).IsUnique().HasDatabaseName("UX_ConnectionOrders_OrderId");
        b.HasIndex(o => o.Status).HasDatabaseName("IX_ConnectionOrders_Status");
        b.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_ConnectionOrders_CreatedAt");
        b.HasIndex(o => new { o.CustomerId, o.Status }).HasDatabaseName("IX_ConnectionOrders_Customer_Status");

        b.Property(o => o.QuotedAmount).HasColumnType("decimal(18,2)");
        b.Property(o => o.ConnectionType).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.BillingCycle).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);

        b.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(o => o.Plan)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(o => o.City)
            .WithMany()
            .HasForeignKey(o => o.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(o => o.AssignedTechnician)
            .WithMany(e => e.AssignedOrders)
            .HasForeignKey(o => o.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(o => o.CreatedByShop)
            .WithMany()
            .HasForeignKey(o => o.CreatedByShopId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Ignore(o => o.AwaitingFeasibility);
        b.Ignore(o => o.CanBeConfirmed);
        b.Ignore(o => o.StatusLabel);
        b.Ignore(o => o.StatusBadge);
    }
}

public class FeasibilityCheckConfiguration : IEntityTypeConfiguration<FeasibilityCheck>
{
    public void Configure(EntityTypeBuilder<FeasibilityCheck> b)
    {
        b.ToTable("FeasibilityChecks");

        // One survey per order.
        b.HasIndex(f => f.OrderId).IsUnique().HasDatabaseName("UX_FeasibilityChecks_Order");

        b.Property(f => f.Result).HasConversion<string>().HasMaxLength(20);

        b.HasOne(f => f.Order)
            .WithOne(o => o.Feasibility)
            .HasForeignKey<FeasibilityCheck>(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(f => f.CheckedBy)
            .WithMany(e => e.FeasibilityChecks)
            .HasForeignKey(f => f.CheckedById)
            .OnDelete(DeleteBehavior.SetNull);

        b.Ignore(f => f.IsResolved);
        b.Ignore(f => f.ResultLabel);
    }
}

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> b)
    {
        b.ToTable("Connections");

        b.HasIndex(c => c.ConnectionNumber).IsUnique()
            .HasDatabaseName("UX_Connections_Number");

        b.HasIndex(c => c.Status).HasDatabaseName("IX_Connections_Status");

        b.HasOne(c => c.Customer)
            .WithMany(cu => cu.Connections)
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(c => c.Plan)
            .WithMany()
            .HasForeignKey(c => c.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(c => c.Order)
            .WithOne(o => o.Connection)
            .HasForeignKey<Connection>(c => c.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(c => c.City)
            .WithMany()
            .HasForeignKey(c => c.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(c => c.ConnectionType).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);

        b.Ignore(c => c.StatusLabel);
        b.Ignore(c => c.IsBillable);
    }
}

public class ConnectionStatusHistoryConfiguration : IEntityTypeConfiguration<ConnectionStatusHistory>
{
    public void Configure(EntityTypeBuilder<ConnectionStatusHistory> b)
    {
        b.ToTable("ConnectionStatusHistory");
        b.HasIndex(h => h.ChangedAt).HasDatabaseName("IX_ConnectionStatusHistory_ChangedAt");

        b.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(30);
        b.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(30);

        b.HasOne(h => h.Connection)
            .WithMany(c => c.StatusHistory)
            .HasForeignKey(h => h.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
