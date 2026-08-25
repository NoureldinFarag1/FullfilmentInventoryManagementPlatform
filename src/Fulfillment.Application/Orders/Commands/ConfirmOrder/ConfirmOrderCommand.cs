using MediatR;

namespace Fulfillment.Application.Orders.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid OrderId) : IRequest;