using DistributedWMS.Domain.Enums;

namespace DistributedWMS.Application.Common.Interfaces;

/// <summary>
/// Udostępnia warstwie aplikacji tożsamość użytkownika wykonującego żądanie.
/// Implementacja odczytuje dane z tokenu dostępowego.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    Guid? AssignedLocationId { get; }
    bool IsAuthenticated { get; }
 }
