using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.User.DTOs;
using APITemplate.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace APITemplate.Application.Features.User;

public sealed record KeycloakPasswordResetCommand(RequestPasswordResetRequest Request) : ICommand;

public sealed class KeycloakPasswordResetCommandHandler
    : ICommandHandler<KeycloakPasswordResetCommand>
{
    private readonly IUserRepository _repository;
    private readonly IKeycloakAdminService _keycloakAdmin;
    private readonly ILogger<KeycloakPasswordResetCommandHandler> _logger;

    public KeycloakPasswordResetCommandHandler(
        IUserRepository repository,
        IKeycloakAdminService keycloakAdmin,
        ILogger<KeycloakPasswordResetCommandHandler> logger
    )
    {
        _repository = repository;
        _keycloakAdmin = keycloakAdmin;
        _logger = logger;
    }

    public async Task HandleAsync(KeycloakPasswordResetCommand command, CancellationToken ct)
    {
        var user = await _repository.FindByEmailAsync(command.Request.Email, ct);

        if (user is null || user.KeycloakUserId is null)
            return;

        try
        {
            await _keycloakAdmin.SendPasswordResetEmailAsync(user.KeycloakUserId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Failed to send password reset email for user {UserId}.",
                user.Id
            );
        }
    }
}
