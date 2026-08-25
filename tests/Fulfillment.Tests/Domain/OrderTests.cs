using FluentAssertions;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using Xunit;

namespace Fulfillment.Tests.Domain;

public class OrderTests
{
    private static Product ActiveProduct(decimal price = 25.00m) => new()
    {
        Sku = "SKU-001",
        Name = "Test Widget",
        Price = price,
        IsActive = true
    };

    private static Order NewOrder() => new(Guid.NewGuid(), Guid.NewGuid(), "ORD-2026-0001");

    private static Order ConfirmedOrder()
    {
        var order = NewOrder();
        order.AddItem(ActiveProduct(), 5);
        order.Confirm();
        return order;
    }

    [Fact]
    public void NewOrder_StartsAsDraft_WithZeroTotal()
    {
        var order = NewOrder();

        order.Status.Should().Be(OrderStatus.Draft);
        order.TotalAmount.Should().Be(0m);
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_SnapshotsCommercialValues()
    {
        var order = NewOrder();

        var item = order.AddItem(ActiveProduct(25.00m), 3);

        item.UnitPrice.Should().Be(25.00m);
        item.ProductSku.Should().Be("SKU-001");
        item.ProductName.Should().Be("Test Widget");
        item.LineTotal.Should().Be(75.00m);
    }

    [Fact]
    public void LaterPriceChange_DoesNotRewriteOrderHistory()
    {
        var order = NewOrder();
        var product = ActiveProduct(25.00m);

        order.AddItem(product, 2);
        product.Price = 99.00m;

        order.Items.Single().UnitPrice.Should().Be(25.00m);
        order.TotalAmount.Should().Be(50.00m);
    }

    [Fact]
    public void TotalAmount_RecalculatesAcrossMultipleItems()
    {
        var order = NewOrder();

        order.AddItem(ActiveProduct(25.00m), 3);
        order.AddItem(new Product { Sku = "SKU-002", Name = "Gadget", Price = 40.00m, IsActive = true }, 2);

        order.TotalAmount.Should().Be(155.00m);
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddingSameProductTwice_MergesIntoOneLine()
    {
        var order = NewOrder();
        var product = ActiveProduct();

        order.AddItem(product, 3);
        order.AddItem(product, 2);

        order.Items.Should().HaveCount(1);
        order.Items.Single().Quantity.Should().Be(5);
        order.TotalAmount.Should().Be(125.00m);
    }

    [Fact]
    public void AddItem_WithNonPositiveQuantity_IsRejected()
    {
        var order = NewOrder();

        var act = () => order.AddItem(ActiveProduct(), 0);

        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void AddItem_WithDeactivatedProduct_IsRejected()
    {
        var order = NewOrder();
        var product = ActiveProduct();
        product.IsActive = false;

        var act = () => order.AddItem(product, 1);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*deactivated*");
    }

    [Fact]
    public void AddItem_AfterConfirmation_IsRejected()
    {
        var order = ConfirmedOrder();

        var act = () => order.AddItem(ActiveProduct(), 1);

        act.Should().Throw<InvalidOrderStateException>();
    }

    [Fact]
    public void EmptyOrder_CannotBeConfirmed()
    {
        var order = NewOrder();

        var act = () => order.Confirm();

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*no items*");
    }

    [Fact]
    public void Confirm_MovesToConfirmed_AndStampsTime()
    {
        var order = NewOrder();
        order.AddItem(ActiveProduct(), 1);

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmingTwice_IsRejected()
    {
        var order = ConfirmedOrder();

        var act = () => order.Confirm();

        act.Should().Throw<InvalidOrderStateException>()
            .WithMessage("*Confirmed to Confirmed*");
    }

    [Fact]
    public void CompletingADraft_IsRejected()
    {
        var order = NewOrder();
        order.AddItem(ActiveProduct(), 1);

        var act = () => order.Complete();

        act.Should().Throw<InvalidOrderStateException>();
    }

    [Fact]
    public void CancellingADraft_ReportsNoStockToRestore()
    {
        var order = NewOrder();
        order.AddItem(ActiveProduct(), 1);

        var stockMustBeRestored = order.Cancel();

        stockMustBeRestored.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void CancellingAConfirmedOrder_ReportsStockMustBeRestored()
    {
        var order = ConfirmedOrder();

        var stockMustBeRestored = order.Cancel();

        stockMustBeRestored.Should().BeTrue();
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void CancellingTwice_IsRejected_SoStockCannotBeRestoredTwice()
    {
        var order = ConfirmedOrder();
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOrderStateException>();
    }

    [Fact]
    public void CompletedOrder_CannotBeCancelled()
    {
        var order = ConfirmedOrder();
        order.Complete();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOrderStateException>()
            .WithMessage("*Completed to Cancelled*");
    }
}