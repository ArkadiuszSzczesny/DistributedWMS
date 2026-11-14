using DistributedWMS.Application.Common.Interfaces;

namespace DistributedWMS.Infrastructure.Authentication;

public class  BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>
    /// Weryfikuje hasło, traktując nieprawidłowy formaty skrótu
    /// jako nieudane uwierzytelnianie zamiast błędu systemowego.
    /// </summary>
    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}