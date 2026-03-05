using APITemplate.Application.Common.Mediator;
using APITemplate.Application.Features.Auth.Interfaces;
using FluentValidation;
using MediatR;

namespace APITemplate.Application.Features.Auth.Mediator;

public sealed record LoginCommand(LoginRequest Request) : IQuery<TokenResponse?>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse?>
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var isValid = await _userService.ValidateAsync(request.Request.Username, request.Request.Password, cancellationToken);
        if (!isValid)
            return null;

        return _tokenService.GenerateToken(request.Request.Username);
    }
}

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator(IValidator<LoginRequest> requestValidator)
    {
        RuleFor(x => x.Request).SetValidator(requestValidator);
    }
}
