using ErrorOr;

namespace Identity.Application.Errors;

/// <summary>
/// Factory methods producing <see cref="Error"/> instances for the Identity & Tenancy domain.
/// </summary>
public static class DomainErrors
{
    public static class Users
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: IdentityErrorCatalog.Users.NotFound,
                description: $"User with id '{id}' not found."
            );

        public static Error EmailAlreadyExists(string email) =>
            Error.Conflict(
                code: IdentityErrorCatalog.Users.EmailAlreadyExists,
                description: $"Email '{email}' is already in use."
            );

        public static Error UsernameAlreadyExists(string username) =>
            Error.Conflict(
                code: IdentityErrorCatalog.Users.UsernameAlreadyExists,
                description: $"Username '{username}' is already in use."
            );
    }

    public static class Tenants
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: IdentityErrorCatalog.Tenants.NotFound,
                description: $"Tenant with id '{id}' not found."
            );

        public static Error CodeAlreadyExists(string code) =>
            Error.Conflict(
                code: IdentityErrorCatalog.Tenants.CodeAlreadyExists,
                description: string.Format(
                    IdentityErrorCatalog.Tenants.CodeAlreadyExistsMessage,
                    code
                )
            );
    }

    public static class Invitations
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: IdentityErrorCatalog.Invitations.NotFound,
                description: $"Invitation with id '{id}' not found."
            );

        public static Error AlreadyPending(string email) =>
            Error.Conflict(
                code: IdentityErrorCatalog.Invitations.AlreadyPending,
                description: $"A pending invitation for '{email}' already exists."
            );

        public static Error Expired() =>
            Error.Conflict(
                code: IdentityErrorCatalog.Invitations.Expired,
                description: IdentityErrorCatalog.Invitations.ExpiredMessage
            );

        public static Error ExpiredCreateNew() =>
            Error.Conflict(
                code: IdentityErrorCatalog.Invitations.Expired,
                description: IdentityErrorCatalog.Invitations.ExpiredCreateNewMessage
            );

        public static Error AlreadyAccepted() =>
            Error.Conflict(
                code: IdentityErrorCatalog.Invitations.AlreadyAccepted,
                description: IdentityErrorCatalog.Invitations.AlreadyAcceptedMessage
            );

        public static Error NotPending() =>
            Error.Conflict(
                code: IdentityErrorCatalog.Invitations.NotPending,
                description: IdentityErrorCatalog.Invitations.NotPendingMessage
            );

        public static Error NotFoundOrExpired() =>
            Error.NotFound(
                code: IdentityErrorCatalog.Invitations.NotFound,
                description: IdentityErrorCatalog.Invitations.NotFoundOrExpiredMessage
            );
    }
}
