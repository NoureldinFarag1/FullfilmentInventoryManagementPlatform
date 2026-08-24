using MediatR;

namespace Fulfillment.Application.Products.Commands.SetProductStatus;

public record SetProductStatusCommand (Guid Id, bool IsActive): IRequest;