using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DistributedWMS.Client.Common;

/// <summary>
/// Odtwarza tożsamość użytkownika na podstawie tokenu przechowywanego
/// w pamięci lokalnej przeglądarki i udostępnia ją komponentom aplikacji.
/// </summary>
public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "wms_token";

    private readonly LocalStorage _storage;
    private readonly HttpClient _http;

    public JwtAuthStateProvider(LocalStorage storage, HttpClient http)
    {
        _storage = storage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetAsync(TokenKey);

        if (string.IsNullOrWhiteSpace(token) || IsExpired(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
            return Anonymous();
        }

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var identity = new ClaimsIdentity(ParseClaims(token), "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SignInAsync(string token)
    {
        await _storage.SetAsync(TokenKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await _storage.RemoveAsync(TokenKey);
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    private static AuthenticationState Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static IEnumerable<Claim> ParseClaims(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;

    private static bool IsExpired(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
