using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public class CosmosHealthCheck : IHealthCheck
  {
    private readonly CosmosClient _client;
    private readonly CosmosOptions _options;

    public CosmosHealthCheck(CosmosClient client, IOptionsSnapshot<CosmosOptions> options)
    {
      _options = options.Value;
      _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
      var database = _client.GetDatabase(_options.Database);

      try
      {
        await Task.WhenAll(
          database.GetContainer(_options.TicketsContainer).ReadContainerAsync(cancellationToken: cancellationToken),
          database.GetContainer(_options.MentorsContainer).ReadContainerAsync(cancellationToken: cancellationToken)
          );
        return new HealthCheckResult(HealthStatus.Healthy, "Cosmos database status");
      }
      catch(Exception e)
      {
        return new HealthCheckResult(HealthStatus.Unhealthy, "Error getting status of cosmos database tables", e);
      }
    }
  }
}
