using DotNet.Testcontainers.Builders;
using Npgsql;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Integration.Tests.Fixtures;

public sealed class SharedContainers : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } =
        new PostgreSqlBuilder("postgres:18.3")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

    public RabbitMqContainer RabbitMq { get; } =
        new RabbitMqBuilder("rabbitmq:4.2.5-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(5672)
                    .UntilMessageIsLogged(
                        "Server startup complete",
                        o => o.WithTimeout(TimeSpan.FromMinutes(2))
                    )
            )
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

        // Force process-level override so every in-process test host resolves the same RabbitMQ endpoint.
        Environment.SetEnvironmentVariable("ConnectionStrings__RabbitMQ", RabbitMqConnectionString);

        // Guard against startup races: ensure AMQP handshake succeeds before tests boot service hosts.
        ConnectionFactory factory = new() { Uri = new Uri(RabbitMqConnectionString) };
        Exception? lastError = null;
        TimeSpan delay = TimeSpan.FromMilliseconds(100);
        for (int attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                await using IConnection connection = await factory.CreateConnectionAsync();
                if (connection.IsOpen)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromTicks(
                Math.Min(delay.Ticks * 3 / 2, TimeSpan.FromSeconds(2).Ticks)
            );
        }

        throw new InvalidOperationException(
            $"RabbitMQ container was not reachable at '{RabbitMqConnectionString}' after warm-up.",
            lastError
        );
    }

    public async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__RabbitMQ", null);
        await Task.WhenAll(Postgres.DisposeAsync().AsTask(), RabbitMq.DisposeAsync().AsTask());
    }
}
