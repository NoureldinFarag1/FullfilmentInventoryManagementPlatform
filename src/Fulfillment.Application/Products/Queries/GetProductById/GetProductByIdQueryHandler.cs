using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Products.Queries.GetProducts;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(
        GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.IsActive,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.InventoryItems.Sum(i => (int?)i.Quantity) ?? 0))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException(nameof(Product), request.Id);
    }
}