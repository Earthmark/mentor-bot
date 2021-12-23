using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Discord
{
  public class DiscordHealthCheck : IHealthCheck
  {
    private readonly IDiscordContext _ctx;

    public DiscordHealthCheck(IDiscordContext ctx)
    {
      _ctx = ctx;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(_ctx.ConnectedAndReady ?
        HealthCheckResult.Healthy("Discord service is ready and bound.") :
        HealthCheckResult.Unhealthy("Discord bot api has disconnected."));
    }
  }
}
