using Fulfillment.Application.Common.Models;
using Fulfillment.Domain.Enums;
using MediatR;

namespace Fulfillment.Application.Orders.Queries.GetOrders;

public record GetOrdersQuery (
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    Guid? WarehouseId = null,
    DateTime? From = null,
    DateTime? To = null,
    string? SortBy = null,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<OrderSummaryDto>>;

public record OrderSummaryDto(Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid WarehouseId,
    string WarehouseCode,
    OrderStatus Status,
    DateTime OrderedAt,
    decimal TotalAmount,
    int ItemCount);