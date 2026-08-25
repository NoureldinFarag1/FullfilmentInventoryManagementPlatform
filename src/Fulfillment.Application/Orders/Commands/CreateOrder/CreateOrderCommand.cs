using MediatR;

namespace Fulfillment.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand (
    Guid CustomerId,
    Guid WarehouseId,
    string? Notes = null,
    string? IdempotencyKey = null) : IRequest<Guid>;