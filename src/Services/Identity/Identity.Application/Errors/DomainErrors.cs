using ErrorOr;
using SharedDomainErrors = SharedKernel.Application.Errors.DomainErrors;

namespace Identity.Application.Errors;

/// <summary>
/// Factory methods producing <see cref="Error"/> instances for the Identity & Tenancy domain.
/// </summary>
public static class DomainErrors
{
    public static class Users
    {
        public static Error NotFound(Guid id) =>
            SharedDomainErrors.General.NotFound(IdentityErrorCatalog.Users.NotFound, "User", id);

        public static Error EmailAlreadyExists(string email) =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Users.EmailAlreadyExists,
                $"Email '{email}' is already in use."
            );

        public static Error UsernameAlreadyExists(string username) =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Users.UsernameAlreadyExists,
                $"Username '{username}' is already in use."
            );
    }

    public static class Tenants
    {
        public static Error NotFound(Guid id) =>
            SharedDomainErrors.General.NotFound(
                IdentityErrorCatalog.Tenants.NotFound,
                "Tenant",
                id
            );

        public static Error CodeAlreadyExists(string code) =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Tenants.CodeAlreadyExists,
                string.Format(IdentityErrorCatalog.Tenants.CodeAlreadyExistsMessage, code)
            );
    }

    public static class Invitations
    {
        public static Error NotFound(Guid id) =>
            SharedDomainErrors.General.NotFound(
                IdentityErrorCatalog.Invitations.NotFound,
                "Invitation",
                id
            );

        public static Error AlreadyPending(string email) =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Invitations.AlreadyPending,
                $"A pending invitation for '{email}' already exists."
            );

        public static Error Expired() =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Invitations.Expired,
                IdentityErrorCatalog.Invitations.ExpiredMessage
            );

        public static Error ExpiredCreateNew() =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Invitations.Expired,
                IdentityErrorCatalog.Invitations.ExpiredCreateNewMessage
            );

        public static Error AlreadyAccepted() =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Invitations.AlreadyAccepted,
                IdentityErrorCatalog.Invitations.AlreadyAcceptedMessage
            );

        public static Error NotPending() =>
            SharedDomainErrors.General.Conflict(
                IdentityErrorCatalog.Invitations.NotPending,
                IdentityErrorCatalog.Invitations.NotPendingMessage
            );

        public static Error NotFoundOrExpired() =>
            Error.NotFound(
                code: IdentityErrorCatalog.Invitations.NotFound,
                description: IdentityErrorCatalog.Invitations.NotFoundOrExpiredMessage
            );
    }
}
