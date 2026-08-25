using System.ComponentModel.DataAnnotations;
using Fulfillment.Domain.Common;

namespace Fulfillment.Domain.Entities;

public class OrderItem : BaseEntity
{
    private OrderItem() { }
    internal OrderItem(Guid orderId, Product product, int quantity)
    {
        OrderId = orderId;
        ProductId = product.Id;
        ProductSku = product.Sku;
        ProductName = product.Name;
        UnitPrice = product.Price;
        Quantity = quantity;
    }
    
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public string ProductSku { get; private set; } = null!;
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal void IncreaseQuantity(int amount) => Quantity += amount;
}