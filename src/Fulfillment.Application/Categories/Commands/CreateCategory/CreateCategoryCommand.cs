using MediatR;

namespace Fulfillment.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, string? Description) : IRequest<Guid>;