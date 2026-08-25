using MediatR;

namespace Fulfillment.Application.Orders.Commands.AddOrderItem;

public record AddOrderItemCommand(Guid OrderId, Guid ProductId, int Quantity) : IRequest;