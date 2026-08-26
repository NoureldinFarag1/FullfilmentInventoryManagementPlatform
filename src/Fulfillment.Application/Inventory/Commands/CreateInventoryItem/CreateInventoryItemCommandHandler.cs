using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Inventory.Commands.CreateInventoryItem;

public class CreateInventoryItemCommandHandler
    : IRequestHandler<CreateInventoryItemCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateInventoryItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(
        CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var productIsActive = await _context.Products
            .Where(p => p.Id == request.ProductId)
            .Select(p => (bool?)p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (productIsActive is null)
            throw new NotFoundException(nameof(Product), request.ProductId);

        if (productIsActive == false)
            throw new ConflictException(
                "This product is deactivated and cannot be stocked.");

        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (!warehouseExists)
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);


        var duplicate = await _context.InventoryItems.AnyAsync(
            i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId,
            cancellationToken);

        if (duplicate)
            throw new ConflictException(
                "This product is already stocked in this warehouse.");

        var item = new InventoryItem(request.ProductId, request.WarehouseId);

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}