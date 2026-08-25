using MediatR;

namespace Fulfillment.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    Guid? CategoryId) : IRequest<Guid>;