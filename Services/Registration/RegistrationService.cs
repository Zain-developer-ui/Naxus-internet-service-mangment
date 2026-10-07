using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Abstractions;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Catalog;
using NEXUS.Domain.Customers;
using NEXUS.Domain.Organisation;
using NEXUS.Models.ViewModels;
using NEXUS.Services.Generation;

namespace NEXUS.Services.Registration;

public sealed record RegisteredCustomer(int CustomerId, string AccountId, string FullName, string OrderId);

public interface IRegistrationService
{
    Task<Result<RegistrationOptions>> GetOptionsAsync(CancellationToken ct = default);
    Task<Result<RegisteredCustomer>> RegisterAsync(OrderViewModel form, int? shopId,
                                                   CancellationToken ct = default);
}

/**
 * Self-service customer sign-up.
 *
 * Two rules from the SRS drive the shape of this:
 *   V1 - a Dial-Up subscriber must already hold a landline with this vendor,
 *        so the landline is captured and stored with the customer record.
 *   V2 - every customer gets an Account ID that encodes the connection type
 *        and the city, which is why the ID cannot be generated until the
 *        connection type and city are both known.
 */
public sealed class RegistrationService : IRegistrationService
{
    private readonly NexusDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IAccountIdGenerator _ids;

    public RegistrationService(NexusDbContext db, IPasswordService passwords,
                               IAccountIdGenerator ids)
    {
        _db = db;
        _passwords = passwords;
        _ids = ids;
    }

    public async Task<Result<RegistrationOptions>> GetOptionsAsync(CancellationToken ct = default)
    {
        var cities = await _db.Cities
            .AsNoTracking()
            .Where(c => c.IsServiced)
            .OrderBy(c => c.Name)
            .Select(c => new CityOption(c.Id, c.Name, c.Region))
            .ToListAsync(ct);

        if (cities.Count == 0)
            return Result<RegistrationOptions>.Fail(ErrorKind.Failure,
                "No serviceable cities are configured. Contact the administrator.");

        var plans = await _db.Plans
            .AsNoTracking()
            .Include(p => p.Prices)
            .Where(p => p.IsActive)
            .OrderBy(p => p.ConnectionType)
            .ThenBy(p => p.SortOrder)
            .ToListAsync(ct);

        var options = new RegistrationOptions(
            cities,
            plans.Select(ToOption).ToList());

        return Result<RegistrationOptions>.Ok(options);
    }

    public async Task<Result<RegisteredCustomer>> RegisterAsync(OrderViewModel form, int? shopId,
                                                                CancellationToken ct = default)
    {
        var errors = new Dictionary<string, List<string>>();

        var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == form.CityId, ct);
        if (city is null)
            Add(errors, nameof(form.CityId), "Select a city from the list.");
        else if (!city.IsServiced)
            Add(errors, nameof(form.CityId), $"{city.Name} is not serviced yet.");

