using Microsoft.AspNetCore.Identity;
using NEXUS.Common.Abstractions;

namespace NEXUS.Services.Security;

/**
 * Wraps Identity's PBKDF2 hasher so nothing else in the codebase has to know
 * which library produced the hash. Verification is constant time and handled
 * entirely by the framework.
 */
public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _inner = new();
    private static readonly object Subject = new();

    public string Hash(string password) => _inner.HashPassword(Subject, password);

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false;

        var outcome = _inner.VerifyHashedPassword(Subject, hash, password);
        return outcome is PasswordVerificationResult.Success
                      or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
