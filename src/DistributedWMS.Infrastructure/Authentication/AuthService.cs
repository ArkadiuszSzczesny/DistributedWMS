using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DistributedWMS.Infrastructure.Authentication;

/// <summary>
/// Weryfikuje poświadczenia użytkownika i wydaje token dostępu JWT
/// zawierający jego rolę oraz identyfikator przydzielonej lokalizacji.
/// </summary> 
public class AuthService : IAuthService
{
    private readonly IWmsDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(IWmsDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var userName = request.UserName.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.AssignedLocation)
            .FirstOrDefaultAsync(u => u.UserName == userName && u.IsActive, ct);

        // Jednakowy komunikat dla nieistniejącego użytkownika i błędnego hasła w celu utrudnienia ataków typu brute-force
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new DomainException("Nieprawidłowa nazwa użytkownika lub hasło.");

        var token = GenerateToken(user.Id, user.UserName, user.FullName, user.Role.ToString(), user.AssignedLocationId);

        return new LoginResponse(
            token,
            user.Id,
            user.UserName,
            user.FullName,
            user.Role.ToString(),
            user.AssignedLocationId);
    }

    private string GenerateToken(Guid userId, string userName, string fullName, string role, Guid? locationId)
    {
        var jwt = _configuration.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, userId.ToString()),
            new (ClaimTypes.NameIdentifier, userId.ToString()),
            new (ClaimTypes.Name, userName),
            new (ClaimTypes.Role, role),
            new ("fullName", fullName)
        };

        if (locationId is not null)
            claims.Add(new Claim("assignedLocation", locationId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(int.Parse(jwt["ExpiryHours"] ?? "8")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool VerifyPassword(string password, string hash)
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

