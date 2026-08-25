namespace Fulfillment.Application.Common.Interfaces;

public interface IOrderReferenceGenerator
{
    Task <string> NextAsync(CancellationToken cancellationToken = default);
}