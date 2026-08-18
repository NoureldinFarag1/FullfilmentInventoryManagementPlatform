using Fulfillment.Application.Products.Queries.GetProducts;
using MediatR;

namespace Fulfillment.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;