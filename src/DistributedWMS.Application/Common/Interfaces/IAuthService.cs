using DistributedWMS.Shared.Users;

namespace DistributedWMS.Application.Common.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
