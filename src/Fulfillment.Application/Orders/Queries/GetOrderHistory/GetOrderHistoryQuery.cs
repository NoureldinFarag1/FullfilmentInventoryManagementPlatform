using MediatR;

namespace Fulfillment.Application.Orders.Queries.GetOrderHistory;

public record GetOrderHistoryQuery (Guid OrderId) : IRequest<OrderHistoryDto>;

public record OrderHistoryDto(
    Guid OrderId,
    string ReferenceNumber,
    IReadOnlyList<OrderHistoryEntryDto> Timeline);
    
public record  OrderHistoryEntryDto(
    DateTime OccurredAt,
    string Event,
    string Detail,
    Guid? UserId
);