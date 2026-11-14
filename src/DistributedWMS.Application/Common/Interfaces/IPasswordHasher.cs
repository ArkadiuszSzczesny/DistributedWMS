namespace DistributedWMS.Application.Common.Interfaces;

/// <summary>
/// Abstrakcja mechanizmu hashowania haseł, dzięki której warstwa aplikacji
/// nie zależy od konkretnej biblioteki kryptograficznej.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
