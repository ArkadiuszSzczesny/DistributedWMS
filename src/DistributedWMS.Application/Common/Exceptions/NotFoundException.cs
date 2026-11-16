namespace DistributedWMS.Application.Common.Exceptions;

/// <summary>
/// Sygnalizuje brak obiektu o wskazanym identyfikatorze.
/// Warstwa WebAPI mapuje ten wyjątek na odpowiedź HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Nie odnaleziono obiektu {entityName} o identyfikatorze {key}.") { }
}