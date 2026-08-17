using Fulfillment.Application.Auth.Login;
using Fulfillment.Application.Common.Interfaces;
using MediatR;

namespace Fulfillment.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult?>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _tokenService;

    public LoginCommandHandler(IIdentityService identityService, IJwtTokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<LoginResult?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.AuthenticateAsync(request.Email, request.Password);

        if (user is null)
            return null;

        var token = _tokenService.CreateToken(user.Id, user.Email, user.Roles);

        return new LoginResult(token, user.Email, user.Roles);
    }
}