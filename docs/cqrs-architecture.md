# CQRS Architecture

The project uses explicit CQRS (Command Query Responsibility Segregation) with custom abstractions — no third-party dispatcher dependency.

---

## Core Abstractions

### Commands & Queries (`Application/Common/CQRS/`)

```csharp
// Marker interfaces
public interface ICommand { }            // void commands
public interface ICommand<TResult> { }   // commands returning a result
public interface IQuery<TResult> { }     // queries

// Handler interfaces
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
```

`ICommand` (void) and `ICommand<TResult>` are separate, unrelated types. This is intentional — they serve different handler signatures (`Task` vs `Task<TResult>`) and require separate decorator implementations.

### Domain Events (`Application/Common/Events/`)

```csharp
public interface IDomainEvent { }

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent;
}
```

`EventPublisher` implementation lives in `Api/Events/` because it uses `IServiceProvider` (service locator) to resolve handlers — infrastructure glue that belongs in the outer layer.

---

## Feature Structure

Each feature follows one-handler-per-class (SRP):

```
Features/{Feature}/
├── Commands/
│   ├── Create{Feature}Command.cs       ← record + handler class in same file
│   ├── Update{Feature}Command.cs
│   └── Delete{Feature}Command.cs
├── Queries/
│   ├── Get{Feature}ByIdQuery.cs        ← record + handler class in same file
│   └── Get{Feature}sQuery.cs
├── {Feature}ValidationHelper.cs         ← shared validation methods (if needed)
├── Specifications/
├── Repositories/
├── Mappings/
├── DTOs/
└── Validation/
```

### Adding a new command

1. Create `Features/{Feature}/Commands/DoSomethingCommand.cs`:

```csharp
public sealed record DoSomethingCommand(DoSomethingRequest Request) : ICommand<SomethingResponse>;

public sealed class DoSomethingCommandHandler : ICommandHandler<DoSomethingCommand, SomethingResponse>
{
    // inject repositories, IUnitOfWork, IEventPublisher via constructor

    public async Task<SomethingResponse> HandleAsync(DoSomethingCommand command, CancellationToken ct)
    {
        // business logic
        await _publisher.PublishAsync(new SomethingChangedNotification(), ct);
        return response;
    }
}
```

2. The handler is **automatically registered** via Scrutor assembly scanning — no manual DI wiring needed.

3. Inject in controller via `[FromServices]`:

```csharp
[HttpPost]
public async Task<ActionResult<SomethingResponse>> Create(
    DoSomethingRequest request,
    [FromServices] ICommandHandler<DoSomethingCommand, SomethingResponse> handler,
    CancellationToken ct)
{
    var result = await handler.HandleAsync(new DoSomethingCommand(request), ct);
    return CreatedAtGetById(result, result.Id);
}
```

### Adding a new query

Same pattern with `IQuery<TResult>` and `IQueryHandler<TQuery, TResult>`.

### Adding a new domain event

1. Create the event record implementing `IDomainEvent`:

```csharp
public sealed record SomethingHappenedEvent(Guid EntityId) : IDomainEvent;
```

2. Create a handler implementing `IDomainEventHandler<T>`:

```csharp
public sealed class SomethingHappenedHandler : IDomainEventHandler<SomethingHappenedEvent>
{
    public async Task HandleAsync(SomethingHappenedEvent @event, CancellationToken ct)
    {
        // react to the event
    }
}
```

3. Publish from command handlers:

```csharp
await _publisher.PublishAsync(new SomethingHappenedEvent(entity.Id), ct);
```

---

## Validation

Command handlers are wrapped with `ValidationCommandHandlerDecorator` via Scrutor's decorator pattern. The decorator:

1. Runs all registered `IValidator<TCommand>` for the command type
2. Validates nested complex objects (one hop — properties and collection items)
3. Throws `ValidationException` with aggregated errors if any fail
4. Delegates to the inner handler on success

