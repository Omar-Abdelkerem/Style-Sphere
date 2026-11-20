using System.Security.Cryptography;
using System.Text;

namespace StyleSphere.Services;

public static class PasswordHasher
{
    public const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string Prefix = "PBKDF2$SHA512";

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] subkey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            KeySize);

        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(subkey)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        // Check if storedHash is in PBKDF2$SHA512$iterations$salt$subkey format
        if (storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 5 || !int.TryParse(parts[2], out int iterations) || iterations < 10_000)
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(parts[3]);
                byte[] expectedSubkey = Convert.FromBase64String(parts[4]);

                byte[] actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA512,
                    expectedSubkey.Length);

                return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Fallback for legacy plaintext migration only (compared in constant time)
        byte[] inputBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        byte[] storedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(storedHash));
        return CryptographicOperations.FixedTimeEquals(inputBytes, storedBytes);
    }

    public static bool IsHashed(string? storedPassword)
    {
        return !string.IsNullOrEmpty(storedPassword) &&
               storedPassword.StartsWith(Prefix + "$", StringComparison.Ordinal);
    }

    public static (string RawToken, string TokenHash) GenerateResetToken()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        string rawToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        string tokenHash = HashResetToken(rawToken);
        return (rawToken, tokenHash);
    }

    public static string HashResetToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return string.Empty;
        }

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
