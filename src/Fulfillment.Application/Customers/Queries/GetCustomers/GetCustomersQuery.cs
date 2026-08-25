using Fulfillment.Application.Common.Models;
using MediatR;

namespace Fulfillment.Application.Customers.Queries.GetCustomers;

public record GetCustomersQuery (
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<CustomerDto>>;
    
public record CustomerDto(Guid Id, string Name, string Email, string? Phone, string? Address, int OrderCount);