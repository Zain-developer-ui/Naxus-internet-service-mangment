namespace NEXUS.Common.Abstractions;

/**
 * Application-facing password contract. Deliberately not named IPasswordHasher
 * so it cannot collide with Identity's generic IPasswordHasher<TUser>.
 */
public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
