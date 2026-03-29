using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Integration.Tests.Fixtures;

public sealed class SharedContainers : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

    public RabbitMqContainer RabbitMq { get; } =
        new RabbitMqBuilder("rabbitmq:4-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();

    public string PostgresServerConnectionString
    {
        get
        {
            NpgsqlConnectionStringBuilder builder = new(Postgres.GetConnectionString())
            {
                Database = "postgres",
            };
            return builder.ConnectionString;
        }
    }

    public string RabbitMqConnectionString => RabbitMq.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(Postgres.StartAsync(), RabbitMq.StartAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(Postgres.DisposeAsync().AsTask(), RabbitMq.DisposeAsync().AsTask());
    }
}
