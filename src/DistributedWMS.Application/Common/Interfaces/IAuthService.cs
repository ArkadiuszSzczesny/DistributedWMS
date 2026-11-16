using DistributedWMS.Shared.Users;

namespace DistributedWMS.Application.Common.Interfaces;

/// <summary>
///  Kontrakt uwierzytelniania użytkowników systemu. 
/// </summary>
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
