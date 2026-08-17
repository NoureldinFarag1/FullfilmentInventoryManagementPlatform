namespace Fulfillment.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key) : base($"{name} with identifier '{key}' was not found.") { }
}