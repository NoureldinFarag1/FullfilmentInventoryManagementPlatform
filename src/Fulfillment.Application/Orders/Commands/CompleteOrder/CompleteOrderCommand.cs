using MediatR;

namespace Fulfillment.Application.Orders.Commands.CompleteOrder;

public record CompleteOrderCommand(Guid OrderId) : IRequest; 