using MediatR;

namespace Fulfillment.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand (
    Guid CustomerId,
    Guid WarehouseId,
    string? IdempotencyKey = null) : IRequest<Guid>;