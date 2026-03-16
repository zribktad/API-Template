using APITemplate.Application.Common.Options;

namespace APITemplate.Application.Common.BackgroundJobs;

public interface IRecurringBackgroundJobRegistration
{
    RecurringBackgroundJobDefinition Build(BackgroundJobsOptions options);
}
