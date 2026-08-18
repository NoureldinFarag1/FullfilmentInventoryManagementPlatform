using Fulfillment.Application.Warehouses.Queries.GetWarehouses;
using MediatR;

namespace Fulfillment.Application.Warehouses.Queries.GetWarehouseById;

public record GetWarehouseByIdQuery(Guid Id) : IRequest<WarehouseDto>;