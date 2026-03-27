using BackgroundJobs.Application.Common;
using BackgroundJobs.Application.Features.Jobs.Commands;
using BackgroundJobs.Application.Features.Jobs.DTOs;
using BackgroundJobs.Domain.Entities;
using BackgroundJobs.Domain.Enums;
using BackgroundJobs.Domain.Interfaces;
using ErrorOr;
using Moq;
using SharedKernel.Application.Context;
using SharedKernel.Domain.Interfaces;
using Shouldly;
using Xunit;

namespace BackgroundJobs.Tests.Features.Jobs.Commands;

public sealed class SubmitJobCommandHandlerTests
{
    private readonly Mock<IJobExecutionRepository> _repositoryMock = new();
    private readonly Mock<IJobQueue> _jobQueueMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ITenantProvider> _tenantProviderMock = new();

    public SubmitJobCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(u =>
                u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<CancellationToken>(),
                    null
                )
            )
            .Returns<Func<Task>, CancellationToken, object?>(
                (action, _, _) =>
                {
                    action();
                    return Task.CompletedTask;
                }
            );

        _tenantProviderMock.SetupGet(x => x.TenantId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task HandleAsync_CreatesJobAndEnqueues()
    {
        SubmitJobRequest request = new("data-export", "{\"format\":\"csv\"}", null);
        SubmitJobCommand command = new(request);

        ErrorOr<JobStatusResponse> result = await SubmitJobCommandHandler.HandleAsync(
            command,
            _repositoryMock.Object,
            _jobQueueMock.Object,
            _unitOfWorkMock.Object,
            _tenantProviderMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        result.IsError.ShouldBeFalse();
        result.Value.JobType.ShouldBe("data-export");
        result.Value.Status.ShouldBe(JobStatus.Pending);
        result.Value.Parameters.ShouldBe("{\"format\":\"csv\"}");
    }

    [Fact]
    public async Task HandleAsync_AddsEntityToRepository()
    {
        SubmitJobRequest request = new("report-gen", null, null);
        SubmitJobCommand command = new(request);

        await SubmitJobCommandHandler.HandleAsync(
            command,
            _repositoryMock.Object,
            _jobQueueMock.Object,
            _unitOfWorkMock.Object,
            _tenantProviderMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        _repositoryMock.Verify(
            r =>
                r.AddAsync(
                    It.Is<JobExecution>(j => j.JobType == "report-gen"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_EnqueuesJobIdAfterPersistence()
    {
        SubmitJobRequest request = new("cleanup", null, "https://callback.example.com/hook");
        SubmitJobCommand command = new(request);

        ErrorOr<JobStatusResponse> result = await SubmitJobCommandHandler.HandleAsync(
            command,
            _repositoryMock.Object,
            _jobQueueMock.Object,
            _unitOfWorkMock.Object,
            _tenantProviderMock.Object,
            TimeProvider.System,
            CancellationToken.None
        );

        _jobQueueMock.Verify(
            q => q.EnqueueAsync(result.Value.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
