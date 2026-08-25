using MediatR;

namespace Fulfillment.Application.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string? Reason = null) : IRequest;