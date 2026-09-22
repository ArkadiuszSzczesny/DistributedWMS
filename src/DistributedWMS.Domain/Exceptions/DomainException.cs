namespace DistributedWMS.Domain.Exceptions;

///<summary>
///Wyjątek sygnalizujący naruszenie reguły biznesowej domeny.
///Warstwa WebAPI mapuje go na odpowiedź HTTP 400 lub 409.
///</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}