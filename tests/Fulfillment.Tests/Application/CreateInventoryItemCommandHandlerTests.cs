using FluentAssertions;
using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Inventory.Commands.CreateInventoryItem;
using Fulfillment.Domain.Entities;
using Xunit;

namespace Fulfillment.Tests.Application;

/// <summary>
/// Regression cover for the missing activation guard: an inventory item could be
/// created for a deactivated product, while ordering the same product was correctly
/// refused. See <see cref="TestDatabase"/> for what SQLite does and does not prove.
/// </summary>
public class CreateInventoryItemCommandHandlerTests
{
    private static Product Product(bool isActive) => new()
    {
        Sku = "SKU-001",
        Name = "Test Widget",
        Price = 25.00m,
        IsActive = isActive
    };

    private static Warehouse Warehouse() => new()
    {
        Code = "WH-01",
        Name = "Main Warehouse",
        Address = "Cairo"
    };

    [Fact]
    public async Task DeactivatedProduct_CannotBeStocked()
    {
        using var db = new TestDatabase();
        var product = Product(isActive: false);
        var warehouse = Warehouse();
        db.Context.Products.Add(product);
        db.Context.Warehouses.Add(warehouse);
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInventoryItemCommandHandler(db.Context);
        var command = new CreateInventoryItemCommand(product.Id, warehouse.Id);

        var act = () => handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .WithMessage("This product is deactivated and cannot be stocked.");
        db.Context.InventoryItems.Should().BeEmpty();
    }

    [Fact]
    public async Task ActiveProduct_IsStockedNormally()
    {
        using var db = new TestDatabase();
        var product = Product(isActive: true);
        var warehouse = Warehouse();
        db.Context.Products.Add(product);
        db.Context.Warehouses.Add(warehouse);
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInventoryItemCommandHandler(db.Context);

        var id = await handler.Handle(
            new CreateInventoryItemCommand(product.Id, warehouse.Id), CancellationToken.None);

        id.Should().NotBeEmpty();
        db.Context.InventoryItems.Should().ContainSingle(i => i.Id == id);
    }

    [Fact]
    public async Task NonexistentProduct_IsNotFound_NotAConflict()
    {
        using var db = new TestDatabase();
        var warehouse = Warehouse();
        db.Context.Warehouses.Add(warehouse);
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInventoryItemCommandHandler(db.Context);

        var act = () => handler.Handle(
            new CreateInventoryItemCommand(Guid.NewGuid(), warehouse.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivatedProductCheck_RunsBeforeTheDuplicateCheck()
    {
        // A deactivated product that is already stocked must report the deactivation,
        // not the duplicate: the guard is the more specific reason to refuse.
        using var db = new TestDatabase();
        var product = Product(isActive: false);
        var warehouse = Warehouse();
        db.Context.Products.Add(product);
        db.Context.Warehouses.Add(warehouse);
        db.Context.InventoryItems.Add(new InventoryItem(product.Id, warehouse.Id));
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateInventoryItemCommandHandler(db.Context);

        var act = () => handler.Handle(
            new CreateInventoryItemCommand(product.Id, warehouse.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>())
            .WithMessage("This product is deactivated and cannot be stocked.");
    }
}
