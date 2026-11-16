using DistributedWMS.Shared.Users;

namespace DistributedWMS.Application.Users;

/// <summary>
/// Zarządzanie kontami pracowników oraz przypisaniem magazynow mobilmych
/// </summary>
public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}