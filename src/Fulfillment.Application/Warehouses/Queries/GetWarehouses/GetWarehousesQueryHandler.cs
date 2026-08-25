using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Warehouses.Queries.GetWarehouses;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery,PaginatedList<WarehouseDto>>
{
    private readonly IApplicationDbContext _context;
    
    public GetWarehousesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<PaginatedList<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Warehouses.AsNoTracking();
        
        if(request.IsActive is not null)
            query = query.Where(w=>w.IsActive == request.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term =  request.Search.Trim();
            
            query = query.Where(w =>w.Code.Contains(term) || w.Name.Contains(term));
        }

        var projected = query
            .OrderBy(w => w.Code)
            .ThenBy(w => w.Id)
            .Select(w => new WarehouseDto(w.Id, w.Code, w.Name, w.Address,w.IsActive));

        return await PaginatedList<WarehouseDto>.CreateAsync(
            projected, request.pageNumber, request.pageSize, cancellationToken);
    }
}