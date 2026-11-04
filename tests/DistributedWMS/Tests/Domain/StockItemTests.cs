using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace DistributedWMS.Tests.Domain;

/// <summary>
/// Weryfiukje reguły ochrony stanów magazynowych przed wartościami ujemnymi,
/// stanowiące podstawowy mechanizm zapewnienia poprawności ewidencji.
/// </summary>
public class StockItemTests
{
    private static StockItem CreateStock(decimal quantity)
        => new(Guid.NewGuid(), Guid.NewGuid(), quantity);

    [Fact(DisplayName = "Przychód powieksza stan o podaną ilość")]
    public void Increase_AddsQuantity()
    {
        var stock = CreateStock(10m);

        stock.Increase(5m);

        stock.Quantity.Should().Be(15m);
    }

    [Fact(DisplayName = "Rozchód pomniejsza stan o podaną ilość")]
    public void Decrease_SubtractsQuantity()
    {
        var stock = CreateStock(10m);

        stock.Decrease(4m);

        stock.Quantity.Should().Be(6m);
    }

    [Fact(DisplayName = "Rozchód przekraczający stan dostępny zostaje odrzucony")]
    public void Decrease_RejectQuantityExceedingAvailableStock()
    { 
        var stock = CreateStock(30m);

        var act = () => stock.Decrease(50m);

        act.Should().Throw<DomainException>()
            .WithMessage("*Dostępne: 30*żądane: 50*");
    }

    [Fact(DisplayName = "Odrzucony rozchód nie wpływa na stan")]
    public void Decrease_LeavesQuantityUnchangedWhenRejected()
    {
        var stock = CreateStock(30m);
        
        try { stock.Decrease(50m); } catch (DomainException) { }

        stock.Quantity.Should().Be(30m);
    }

    [Fact(DisplayName = "Rozchód może zejść dokładnie do zera")]
    public void Decrease_AllowsExactlyZero()
    {
        var stock = CreateStock(30m);

        stock.Decrease(30m);

        stock.Quantity.Should().Be(0m);
    }

    [Theory(DisplayName = "Przychód odrzuca ilość dodatnią")]
    [InlineData(0)]
    [InlineData(-5)]
    public void Increase_RejectsNonPositiveQuantity(decimal quantity)
    {
        var stock = CreateStock(10m);

        var act = () => stock.Increase(quantity);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Inwentaryzacja ustawia stan na podaną wartość")]
    public void SetQuantity_OverwritesCurrentStock()
    {
        var stock = CreateStock(30m);

        stock.SetQuantity(12m);

        stock.Quantity.Should().Be(12m);
    }

    [Fact(DisplayName = "Inwentaryzacja odrzuca wartość ujemną")]
    public void SetQuantity_RejectsNegativeValue()
    {
        var stock = CreateStock(30m);

        var act = () => stock.SetQuantity(-1m);

        act.Should().Throw<DomainException>();
    }
}