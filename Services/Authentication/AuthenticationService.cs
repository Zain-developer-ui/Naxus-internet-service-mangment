using Microsoft.EntityFrameworkCore;
using NEXUS.Common;
using NEXUS.Common.Abstractions;
using NEXUS.Common.Constants;
using NEXUS.Data;
using NEXUS.Domain.Organisation;

namespace NEXUS.Services.Authentication;

public sealed record AuthenticatedUser(
    AppUser User,
    string Role,
    int? EmployeeId,
    int? CustomerId,
    int? ShopId);

public interface IUserAuthenticator
{
    Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(string accountIdOrEmail,
                                                             string password,
                                                             CancellationToken ct = default);
    Task RecordSuccessfulLoginAsync(int userId, CancellationToken ct = default);
    Task RecordFailedLoginAsync(int userId, CancellationToken ct = default);
}

public sealed class AuthenticationService : IUserAuthenticator
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly NexusDbContext _db;
    private readonly IPasswordService _passwords;

    public AuthenticationService(NexusDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    /**
     * Every failure path returns the same message. Distinguishing "no such
     * account" from "wrong password" would let someone enumerate valid account
     * IDs without ever guessing a password.
     */
    public async Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(
        string accountIdOrEmail, string password, CancellationToken ct = default)
    {
        const string genericFailure = "Account ID or password is incorrect.";

        if (string.IsNullOrWhiteSpace(accountIdOrEmail) || string.IsNullOrWhiteSpace(password))
            return Result<AuthenticatedUser>.Fail(ErrorKind.Unauthorized, genericFailure);

        var identifier = accountIdOrEmail.Trim();

        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Employee)
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u =>
                u.AccountId == identifier ||
                (u.Email != null && u.Email.ToLower() == identifier.ToLower()), ct);

        if (user is null)
            return Result<AuthenticatedUser>.Fail(ErrorKind.Unauthorized, genericFailure);

        if (!user.IsActive || user.IsDeleted)
            return Result<AuthenticatedUser>.Fail(ErrorKind.Unauthorized,
                "This account is no longer active. Contact the administrator.");

        if (user.IsLockedOut)
        {
            var remaining = Math.Max(1, (int)Math.Ceiling(
                (user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes));

            return Result<AuthenticatedUser>.Fail(ErrorKind.Unauthorized,
                $"Too many failed attempts. Try again in {remaining} minute(s).");
        }

        if (!_passwords.Verify(password, user.PasswordHash))
        {
            await RecordFailedLoginAsync(user.Id, ct);
            return Result<AuthenticatedUser>.Fail(ErrorKind.Unauthorized, genericFailure);
        }

        var authenticated = new AuthenticatedUser(
            user,
            user.Role,
            user.Employee?.Id,
            user.Customer?.Id,
            user.Employee?.ShopId);

        return Result<AuthenticatedUser>.Ok(authenticated);
    }

    public async Task RecordSuccessfulLoginAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordFailedLoginAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;

        user.AccessFailedCount++;

        if (user.AccessFailedCount >= MaxFailedAttempts)
        {
            user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            user.AccessFailedCount = 0;
        }

        await _db.SaveChangesAsync(ct);
    }
}
