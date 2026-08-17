namespace Fulfillment.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, IEnumerable<string> roles);
}