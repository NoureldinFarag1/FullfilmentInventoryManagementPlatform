using Fulfillment.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Reads the highest reference for the year and adds one. Two creations racing here
/// can compute the same number; the unique index on Order.ReferenceNumber then rejects
/// the loser's insert and the request fails with a 409 rather than issuing a duplicate.
/// That is safe but not graceful — a database sequence would remove the race entirely,
/// and is the improvement to make when there is time to migrate the column.
/// </summary>
public class OrderReferenceGenerator : IOrderReferenceGenerator
{
    private readonly IApplicationDbContext _context;

    public OrderReferenceGenerator(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
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