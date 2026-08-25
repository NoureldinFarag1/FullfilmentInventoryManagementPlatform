using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using Fulfillment.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, PaginatedList<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ProductDto>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsNoTracking<Domain.Entities.Product>();

        if (request.CategoryId is not null)
            query = query.Where(p => p.CategoryId == request.CategoryId);

        if (request.IsActive is not null)
            query = query.Where(p => p.IsActive == request.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }

        var projected = query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.IsActive,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.InventoryItems.Sum(i => (int?)i.Quantity) ?? 0));

        return await PaginatedList<ProductDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}