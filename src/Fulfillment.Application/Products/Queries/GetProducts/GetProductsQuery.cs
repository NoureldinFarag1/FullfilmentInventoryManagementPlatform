using Fulfillment.Application.Common.Models;
using MediatR;

namespace Fulfillment.Application.Products.Queries.GetProducts;

public record GetProductsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<ProductDto>>;

public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    bool IsActive,
    Guid? CategoryId,
    string? CategoryName,
    int TotalQuantity);