using MediatR;

namespace Fulfillment.Application.Auth.Login;

public record LoginCommand(string Email, string Password) :  IRequest<LoginResult?>;
public record LoginResult(string Token, string Email, IReadOnlyList<string> Roles);