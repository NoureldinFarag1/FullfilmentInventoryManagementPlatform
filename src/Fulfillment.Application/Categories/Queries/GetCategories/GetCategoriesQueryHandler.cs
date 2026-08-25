using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Categories.Queries.GetCategories;

public class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, PaginatedList<CategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<CategoryDto>> Handle(
        GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Categories.AsNoTracking();

        if (request.IsActive is not null)
            query = query.Where(c => c.IsActive == request.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => c.Name.Contains(term));
        }

        var projected = query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new CategoryDto(
                c.Id, c.Name, c.Description, c.IsActive, c.Products.Count));

        return await PaginatedList<CategoryDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}