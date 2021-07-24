using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public interface IMentorContext
  {
    ValueTask<Mentor?> GetMentor(string discordId, CancellationToken cancellationToken = default);
  }

  public class MentorContext : IMentorContext
  {
    private readonly Container _container;

    public MentorContext(CosmosClient dbClient, IOptionsSnapshot<CosmosOptions> options)
    {
      _container = dbClient.GetContainer(options.Value.Database, options.Value.MentorsContainer);
    }

    public async ValueTask<Mentor?> GetMentor(string discordId, CancellationToken cancellationToken = default)
    {
      var result = await _container.ReadItemAsync<Mentor>(discordId, new PartitionKey(discordId), cancellationToken: cancellationToken);
      return result.StatusCode != System.Net.HttpStatusCode.NotFound ? result : null;
    }
  }
}
