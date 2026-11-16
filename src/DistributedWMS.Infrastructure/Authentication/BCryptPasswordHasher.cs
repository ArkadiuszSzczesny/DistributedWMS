using DistributedWMS.Application.Common.Interfaces;

namespace DistributedWMS.Infrastructure.Authentication;

/// <summary>
/// Implementacja haszowania haseł oparta na algorytmie BCrypt.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>
    /// Weryfikuje hasło, traktując nieprawidłowy format skrótu
    /// jako nieudane uwierzytelnienie zamiast błędu systemowego.
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