using FluentAssertions;
using Fulfillment.Application.Reports.Queries.GetOperationsSummary;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fulfillment.Tests.Application;

/// <summary>
/// Regression cover for the two untranslatable ORDER BY clauses that made
/// /api/reports/operations-summary return 500 on every call: both ordered over a
/// constructed DTO whose members were aggregate subqueries.
///
/// This runs on SQLite rather than SQL Server. That is enough here because the
/// failure was raised by EF's provider-agnostic translation pipeline, not by any
/// SQL Server dialect rule — <see cref="UntranslatableOrderByShape_StillFails"/>
/// pins that down by running the original broken shape and asserting it throws.
/// Without that companion test this file would pass whether or not the bug existed.
/// </summary>
public class GetOperationsSummaryQueryHandlerTests
{
    private static async Task<(Guid ProductId, Guid WarehouseId)> SeedConfirmedOrderAsync(TestDatabase db)
    {
        // StockMovement.PerformedByUserId is a real FK to AspNetUsers.
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "seed@fulfillment.local",
            Email = "seed@fulfillment.local",
            Fullname = "Seed User"
        };
        db.Context.Users.Add(user);

        var category = new Category { Name = "General", Description = "Seed category" };
        var product = new Product
        {
            Sku = "SKU-001", Name = "Test Widget", Price = 25.00m, IsActive = true,
            Category = category
        };
        var warehouse = new Warehouse { Code = "WH-01", Name = "Main Warehouse", Address = "Cairo" };
        var customer = new Customer { Name = "Acme", Email = "orders@acme.example" };

        db.Context.Categories.Add(category);
        db.Context.Products.Add(product);
        db.Context.Warehouses.Add(warehouse);
        db.Context.Customers.Add(customer);

        var item = new InventoryItem(product.Id, warehouse.Id);
        item.ApplyAdjustment(100, MovementType.Receipt, "seed", user.Id);
        db.Context.InventoryItems.Add(item);

        var order = new Order(customer.Id, warehouse.Id, "ORD-2026-0001");
        order.AddItem(product, 4);
        order.Confirm();
        db.Context.Orders.Add(order);

        await db.Context.SaveChangesAsync(CancellationToken.None);

        return (product.Id, warehouse.Id);
    }

    [Fact]
    public async Task Summary_IsProduced_WithoutATranslationFailure()
    {
        using var db = new TestDatabase();
        await SeedConfirmedOrderAsync(db);

        var handler = new GetOperationsSummaryQueryHandler(db.Context);

        var summary = await handler.Handle(new GetOperationsSummaryQuery(), CancellationToken.None);

        summary.Should().NotBeNull();
        summary.TotalOrders.Should().Be(1);
        summary.TotalRevenue.Should().Be(100.00m);
    }

    [Fact]
    public async Task TopProducts_IsPopulated_AndOrderedByUnitsDescending()
    {
        using var db = new TestDatabase();
        await SeedConfirmedOrderAsync(db);

        // A second, smaller confirmed order for a different product.
        var otherProduct = new Product
        {
            Sku = "SKU-002", Name = "Test Gadget", Price = 40.00m, IsActive = true
        };
        db.Context.Products.Add(otherProduct);
        var customer = await db.Context.Customers.FirstAsync();
        var warehouse = await db.Context.Warehouses.FirstAsync();
        var smaller = new Order(customer.Id, warehouse.Id, "ORD-2026-0002");
        smaller.AddItem(otherProduct, 1);
        smaller.Confirm();
        db.Context.Orders.Add(smaller);
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOperationsSummaryQueryHandler(db.Context);

        var summary = await handler.Handle(new GetOperationsSummaryQuery(), CancellationToken.None);

        summary.TopProducts.Should().HaveCount(2);
        summary.TopProducts[0].Sku.Should().Be("SKU-001");
        summary.TopProducts[0].UnitsOrdered.Should().Be(4);
        summary.TopProducts[0].Revenue.Should().Be(100.00m);
        summary.TopProducts[1].UnitsOrdered.Should().Be(1);
    }

    [Fact]
    public async Task StockByWarehouse_IsPopulated_AndOrderedByCode()
    {
        using var db = new TestDatabase();
        await SeedConfirmedOrderAsync(db);

        var handler = new GetOperationsSummaryQueryHandler(db.Context);

        var summary = await handler.Handle(new GetOperationsSummaryQuery(), CancellationToken.None);

        summary.StockByWarehouse.Should().ContainSingle();
        summary.StockByWarehouse[0].WarehouseCode.Should().Be("WH-01");
        // 100, not 96: Order.Confirm() is a pure state transition. Stock is deducted
        // by ConfirmOrderCommandHandler, which this test does not run.
        summary.StockByWarehouse[0].TotalUnits.Should().Be(100);
        summary.StockByWarehouse[0].DistinctProducts.Should().Be(1);
    }

    [Fact]
    public async Task OrdersByStatus_CountsConfirmedOrders()
    {
        using var db = new TestDatabase();
        await SeedConfirmedOrderAsync(db);

        var handler = new GetOperationsSummaryQueryHandler(db.Context);

        var summary = await handler.Handle(new GetOperationsSummaryQuery(), CancellationToken.None);

        summary.OrdersByStatus.Should().ContainSingle();
        summary.OrdersByStatus[0].Status.Should().Be(nameof(OrderStatus.Confirmed));
        summary.OrdersByStatus[0].Count.Should().Be(1);
    }

    [Fact]
    public async Task UntranslatableOrderByShape_StillFails()
    {
        // This is the exact shape the handler used to have. It is kept here so the
        // tests above cannot quietly become vacuous: if EF ever starts translating
        // this, or if the provider stops raising it, this test fails and tells us
        // the other assertions no longer prove anything about the original bug.
        using var db = new TestDatabase();
        await SeedConfirmedOrderAsync(db);

        var broken = db.Context.Orders
            .Where(o => o.Status == OrderStatus.Confirmed)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductSku, i.ProductName })
            .Select(g => new TopProductDto(
                g.Key.ProductSku,
                g.Key.ProductName,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.UnitPrice * i.Quantity)))
            .OrderByDescending(p => p.UnitsOrdered);

        var act = () => broken.ToListAsync(CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*could not be translated*");
    }
}
