using Fulfillment.Domain.Enums;

namespace Fulfillment.Api.Contracts;

public record AdjustStockRequest(int Delta, MovementType Type, string? Reason);