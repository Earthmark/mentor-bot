using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public class MentorContext
  {
    private readonly Container _container;

    public MentorContext(CosmosClient dbClient, IOptionsSnapshot<CosmosOptions> options)
    {
      _container = dbClient.GetContainer(options.Value.Database, options.Value.MentorsContainer);
    }

    public async Task<Mentor?> GetMentor(string discordId, CancellationToken cancellationToken = default)
    {
      try
      {
        return await _container.ReadItemAsync<Mentor>(discordId, new PartitionKey(discordId), cancellationToken: cancellationToken);
      }
      catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        return null;
      }
    }
  }
}