**Query handlers are NOT decorated** — queries don't mutate state; filter validation is handled by the FluentValidation action filter.

Shared validation logic lives in `Application/Common/CQRS/Decorators/CommandValidation.cs` (cached reflection, nested object traversal).

---

## DI Registration

All handler registration happens in `ServiceCollectionExtensions.AddCqrsHandlers()`:

```csharp
// Scrutor scans Application + Api assemblies for closed generic implementations
services.Scan(scan => scan
    .FromAssemblies(applicationAssembly, apiAssembly)
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
// ... same for ICommandHandler<>, IQueryHandler<,>, IDomainEventHandler<>

// Closed generic — open generic would break non-cache events at runtime
services.AddScoped<IDomainEventHandler<CacheInvalidationNotification>, CacheInvalidationHandler<CacheInvalidationNotification>>();

// Validation decorators wrap all command handlers
services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
services.Decorate(typeof(ICommandHandler<>), typeof(ValidationCommandHandlerDecorator<>));

// Event publisher
services.AddScoped<IEventPublisher, EventPublisher>();
```

---

## Controller Injection Pattern

REST controllers use `[FromServices]` parameter injection per action method:

```csharp
public sealed class ProductsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductsResponse>> GetAll(
        [FromQuery] ProductFilter filter,
        [FromServices] IQueryHandler<GetProductsQuery, ProductsResponse> handler,
        CancellationToken ct)
    {
        var products = await handler.HandleAsync(new GetProductsQuery(filter), ct);
        return Ok(products);
    }
}
```

GraphQL uses Hot Chocolate's `[Service]` attribute:

```csharp
public async Task<ProductResponse> CreateProduct(
    [Service] ICommandHandler<CreateProductCommand, ProductResponse> handler,
    CreateProductRequest input, CancellationToken ct)
    => await handler.HandleAsync(new CreateProductCommand(input), ct);
```

---

## Cache Invalidation

Cache invalidation uses domain events with a generic open handler:

```csharp
// Marker interface for cache events
public interface ICacheInvalidationEvent : IDomainEvent
{
    string CacheTag { get; }
}

// Single generic event — pass a CacheTags constant to specify the region
public sealed record CacheInvalidationNotification(string CacheTag) : ICacheInvalidationEvent;

// Single generic handler evicts the output cache
public sealed class CacheInvalidationHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : ICacheInvalidationEvent
{
    public Task HandleAsync(TEvent @event, CancellationToken ct) =>
        _outputCacheInvalidationService.EvictAsync(@event.CacheTag, ct);
}
```

Command handlers publish these events after successful writes using `CacheTags` constants:

```csharp
await _publisher.PublishAsync(new CacheInvalidationNotification(CacheTags.Products), ct);
```

---

## Layer Boundaries

| Component | Layer | Why |
|---|---|---|
| `ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler` | Application | CQRS abstractions |
| `ValidationCommandHandlerDecorator`, `CommandValidation` | Application | Cross-cutting validation concern |
| `IDomainEvent`, `IDomainEventHandler`, `IEventPublisher` | Application | Event abstractions |
| `EventPublisher` (implementation) | Api | Uses `IServiceProvider` — infrastructure glue |
| `CacheInvalidationHandler<T>` | Api | Depends on `IOutputCacheInvalidationService` |
| Command/Query handlers | Application | Business logic |
| Controllers, GraphQL resolvers | Api | Presentation — inject handlers directly |

---

## Key Decisions

- **No dispatcher** — handlers are injected directly, making dependencies explicit and compile-time checked
- **Scrutor for DI scanning** — auto-registers all handlers by convention, decorators wrap commands only
- **Separate command/query/event concerns** — CQRS interfaces in `Common/CQRS/`, events in `Common/Events/`
- **One handler per class** — follows SRP, each file contains the record + handler together
- **`[FromServices]` injection** — no constructor field needed in controllers, each action declares its dependency
