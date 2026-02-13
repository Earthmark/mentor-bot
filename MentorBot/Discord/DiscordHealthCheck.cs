using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MentorBot.Discord;

public class DiscordHealthCheck(IDiscordContext ctx) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ctx.ConnectedAndReady
            ? HealthCheckResult.Healthy("Discord service is ready and bound.")
            : HealthCheckResult.Unhealthy("Discord bot api has disconnected."));
    }
}