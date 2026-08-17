namespace Fulfillment.Application.Common.Interfaces;

public record AuthenticatedUser(Guid Id, string Email, IReadOnlyList<string> Roles);

public interface IIdentityService
{
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password);
}