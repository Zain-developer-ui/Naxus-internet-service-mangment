using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Constants;
using NEXUS.Common.Extensions;
using NEXUS.Data;
using NEXUS.Models.ViewModels;

namespace NEXUS.Services.Profile;

public interface IProfileService
{
    Task<Result<ProfileViewModel>> GetAsync(int userId, CancellationToken ct = default);
}

/**
 * Builds the profile screen from the signed-in account rather than a fixture.
 * A member of staff has no CNIC, address or documents on file, so those blocks
 * are left empty and the view hides them - showing a customer's details to an
 * administrator was the bug this replaces.
 */
public sealed class ProfileService : IProfileService
{
    private readonly NexusDbContext _db;

    public ProfileService(NexusDbContext db) => _db = db;

    public async Task<Result<ProfileViewModel>> GetAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Customer)
                .ThenInclude(c => c!.City)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);

        if (user is null)
            return Result<ProfileViewModel>.NotFound("Your account could not be loaded.");

        var customer = user.Customer;
        var isCustomer = user.Role == NexusRoles.Customer;

        var vm = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            AccountId = user.AccountId,
            RoleLabel = NexusRoles.DisplayName(user.Role),
            IsStaff = !isCustomer,
            MemberSince = user.CreatedAt.ToString("MMMM yyyy"),
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };

        if (customer is not null)
        {
            vm.Cnic = customer.Cnic;
            vm.Address = customer.Address;
            vm.CityName = customer.City?.Name ?? string.Empty;
            vm.ConnectionTypeName = customer.PrimaryConnectionType.DisplayName();
            vm.LandlineNumber = customer.LandlineNumber ?? string.Empty;
            vm.IsCorporate = customer.IsCorporate;
            vm.OrganisationName = customer.OrganisationName ?? string.Empty;

            vm.ConnectionCount = await _db.Connections
                .AsNoTracking()
                .CountAsync(c => c.CustomerId == customer.Id && !c.IsDeleted, ct);
        }
        else if (user.Employee is not null)
        {
            vm.Designation = user.Employee.Designation;
            vm.EmployeeCode = user.Employee.EmployeeCode;
            vm.JoinedOn = user.Employee.JoinedOn;
        }

        return Result<ProfileViewModel>.Ok(vm);
    }
}
