using MediatR;

namespace Fulfillment.Application.Inventory.Commands.CreateInventoryItem;

public record CreateInventoryItemCommand(Guid ProductId, Guid WarehouseId)
    : IRequest<Guid>;