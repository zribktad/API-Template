using Identity.Domain.Entities;
using SharedKernel.Domain.Entities.Contracts;
using SharedKernel.Infrastructure.Persistence.EntityNormalization;

namespace Identity.Infrastructure.Persistence.EntityNormalization;

public sealed class AppUserEntityNormalizationService : IEntityNormalizationService
{
    public void Normalize(IAuditableTenantEntity entity)
    {
        if (entity is AppUser user)
        {
            user.NormalizedUsername = AppUser.NormalizeUsername(user.Username);
            user.NormalizedEmail = AppUser.NormalizeEmail(user.Email);
        }
    }
}
