using Npgsql;
using Wolverine;
using Wolverine.ErrorHandling;

namespace Palantir.Platform.Messaging;

public static class DeadLetterExtensions
{
    /// <summary>
    /// Configures transient retry cooldowns and dead-letter error-queue routing for Wolverine handlers.
    /// </summary>
    public static void AddPalantirDeadLetterHandling(this WolverineOptions opts)
    {
        opts.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2))
            .Then.MoveToErrorQueue();

        opts.Policies.OnException<NpgsqlException>()
            .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15))
            .Then.MoveToErrorQueue();

        opts.Policies.OnAnyException()
            .MoveToErrorQueue();
    }
}
