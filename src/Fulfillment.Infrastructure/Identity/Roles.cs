namespace Fulfillment.Infrastructure.Identity;

public class Roles
{
    public const string Administrator = "Administrator";
    public const string WareHouseOperator = "WareHouseOperator";
    public const string Manager = "Manager";
    public const string SalesAgent =  "SalesAgent";

    public static readonly string[] All =
        [Administrator, WareHouseOperator, Manager, SalesAgent];
}