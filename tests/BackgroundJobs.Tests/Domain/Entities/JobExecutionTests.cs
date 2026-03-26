using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Enums;
using Shouldly;
using Xunit;

namespace BackgroundJobs.Tests.Domain.Entities;

public sealed class JobExecutionTests
{
    [Fact]
    public void MarkProcessing_SetsStatusAndStartTime()
    {
        JobExecution job = CreatePendingJob();

        job.MarkProcessing(TimeProvider.System);

        job.Status.ShouldBe(JobStatus.Processing);
        job.StartedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkCompleted_SetsStatusProgressAndPayload()
    {
        JobExecution job = CreatePendingJob();
        job.MarkProcessing(TimeProvider.System);

        job.MarkCompleted("{\"rows\":42}", TimeProvider.System);

        job.Status.ShouldBe(JobStatus.Completed);
        job.ProgressPercent.ShouldBe(100);
        job.ResultPayload.ShouldBe("{\"rows\":42}");
        job.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkCompleted_WithNullPayload_SetsStatusAndProgress()
    {
        JobExecution job = CreatePendingJob();

        job.MarkCompleted(null, TimeProvider.System);

        job.Status.ShouldBe(JobStatus.Completed);
        job.ProgressPercent.ShouldBe(100);
        job.ResultPayload.ShouldBeNull();
    }

    [Fact]
    public void MarkFailed_SetsStatusAndErrorMessage()
    {
        JobExecution job = CreatePendingJob();
        job.MarkProcessing(TimeProvider.System);

        job.MarkFailed("Something went wrong", TimeProvider.System);

        job.Status.ShouldBe(JobStatus.Failed);
        job.ErrorMessage.ShouldBe("Something went wrong");
        job.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateProgress_ClampsToValidRange()
    {
        JobExecution job = CreatePendingJob();

        job.UpdateProgress(50);
        job.ProgressPercent.ShouldBe(50);

        job.UpdateProgress(-10);
        job.ProgressPercent.ShouldBe(0);

        job.UpdateProgress(200);
        job.ProgressPercent.ShouldBe(100);
    }

    [Fact]
    public void NewJob_HasPendingStatusAndZeroProgress()
    {
        JobExecution job = CreatePendingJob();

        job.Status.ShouldBe(JobStatus.Pending);
        job.ProgressPercent.ShouldBe(0);
        job.StartedAtUtc.ShouldBeNull();
        job.CompletedAtUtc.ShouldBeNull();
        job.ResultPayload.ShouldBeNull();
        job.ErrorMessage.ShouldBeNull();
    }

    private static JobExecution CreatePendingJob() =>
        new()
        {
            Id = Guid.NewGuid(),
            JobType = "test-job",
            SubmittedAtUtc = DateTime.UtcNow,
        };
}
