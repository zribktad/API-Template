using APITemplate.Application.Common.Mediator;
using APITemplate.Domain.Exceptions;
using APITemplate.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Mediator;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task ValidationBehavior_WhenInvalid_ThrowsDomainValidationException()
    {
        var validator = new InlineValidator<TestQuery>();
        validator.RuleFor(x => x.Value).GreaterThan(0);
        var sut = new ValidationBehavior<TestQuery, int>([validator]);

        var act = () => sut.Handle(new TestQuery(0), () => Task.FromResult(1), CancellationToken.None);

        await Should.ThrowAsync<APITemplate.Domain.Exceptions.ValidationException>(act);
    }

    [Fact]
    public async Task LoggingBehavior_CallsNextAndReturnsValue()
    {
        var sut = new LoggingBehavior<TestQuery, int>(NullLogger<LoggingBehavior<TestQuery, int>>.Instance);

        var result = await sut.Handle(new TestQuery(10), () => Task.FromResult(42), CancellationToken.None);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task TransactionBehavior_ForCommand_UsesUnitOfWorkTransaction()
    {
        var uowMock = new Mock<IUnitOfWork>();
        uowMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task> action, CancellationToken _) => action());

        var sut = new TransactionBehavior<TestCommand, int>(uowMock.Object);

        var result = await sut.Handle(new TestCommand(5), () => Task.FromResult(99), CancellationToken.None);

        result.ShouldBe(99);
        uowMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransactionBehavior_ForQuery_DoesNotUseTransaction()
    {
        var uowMock = new Mock<IUnitOfWork>();
        var sut = new TransactionBehavior<TestQuery, int>(uowMock.Object);

        var result = await sut.Handle(new TestQuery(5), () => Task.FromResult(7), CancellationToken.None);

        result.ShouldBe(7);
        uowMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed record TestCommand(int Value) : ICommand<int>;

    private sealed record TestQuery(int Value) : IQuery<int>;
}
