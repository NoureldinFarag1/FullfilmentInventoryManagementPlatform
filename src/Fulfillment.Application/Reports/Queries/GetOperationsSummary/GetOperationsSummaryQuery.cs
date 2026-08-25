using MediatR;

namespace Fulfillment.Application.Reports.Queries.GetOperationsSummary;

public record GetOperationsSummaryQuery(DateTime? From = null, DateTime? To = null) : IRequest<OperationsSummaryDto>;

public record OperationsSummaryDto(
    DateTime? From,
    DateTime? To,
    int TotalOrders,
    decimal TotalRevenue,
    IReadOnlyList<OrdersByStatusDto> OrdersByStatus,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<WarehouseStockDto> StockByWarehouse,
    int LowStockProductCount);

public record OrdersByStatusDto(string Status, int Count, decimal Value);
public record TopProductDto(string Sku, string Name, int UnitsOrdered, decimal Revenue);
public record WarehouseStockDto(string WarehouseCode, string WarehouseName, int TotalUnits, int DistinctProducts);