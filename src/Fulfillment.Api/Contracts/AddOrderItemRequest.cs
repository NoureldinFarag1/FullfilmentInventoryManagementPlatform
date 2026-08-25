namespace Fulfillment.Api.Contracts;

public record AddOrderItemRequest(Guid ProductId, int Quantity);