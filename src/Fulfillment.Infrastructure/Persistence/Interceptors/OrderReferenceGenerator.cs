using Fulfillment.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Infrastructure.Persistence.Interceptors;

public class OrderReferenceGenerator : IOrderReferenceGenerator
{
    private readonly IApplicationDbContext _context;

    public OrderReferenceGenerator(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.Now.Year;
        var prefix = $"ORD-{year}";

        var lastReference = await _context.Orders
            .AsNoTracking()
            .Where(o => o.ReferenceNumber.StartsWith(prefix))
            .OrderByDescending(o => o.ReferenceNumber)
            .Select(o => o.ReferenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;

        if (lastReference is not null && int.TryParse(lastReference[prefix.Length..], out var lastNumber))
            next = lastNumber + 1;
        
        return $"{prefix}{next:D4}";
    }
}