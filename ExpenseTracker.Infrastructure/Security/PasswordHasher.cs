using System.Security.Cryptography;
using ExpenseTracker.Application.Abstractions.Security;

namespace ExpenseTracker.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const char Separator = ';';
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join(Separator, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string password, string passwordHash)
    {
        var segments = passwordHash.Split(Separator);

        if (segments.Length != 2)
            return false;

        var salt = Convert.FromBase64String(segments[0]);
        var key = Convert.FromBase64String(segments[1]);
        var candidate = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return CryptographicOperations.FixedTimeEquals(candidate, key);
    }
}
