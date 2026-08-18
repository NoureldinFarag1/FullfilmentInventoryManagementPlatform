using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Warehouses.Queries.GetWarehouses;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Warehouses.Queries.GetWarehouseById;

public class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    private readonly IApplicationDbContext _context;

    public GetWarehouseByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WarehouseDto> Handle(
        GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == request.Id)
            .Select(w => new WarehouseDto(w.Id, w.Code, w.Name, w.Address, w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return warehouse ?? throw new NotFoundException(nameof(Warehouse), request.Id);
    }
}