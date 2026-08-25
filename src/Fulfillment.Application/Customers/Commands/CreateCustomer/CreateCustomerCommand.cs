using MediatR;

namespace Fulfillment.Application.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(
    string Name,
    string Email,
    string? Phone,
    string? Address) : IRequest<Guid>;