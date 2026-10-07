using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NEXUS.Domain.Billing;
using NEXUS.Domain.Procurement;

namespace NEXUS.Data.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> b)
    {
        b.ToTable("Bills");

        b.HasIndex(x => x.BillNumber).IsUnique().HasDatabaseName("UX_Bills_Number");
        b.HasIndex(x => new { x.CustomerId, x.PeriodStart })
            .HasDatabaseName("IX_Bills_Customer_Period");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Bills_Status");
        b.HasIndex(x => x.DueOn).HasDatabaseName("IX_Bills_DueOn");

        // Money is always exact decimal. Float rounding would corrupt totals.
        foreach (var money in new[]
        {
            nameof(Bill.SubTotal), nameof(Bill.DiscountAmount), nameof(Bill.TaxableAmount),
            nameof(Bill.ServiceTaxAmount), nameof(Bill.TotalAmount), nameof(Bill.AmountPaid)
        })
        {
            b.Property(money).HasColumnType("decimal(18,2)");
        }

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.Customer)
            .WithMany(c => c.Bills)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Connection)
            .WithMany(c => c.Bills)
            .HasForeignKey(x => x.ConnectionId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Ignore(x => x.OutstandingAmount);
        b.Ignore(x => x.IsFullyPaid);
        b.Ignore(x => x.StatusLabel);
        b.Ignore(x => x.StatusBadge);
    }
}

public class BillLineConfiguration : IEntityTypeConfiguration<BillLine>
{
    public void Configure(EntityTypeBuilder<BillLine> b)
    {
        b.ToTable("BillLines");
        b.HasIndex(l => l.BillId).HasDatabaseName("IX_BillLines_BillId");

        b.Property(l => l.Quantity).HasColumnType("decimal(18,4)");
        b.Property(l => l.UnitAmount).HasColumnType("decimal(18,2)");
        b.Property(l => l.LineAmount).HasColumnType("decimal(18,2)");

        b.HasOne(l => l.Bill)
            .WithMany(x => x.Lines)
            .HasForeignKey(l => l.BillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");

        b.HasIndex(p => p.ReceiptNumber).IsUnique().HasDatabaseName("UX_Payments_Receipt");
        b.HasIndex(p => p.PaidOn).HasDatabaseName("IX_Payments_PaidOn");

        b.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        b.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);

        b.HasOne(p => p.Bill)
            .WithMany(x => x.Payments)
            .HasForeignKey(p => p.BillId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(p => p.ReceivedByShop)
            .WithMany(s => s.PaymentsReceived)
            .HasForeignKey(p => p.ReceivedByShopId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(p => p.ReceivedBy)
            .WithMany()
            .HasForeignKey(p => p.ReceivedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Ignore(p => p.MethodLabel);
    }
}

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> b)
    {
        b.ToTable("Vendors");
        b.HasIndex(v => v.Name).HasDatabaseName("IX_Vendors_Name");
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("PurchaseOrders");
        b.HasIndex(p => p.PoNumber).IsUnique().HasDatabaseName("UX_PurchaseOrders_Number");

        b.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");

        b.HasOne(p => p.Vendor)
            .WithMany(v => v.PurchaseOrders)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.ToTable("PurchaseOrderLines");

        b.Property(l => l.UnitCost).HasColumnType("decimal(18,2)");
        b.Property(l => l.LineAmount).HasColumnType("decimal(18,2)");

        b.HasOne(l => l.PurchaseOrder)
            .WithMany(p => p.Lines)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(l => l.Product)
            .WithMany(p => p.PurchaseLines)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> b)
    {
        b.ToTable("Feedback");
        b.HasIndex(f => f.CreatedAt).HasDatabaseName("IX_Feedback_CreatedAt");

        b.HasOne(f => f.Customer)
            .WithMany(c => c.FeedbackEntries)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(f => f.Order)
            .WithMany()
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
