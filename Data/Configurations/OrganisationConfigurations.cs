using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NEXUS.Domain.Organisation;

namespace NEXUS.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        b.ToTable("Cities");
        b.HasIndex(c => c.Code).IsUnique().HasDatabaseName("UX_Cities_Code");
        b.HasIndex(c => c.Name).HasDatabaseName("IX_Cities_Name");
        b.Property(c => c.Name).IsRequired().HasMaxLength(80);
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasIndex(u => u.AccountId).IsUnique().HasDatabaseName("UX_Users_AccountId");
        b.HasIndex(u => u.Email).HasDatabaseName("IX_Users_Email");
        b.HasIndex(u => u.Role).HasDatabaseName("IX_Users_Role");

        b.Property(u => u.AccountId).IsRequired().HasMaxLength(16);
        b.Property(u => u.PasswordHash).IsRequired();
        b.Property(u => u.Role).IsRequired().HasMaxLength(30);

        b.Ignore(u => u.IsLockedOut);
    }
}

public class RetailShopConfiguration : IEntityTypeConfiguration<RetailShop>
{
    public void Configure(EntityTypeBuilder<RetailShop> b)
    {
        b.ToTable("RetailShops");
        b.HasIndex(s => s.OutletCode).IsUnique().HasDatabaseName("UX_RetailShops_OutletCode");

        b.HasOne(s => s.City)
            .WithMany(c => c.Shops)
            .HasForeignKey(s => s.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.ToTable("SiteSettings");
        b.HasIndex(s => s.Key).IsUnique().HasDatabaseName("UX_SiteSettings_Key");
        b.Property(s => s.Key).IsRequired().HasMaxLength(80);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees");
        b.HasIndex(e => e.EmployeeCode).IsUnique().HasDatabaseName("UX_Employees_Code");

        b.HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.Shop)
            .WithMany(s => s.Staff)
            .HasForeignKey(e => e.ShopId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Ignore(e => e.IsTechnician);
    }
}