        var plan = await _db.Plans
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == form.PlanId, ct);

        if (plan is null)
            Add(errors, nameof(form.PlanId), "Select a plan from the list.");
        else if (!plan.IsActive)
            Add(errors, nameof(form.PlanId), "That plan is no longer offered.");
        else if (plan.ConnectionType != form.ConnectionType)
            Add(errors, nameof(form.PlanId),
                $"{plan.Name} is a {plan.ConnectionType.DisplayName()} plan.");

        var cnic = Required(form.Cnic);
        var phone = Required(form.Phone);

        if (await _db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Cnic == cnic, ct))
            Add(errors, nameof(form.Cnic),
                "A customer with this CNIC is already registered. Use the status lookup on the sign in page.");

        if (await _db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Phone == phone, ct))
            Add(errors, nameof(form.Cnic), "This mobile number is already registered.");

        var email = Optional(form.Email)?.ToLowerInvariant();
        if (email is not null &&
            await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct))
            Add(errors, nameof(form.Email), "This email address is already registered.");

        // Rule V1: Dial-Up rides over a landline the vendor already owns.
        var landline = Optional(form.LandlineNumber);
        if (form.ConnectionType.RequiresLandline())
        {
            if (landline is null)
            {
                Add(errors, nameof(form.LandlineNumber),
                    $"{form.ConnectionType.DisplayName()} requires an existing landline number.");
            }
            else if (!await _db.Customers.IgnoreQueryFilters()
                         .AnyAsync(c => c.LandlineNumber == landline, ct))
            {
                Add(errors, nameof(form.LandlineNumber),
                    "No landline with this number is registered. Walk-in registration at an outlet can override this.");
            }
        }

        if (form.IsCorporate && string.IsNullOrWhiteSpace(form.OrganisationName))
            Add(errors, nameof(form.OrganisationName), "Enter the organisation name.");

        if (errors.Count > 0)
            return Result<RegisteredCustomer>.Invalid(ToDictionary(errors));

        // Everything above here is a read, so nothing has been tracked that a
        // caller could accidentally persist by returning early.
        var user = new AppUser
        {
            AccountId = string.Empty,
            Email = email,
            PasswordHash = _passwords.Hash(form.Password),
            FullName = form.FullName.Trim(),
            Phone = phone,
            Role = NexusRoles.Customer,
            IsActive = true,
            MustChangePassword = false
        };

        var customer = new Customer
        {
            FullName = user.FullName,
            Cnic = cnic,
            Phone = phone,
            Email = email,
            Address = form.Address.Trim(),
            CityId = city!.Id,
            PrimaryConnectionType = form.ConnectionType,
            LandlineNumber = landline,
            IsCorporate = form.IsCorporate,
            OrganisationName = form.IsCorporate ? form.OrganisationName!.Trim() : null,
            ShopId = shopId
        };

        var order = new Domain.Orders.ConnectionOrder
        {
            Customer = customer,
            PlanId = plan!.Id,
            ConnectionType = form.ConnectionType,
            BillingCycle = form.BillingCycle,
            Status = OrderStatus.AwaitingFeasibility,
            InstallationAddress = customer.Address,
            CityId = city.Id,
            LandlineNumber = landline,
            Notes = Optional(form.Notes),
            ScheduledFor = form.PreferredInstallDate,
            PreferredSlot = Optional(form.PreferredTime),
            QuotedAmount = plan.SecurityDeposit,
            CreatedByShopId = shopId
        };

        /**
         * The retry strategy and a hand-rolled transaction cannot both be in
         * play: EF refuses to replay a transaction it did not open. Everything
         * inside ExecuteAsync is therefore the retriable unit, and the
         * transaction lives in there so a transient failure rolls back the
         * whole sign-up rather than leaving a customer without a user row.
         */
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Account and order IDs are derived from rows created in this same
            // transaction, so they are allocated after the writes are staged.
            user.AccountId = await _ids.NextAccountIdAsync(form.ConnectionType, city.Code, ct);
            customer.AccountId = user.AccountId;
            user.Customer = customer;

            order.OrderId = await _ids.NextOrderIdAsync(form.ConnectionType, ct);

            _db.Users.Add(user);
            _db.ConnectionOrders.Add(order);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return Result<RegisteredCustomer>.Ok(
            new RegisteredCustomer(customer.Id, customer.AccountId,
                                   customer.FullName, order.OrderId));
    }

    private static PlanOption ToOption(Plan plan)
    {
        // Prefer the monthly row; a plan may only be sold yearly in some cases,
        // in which case the effective monthly rate is derived from that.
        var monthly = plan.Prices.FirstOrDefault(p => p.Cycle == BillingCycle.Monthly && p.IsAvailable);
        var rate = monthly?.Amount
                   ?? plan.Prices.Where(p => p.IsAvailable)
                                 .Select(p => p.EffectiveMonthlyRate)
                                 .DefaultIfEmpty(0m)
                                 .Min();

        /**
         * The badge is the one-line summary of what the plan gives you. A
         * telephone line is neither unlimited nor sold in hours, so it falls
         * back to the connection type rather than rendering " hours" with a
         * null count in front of it.
         */
        var badge = plan.IsUnlimited
            ? "Unlimited"
            : plan.HoursIncluded is int hours
                ? $"{hours} hours"
                : plan.ConnectionType.DisplayName();

        // The picker filters plans by matching this against the connection type
        // radio's value, so it has to be the enum member name - the display
        // name ("Dial-Up") would never match the radio's "DialUp".
        return new PlanOption(
            plan.Id,
            plan.Name,
            plan.ConnectionType.ToString(),
            plan.IsUnlimited,
            plan.HoursIncluded,
            plan.SpeedKbps,
            plan.SecurityDeposit,
            Math.Round(rate, 2),
            badge);
    }

    /**
     * Optional text fields arrive as "" when the browser posts an empty input,
     * which is not the same thing as absent. Collapsing both to null keeps the
     * "did they supply this?" checks honest and stops blank strings reaching
     * unique indexes.
     */
    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Required(string? value) => (value ?? string.Empty).Trim();

    private static void Add(Dictionary<string, List<string>> bag, string field, string message)
    {
        if (!bag.TryGetValue(field, out var list))
            bag[field] = list = new List<string>();

        list.Add(message);
    }

    private static IReadOnlyDictionary<string, string[]> ToDictionary(
        Dictionary<string, List<string>> bag) =>
        bag.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
}

public sealed record RegistrationOptions(
    IReadOnlyList<CityOption> Cities,
    IReadOnlyList<PlanOption> Plans);
