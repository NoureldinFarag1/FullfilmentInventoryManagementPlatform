namespace Fulfillment.Domain.Enums;

public enum MovementType
{
    Receipt = 1,
    Issue = 2,
    Damage = 3,
    Loss = 4,
    CountCorrection = 5,
    Other = 6,
    OrderAllocation = 7,
    OrderCancellation = 8,
}