using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PaginatedList<CustomerDto>>
{
    private readonly IApplicationDbContext _context;
    
    public GetCustomersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<PaginatedList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c=>c.Name.Contains(term) || c.Email.Contains(term));
        }
        
        var projected = query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new CustomerDto(
                c.Id, c.Name, c.Email, c.Phone, c.Address, c.Orders.Count));

        return await PaginatedList<CustomerDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}