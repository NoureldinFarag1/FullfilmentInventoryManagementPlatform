using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, PaginatedList<OrderSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<OrderSummaryDto>> Handle(
        GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders.AsNoTracking();

        if (request.Status is not null)
            query = query.Where(o => o.Status == request.Status);

        if (request.CustomerId is not null)
            query = query.Where(o => o.CustomerId == request.CustomerId);

        if (request.WarehouseId is not null)
            query = query.Where(o => o.WarehouseId == request.WarehouseId);

        if (request.From is not null)
            query = query.Where(o => o.OrderedAt >= request.From);

        if (request.To is not null)
            query = query.Where(o => o.OrderedAt <= request.To);
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            query = query.Where(o =>
                o.Customer.Name.Contains(term) ||
                o.Customer.Email.Contains(term) ||
                o.Items.Any(i => i.ProductSku.Contains(term) || i.ProductName.Contains(term)));
        }
        
        query = ApplySort(query, request.SortBy, request.SortDescending);

        var projected = query.Select(o => new OrderSummaryDto(
            o.Id,
            o.CustomerId,
            o.ReferenceNumber,
            o.Customer.Name,
            o.WarehouseId,
            o.Warehouse.Code,
            o.Status,
            o.OrderedAt,
            o.TotalAmount,
            o.Items.Count));

        return await PaginatedList<OrderSummaryDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }

    private static IQueryable<Order> ApplySort(
        IQueryable<Order> query, string? sortBy, bool descending)
    {
        // An allow-list, not reflection: an arbitrary property name from the
        // client must not be able to reach the query.
        var sorted = sortBy?.ToLowerInvariant() switch
        {
            "total" => descending
                ? query.OrderByDescending(o => o.TotalAmount)
                : query.OrderBy(o => o.TotalAmount),

            "status" => descending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),

            "customer" => descending
                ? query.OrderByDescending(o => o.Customer.Name)
                : query.OrderBy(o => o.Customer.Name),

            _ => descending
                ? query.OrderByDescending(o => o.OrderedAt)
                : query.OrderBy(o => o.OrderedAt)
        };
        
        return sorted.ThenBy(o => o.Id);
    }
}