using System.Diagnostics;

namespace TestCommon;

/// <summary>
/// Bounded polling for async integration tests (e.g. DB visibility after Wolverine + RabbitMQ).
/// Avoids fixed <see cref="Task.Delay"/> in tests while still enforcing a hard timeout.
/// </summary>
public static class AsyncPoll
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Repeatedly calls <paramref name="probe"/> until it returns non-null or <paramref name="timeout"/> elapses.
    /// </summary>
    public static async Task<T> UntilNotNullAsync<T>(
        Func<Task<T?>> probe,
        TimeSpan timeout,
        TimeSpan? interval = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        TimeSpan step = interval ?? DefaultInterval;
        Stopwatch sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            T? value = await probe().ConfigureAwait(false);
            if (value is not null)
                return value;

            TimeSpan remaining = timeout - sw.Elapsed;
            if (remaining <= TimeSpan.Zero)
                break;

            TimeSpan delay = step < remaining ? step : remaining;
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Expected non-null result within {timeout.TotalSeconds} seconds (type {typeof(T).Name})."
        );
    }

    /// <summary>
    /// Repeatedly calls <paramref name="condition"/> until it returns true or <paramref name="timeout"/> elapses.
    /// </summary>
    public static async Task UntilTrueAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? interval = null,
        CancellationToken cancellationToken = default
    )
    {
        TimeSpan step = interval ?? DefaultInterval;
        Stopwatch sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await condition().ConfigureAwait(false))
                return;

            TimeSpan remaining = timeout - sw.Elapsed;
            if (remaining <= TimeSpan.Zero)
                break;

            TimeSpan delay = step < remaining ? step : remaining;
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Expected condition to become true within {timeout.TotalSeconds} seconds."
        );
    }
}
