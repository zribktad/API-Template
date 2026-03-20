using APITemplate.Application.Common.CQRS;
using APITemplate.Application.Common.CQRS.Decorators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace APITemplate.Tests.Unit.Handlers;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task HandleAsync_WhenNestedRequestIsInvalid_ThrowsValidationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CreateWidgetRequest>, CreateWidgetRequestValidator>();
        using var provider = services.BuildServiceProvider();

        var inner = new StubCommandHandler<CreateWidgetCommand, string>("ok");
        var sut = new ValidationCommandHandlerDecorator<CreateWidgetCommand, string>(
            inner,
            provider,
            []
        );

        var act = () =>
            sut.HandleAsync(
                new CreateWidgetCommand(new CreateWidgetRequest(string.Empty)),
                TestContext.Current.CancellationToken
            );

        await Should.ThrowAsync<Domain.Exceptions.ValidationException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsValid_InvokesInnerHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CreateWidgetRequest>, CreateWidgetRequestValidator>();
        using var provider = services.BuildServiceProvider();

        var inner = new StubCommandHandler<CreateWidgetCommand, string>("ok");
        var sut = new ValidationCommandHandlerDecorator<CreateWidgetCommand, string>(
            inner,
            provider,
            []
        );

        var result = await sut.HandleAsync(
            new CreateWidgetCommand(new CreateWidgetRequest("widget")),
            TestContext.Current.CancellationToken
        );

        result.ShouldBe("ok");
        inner.WasCalled.ShouldBeTrue();
    }

    private sealed record CreateWidgetRequest(string Name);

    private sealed record CreateWidgetCommand(CreateWidgetRequest Request) : ICommand<string>;

    private sealed class CreateWidgetRequestValidator : AbstractValidator<CreateWidgetRequest>
    {
        public CreateWidgetRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public async Task HandleAsync_WhenCollectionItemIsInvalid_ThrowsValidationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<OrderLineRequest>, OrderLineRequestValidator>();
        using var provider = services.BuildServiceProvider();

        var inner = new StubCommandHandler<CreateOrderCommand, string>("ok");
        var sut = new ValidationCommandHandlerDecorator<CreateOrderCommand, string>(
            inner,
            provider,
            []
        );

        var act = () =>
            sut.HandleAsync(
                new CreateOrderCommand([
                    new OrderLineRequest(string.Empty),
                    new OrderLineRequest("valid"),
                ]),
                TestContext.Current.CancellationToken
            );

        await Should.ThrowAsync<Domain.Exceptions.ValidationException>(act);
    }

    [Fact]
    public async Task HandleAsync_WhenCollectionItemsAreValid_InvokesInnerHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<OrderLineRequest>, OrderLineRequestValidator>();
        using var provider = services.BuildServiceProvider();

        var inner = new StubCommandHandler<CreateOrderCommand, string>("ok");
        var sut = new ValidationCommandHandlerDecorator<CreateOrderCommand, string>(
            inner,
            provider,
            []
        );

        var result = await sut.HandleAsync(
            new CreateOrderCommand([new OrderLineRequest("one"), new OrderLineRequest("two")]),
            TestContext.Current.CancellationToken
        );

        result.ShouldBe("ok");
        inner.WasCalled.ShouldBeTrue();
    }

    private sealed record OrderLineRequest(string Name);

    private sealed record CreateOrderCommand(IReadOnlyCollection<OrderLineRequest> Lines)
        : ICommand<string>;

    private sealed class OrderLineRequestValidator : AbstractValidator<OrderLineRequest>
    {
        public OrderLineRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    /// <summary>Simple stub handler that avoids Moq proxy issues with private types.</summary>
    private sealed class StubCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly TResult _result;
        public bool WasCalled { get; private set; }

        public StubCommandHandler(TResult result) => _result = result;

        public Task<TResult> HandleAsync(TCommand command, CancellationToken ct)
        {
            WasCalled = true;
            return Task.FromResult(_result);
        }
    }
}
