namespace Fulfillment.Api.Contracts;

public record CreateOrderRequest(Guid CustomerId,Guid WarehouseId, string? Notes);