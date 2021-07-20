using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public static class CosmosDbCreator
  {
    public static async Task EnsureCreated(CosmosClient client, IOptionsSnapshot<CosmosOptions> options, CancellationToken cancellationToken = default)
    {
      var opt = options.Value;
      Database db = await client.CreateDatabaseIfNotExistsAsync(opt.Database, cancellationToken: cancellationToken);
      await db.CreateContainerIfNotExistsAsync(opt.TicketsContainer, "/id", cancellationToken: cancellationToken);
      await db.CreateContainerIfNotExistsAsync(opt.MentorsContainer, "/id", cancellationToken: cancellationToken);
    }
  }
}
