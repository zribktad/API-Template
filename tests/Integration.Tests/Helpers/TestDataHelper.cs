using SharedKernel.Domain.Entities;

namespace Integration.Tests.Helpers;

internal static class TestDataHelper
{
    internal static AuditInfo CreateAudit(Guid actorId)
    {
        DateTime now = DateTime.UtcNow;
        return new AuditInfo
        {
            CreatedAtUtc = now,
            CreatedBy = actorId,
            UpdatedAtUtc = now,
            UpdatedBy = actorId,
        };
    }
}
