using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NEXUS.Domain.Catalog;

namespace NEXUS.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("Plans");
        b.HasIndex(p => p.Code).IsUnique().HasDatabaseName("UX_Plans_Code");
        b.HasIndex(p => new { p.ConnectionType, p.IsActive }).HasDatabaseName("IX_Plans_Type_Active");

        b.Property(p => p.SecurityDeposit).HasColumnType("decimal(18,2)");
        b.Ignore(p => p.DisplayName);
    }
}

public class PlanPriceConfiguration : IEntityTypeConfiguration<PlanPrice>
{
    public void Configure(EntityTypeBuilder<PlanPrice> b)
    {
        b.ToTable("PlanPrices");

        // One rate per plan per cycle - a second row for the same pair is a bug.
        b.HasIndex(p => new { p.PlanId, p.Cycle }).IsUnique()
            .HasDatabaseName("UX_PlanPrices_Plan_Cycle");

        b.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        b.Property(p => p.Cycle).HasConversion<string>().HasMaxLength(20);

        b.HasOne(p => p.Plan)
            .WithMany(pl => pl.Prices)
            .HasForeignKey(p => p.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Ignore(p => p.EffectiveMonthlyRate);
    }
}

public class BulkDiscountTierConfiguration : IEntityTypeConfiguration<BulkDiscountTierEntity>
{
    public void Configure(EntityTypeBuilder<BulkDiscountTierEntity> b)
    {
        b.ToTable("BulkDiscountTiers");

        // Rate is a fraction (0.25), not a percentage, so the maths stays exact.
        b.Property(t => t.Rate).HasColumnType("decimal(5,4)");
        b.HasIndex(t => t.MinConnections).HasDatabaseName("IX_BulkDiscountTiers_Min");

        b.Ignore(t => t.Label);
        b.Ignore(t => t.PercentRate);
    }
}

public class EquipmentProductConfiguration : IEntityTypeConfiguration<EquipmentProduct>
{
    public void Configure(EntityTypeBuilder<EquipmentProduct> b)
    {
        b.ToTable("EquipmentProducts");
        b.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("UX_EquipmentProducts_Sku");

        // Nullable on purpose - the SRS never prices equipment.
        b.Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
    }
}

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> b)
    {
        b.ToTable("StockItems");

        // One row per product per shop; quantity lives on that row.
        b.HasIndex(s => new { s.ProductId, s.ShopId }).IsUnique()
            .HasDatabaseName("UX_StockItems_Product_Shop");

        b.HasOne(s => s.Product)
            .WithMany(p => p.Stock)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(s => s.Shop)
            .WithMany(sh => sh.Stock)
            .HasForeignKey(s => s.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Ignore(s => s.IsBelowThreshold);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("StockMovements");
        b.HasIndex(m => m.OccurredAt).HasDatabaseName("IX_StockMovements_OccurredAt");

        b.HasOne(m => m.StockItem)
            .WithMany()
            .HasForeignKey(m => m.StockItemId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(m => m.PurchaseOrder)
            .WithMany()
            .HasForeignKey(m => m.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
