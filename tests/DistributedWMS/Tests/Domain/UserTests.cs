using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DistributedWMS.Tests.Domain;

/// <summary>
/// Weryfikuje regułe przypisania magazynu mobilnego wyłącznie serwisantom
/// </summary>
public class UserTests
{
    private static Location MobileWarehouse()
        => new("AUTO-01", "Serwis mobilny nr 1", LocationType.MobileWarehouse);

    private static Location MainWarehouse()
        => new("MG-01", "Magazyn główny", LocationType.MainWarehouse);

    [Fact(DisplayName = "Serwisantowi można przypisać magazyn mobilny")]
    public void AssigLocation_AllowsMobileWarehouseForTechnician()
    {
        var user = new User("serwisant1", "Marek Wiśniewski", "hash", UserRole.Technician);
        var van = MobileWarehouse();

        user.AssignLocation(van);

        user.AssignedLocationId.Should().Be(van.Id);
    }

    [Fact(DisplayName = "Kierownikowi nie można przypisać magazynu mobilnego")]
    public void AssignLocation_RejectsManager()
    {
        var user = new User("kierownik", "Anna Kowalska", "hash", UserRole.Manager);
       

        var act = () => user.AssignLocation(MobileWarehouse());

        act.Should().Throw<DomainException>()
            .WithMessage("*wyłącznie serwisantowi*");
    }

    [Fact(DisplayName = "Serwisantowi nie można przypisać magazynu głównego")]
    public void AssignLocation_RejectsMainWarehouse()
    {
        var user = new User("serwisant1", "Marek Wiśniewski", "hash", UserRole.Technician);

        var act = () => user.AssignLocation(MainWarehouse());

        act.Should().Throw<DomainException>()
            .WithMessage("*wyłącznie magazyn mobilny*");
    }
}