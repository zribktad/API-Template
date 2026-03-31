using ErrorOr;
using Identity.Application.Errors;
using Identity.Application.Features.TenantInvitation.Commands;
using Identity.Application.Security;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces;
using Moq;
using SharedKernel.Domain.Interfaces;
using Shouldly;
using Xunit;
using TenantInvitationEntity = Identity.Domain.Entities.TenantInvitation;

namespace Identity.Tests.Features.TenantInvitation.Commands;

public sealed class AcceptTenantInvitationCommandHandlerTests
{
    private readonly Mock<ITenantInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ISecureTokenGenerator> _tokenGeneratorMock = new();

    public AcceptTenantInvitationCommandHandlerTests()
    {
        _tokenGeneratorMock
            .Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns<string>(token => $"hashed_{token}");
    }

    [Fact]
    public async Task HandleAsync_WhenInvitationNotFound_ReturnsNotFoundError()
    {
        AcceptTenantInvitationCommand command = new("some-token");
        _invitationRepoMock
            .Setup(r =>
                r.GetValidByTokenHashAsync("hashed_some-token", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((TenantInvitationEntity?)null);

        ErrorOr<Success> result = await AcceptTenantInvitationCommandHandler.HandleAsync(
            command,
            _invitationRepoMock.Object,
            _unitOfWorkMock.Object,
            _tokenGeneratorMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(IdentityErrorCatalog.Invitations.NotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenInvitationExpired_ReturnsExpiredError()
    {
        AcceptTenantInvitationCommand command = new("expired-token");
        TenantInvitationEntity invitation = CreateInvitation(
            expiresAtUtc: DateTime.UtcNow.AddHours(-1),
            status: InvitationStatus.Pending
        );
        _invitationRepoMock
            .Setup(r =>
                r.GetValidByTokenHashAsync("hashed_expired-token", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(invitation);

        ErrorOr<Success> result = await AcceptTenantInvitationCommandHandler.HandleAsync(
            command,
            _invitationRepoMock.Object,
            _unitOfWorkMock.Object,
            _tokenGeneratorMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(IdentityErrorCatalog.Invitations.Expired);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyAccepted_ReturnsAlreadyAcceptedError()
    {
        AcceptTenantInvitationCommand command = new("accepted-token");
        TenantInvitationEntity invitation = CreateInvitation(
            expiresAtUtc: DateTime.UtcNow.AddHours(24),
            status: InvitationStatus.Accepted
        );
        _invitationRepoMock
            .Setup(r =>
                r.GetValidByTokenHashAsync("hashed_accepted-token", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(invitation);

        ErrorOr<Success> result = await AcceptTenantInvitationCommandHandler.HandleAsync(
            command,
            _invitationRepoMock.Object,
            _unitOfWorkMock.Object,
            _tokenGeneratorMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(IdentityErrorCatalog.Invitations.AlreadyAccepted);
    }

    [Fact]
    public async Task HandleAsync_WhenValid_AcceptsInvitationAndCommits()
    {
        AcceptTenantInvitationCommand command = new("valid-token");
        TenantInvitationEntity invitation = CreateInvitation(
            expiresAtUtc: DateTime.UtcNow.AddHours(24),
            status: InvitationStatus.Pending
        );
        _invitationRepoMock
            .Setup(r =>
                r.GetValidByTokenHashAsync("hashed_valid-token", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(invitation);

        ErrorOr<Success> result = await AcceptTenantInvitationCommandHandler.HandleAsync(
            command,
            _invitationRepoMock.Object,
            _unitOfWorkMock.Object,
            _tokenGeneratorMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        invitation.Status.ShouldBe(InvitationStatus.Accepted);
        _invitationRepoMock.Verify(
            r => r.UpdateAsync(invitation, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static TenantInvitationEntity CreateInvitation(
        DateTime expiresAtUtc,
        InvitationStatus status
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            TokenHash = "hashed_token",
            ExpiresAtUtc = expiresAtUtc,
            Status = status,
            TenantId = Guid.NewGuid(),
        };
}
