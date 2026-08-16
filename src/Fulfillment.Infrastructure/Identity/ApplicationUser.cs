using Microsoft.AspNetCore.Identity;

namespace Fulfillment.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Fullname { get; set; } = null!;
}