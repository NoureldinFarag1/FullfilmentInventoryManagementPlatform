using Fulfillment.Application.Common.Models;
using MediatR;

namespace Fulfillment.Application.Categories.Queries.GetCategories;

public record GetCategoriesQuery(
    string? Search = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<CategoryDto>>;

public record CategoryDto(Guid Id, string Name, string? Description, bool IsActive, int ProductCount);