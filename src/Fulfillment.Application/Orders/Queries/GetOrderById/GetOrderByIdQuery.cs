using Fulfillment.Domain.Enums;
using MediatR;

namespace Fulfillment.Application.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto>;

public record OrderDetailDto(
    Guid Id,
    string ReferenceNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Guid WarehouseId,
    string WarehouseCode,
    OrderStatus Status,
    DateTime OrderedAt,
    DateTime? ConfirmedAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);