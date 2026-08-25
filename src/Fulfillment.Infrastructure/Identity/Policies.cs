namespace Fulfillment.Infrastructure.Identity;

public class Policies
{
    public const string CanManageCatalog = nameof(CanManageCatalog);
    public const string CanAdjustStock = nameof(CanAdjustStock);
    public const string CanManageOrders = nameof(CanManageOrders);
    public const string CanProcessOrders = nameof(CanProcessOrders);
}