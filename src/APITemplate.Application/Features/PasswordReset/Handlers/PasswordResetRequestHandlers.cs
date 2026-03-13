using APITemplate.Application.Common.Email;
using APITemplate.Application.Common.Events;
using APITemplate.Application.Common.Options;
using APITemplate.Application.Common.Security;
using APITemplate.Application.Features.PasswordReset.DTOs;
using APITemplate.Domain.Entities;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace APITemplate.Application.Features.PasswordReset;

public sealed record RequestPasswordResetCommand(RequestPasswordResetRequest Request) : IRequest;

public sealed record ConfirmPasswordResetCommand(ConfirmPasswordResetRequest Request) : IRequest;

public sealed class PasswordResetRequestHandlers
    : IRequestHandler<RequestPasswordResetCommand>,
        IRequestHandler<ConfirmPasswordResetCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly EmailOptions _emailOptions;

    public PasswordResetRequestHandlers(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IPublisher publisher,
        TimeProvider timeProvider,
        IOptions<EmailOptions> emailOptions
    )
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _unitOfWork = unitOfWork;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _emailOptions = emailOptions.Value;
    }

    public async Task Handle(RequestPasswordResetCommand command, CancellationToken ct)
    {
        var user = await _userRepository.FindByEmailAsync(command.Request.Email, ct);

        // Silent success if user not found — don't leak user existence
        if (user is null)
            return;

        await _tokenRepository.InvalidateAllForUserAsync(user.Id, ct);

        var rawToken = _tokenGenerator.GenerateToken();
        var tokenHash = _tokenGenerator.HashToken(rawToken);

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = _timeProvider
                .GetUtcNow()
                .UtcDateTime.AddMinutes(_emailOptions.PasswordResetTokenExpiryMinutes),
        };

        await _tokenRepository.AddAsync(resetToken, ct);
        await _unitOfWork.CommitAsync(ct);

        await _publisher.Publish(
            new PasswordResetRequestedNotification(user.Email, user.Username, rawToken),
            ct
        );
    }

    public async Task Handle(ConfirmPasswordResetCommand command, CancellationToken ct)
    {
        var tokenHash = _tokenGenerator.HashToken(command.Request.Token);
        var resetToken =
            await _tokenRepository.GetValidByTokenHashAsync(tokenHash, ct)
            ?? throw new NotFoundException(
                "Password reset token not found or expired.",
                ErrorCatalog.PasswordReset.TokenNotFound
            );

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (resetToken.ExpiresAtUtc < now)
            throw new ConflictException(
                "Password reset token has expired.",
                ErrorCatalog.PasswordReset.TokenExpired
            );

        if (resetToken.IsUsed)
            throw new ConflictException(
                "Password reset token has already been used.",
                ErrorCatalog.PasswordReset.TokenAlreadyUsed
            );

        resetToken.IsUsed = true;

        var user =
            await _userRepository.GetByIdAsync(resetToken.UserId, ct)
            ?? throw new NotFoundException(
                nameof(AppUser),
                resetToken.UserId,
                ErrorCatalog.Users.NotFound
            );

        user.PasswordHash = _passwordHasher.Hash(command.Request.NewPassword);

        await _userRepository.UpdateAsync(user, ct);
        await _tokenRepository.UpdateAsync(resetToken, ct);
        await _unitOfWork.CommitAsync(ct);
    }
}
