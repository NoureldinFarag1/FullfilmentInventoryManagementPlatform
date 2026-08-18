using MediatR;

namespace Fulfillment.Application.Warehouses.Commands.CreateWarehouse;

public record CreateWarehouseCommand(string Code,
    string Name, string? Address) : IRequest<Guid>;