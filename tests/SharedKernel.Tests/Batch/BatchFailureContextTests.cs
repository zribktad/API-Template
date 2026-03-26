using SharedKernel.Application.Batch;
using SharedKernel.Application.DTOs;
using Shouldly;
using Xunit;

namespace SharedKernel.Tests.Batch;

public sealed class BatchFailureContextTests
{
    [Fact]
    public void Constructor_SetsItems()
    {
        IReadOnlyList<string> items = new[] { "a", "b", "c" };

        BatchFailureContext<string> context = new(items);

        context.Items.ShouldBe(items);
        context.HasFailures.ShouldBeFalse();
        context.FailedIndices.ShouldBeEmpty();
    }

    [Fact]
    public void AddFailure_WithErrorList_TracksFailure()
    {
        BatchFailureContext<string> context = new(new[] { "item" });
        Guid id = Guid.NewGuid();
        IReadOnlyList<string> errors = new[] { "Error 1", "Error 2" };

        context.AddFailure(0, id, errors);

        context.HasFailures.ShouldBeTrue();
        context.FailedIndices.ShouldContain(0);
        context.IsFailed(0).ShouldBeTrue();
    }

    [Fact]
    public void AddFailure_WithSingleError_TracksFailure()
    {
        BatchFailureContext<string> context = new(new[] { "item" });
        Guid id = Guid.NewGuid();

        context.AddFailure(0, id, "single error");

        context.HasFailures.ShouldBeTrue();
        context.IsFailed(0).ShouldBeTrue();
    }

    [Fact]
    public void AddFailures_MergesMultipleFailures()
    {
        BatchFailureContext<string> context = new(new[] { "a", "b", "c" });
        List<BatchResultItem> failures = new()
        {
            new BatchResultItem(0, Guid.NewGuid(), new[] { "err0" }),
            new BatchResultItem(2, Guid.NewGuid(), new[] { "err2" }),
        };

        context.AddFailures(failures);

        context.HasFailures.ShouldBeTrue();
        context.IsFailed(0).ShouldBeTrue();
        context.IsFailed(1).ShouldBeFalse();
        context.IsFailed(2).ShouldBeTrue();
        context.FailedIndices.Count.ShouldBe(2);
    }

    [Fact]
    public void ToFailureResponse_ReturnsResponseWithAllFailures()
    {
        BatchFailureContext<string> context = new(new[] { "a", "b" });
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        context.AddFailure(0, id1, "err1");
        context.AddFailure(1, id2, "err2");

        BatchResponse response = context.ToFailureResponse();

        response.Failures.Count.ShouldBe(2);
        response.SuccessCount.ShouldBe(0);
        response.FailureCount.ShouldBe(2);
    }

    [Fact]
    public async Task ApplyRulesAsync_ExecutesAllRules()
    {
        BatchFailureContext<string> context = new(new[] { "a", "b" });
        int ruleCallCount = 0;
        TestBatchRule<string> rule1 = new(() => ruleCallCount++);
        TestBatchRule<string> rule2 = new(() => ruleCallCount++);

        await context.ApplyRulesAsync(CancellationToken.None, rule1, rule2);

        ruleCallCount.ShouldBe(2);
    }

    [Fact]
    public void IsFailed_ReturnsFalse_ForNonFailedIndex()
    {
        BatchFailureContext<string> context = new(new[] { "a", "b" });
        context.AddFailure(0, null, "err");

        context.IsFailed(1).ShouldBeFalse();
    }

    private sealed class TestBatchRule<TItem> : IBatchRule<TItem>
    {
        private readonly Action _onApply;

        public TestBatchRule(Action onApply) => _onApply = onApply;

        public Task ApplyAsync(BatchFailureContext<TItem> context, CancellationToken ct)
        {
            _onApply();
            return Task.CompletedTask;
        }
    }
}
