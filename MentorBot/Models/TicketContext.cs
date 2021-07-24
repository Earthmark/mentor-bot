using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public interface ITicketContext
  {
    ValueTask<Ticket?> GetTicket(string id, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> CreateTicket(Ticket item, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> UpdateTicket(Ticket item, CancellationToken cancellationToken = default);
  }

  public class TicketContext : ITicketContext
  {
    private readonly Container _container;
    private readonly ITicketNotifier _notifier;

    public TicketContext(CosmosClient dbClient, ITicketNotifier notifier, IOptionsSnapshot<CosmosOptions> options)
    {
      _container = dbClient.GetContainer(options.Value.Database, options.Value.TicketsContainer);
      _notifier = notifier;
    }

    public async ValueTask<Ticket?> GetTicket(string id, CancellationToken cancellationToken = default)
    {
      try
      {
        return await _container.ReadItemAsync<Ticket>(id.ToString(), new PartitionKey(id), cancellationToken: cancellationToken);
      }
      catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
      {
        return null;
      }
    }

    public async ValueTask<Ticket?> CreateTicket(Ticket item, CancellationToken cancellationToken = default)
    {
      var ticket = await _container.CreateItemAsync(item, new PartitionKey(item.Id), cancellationToken: cancellationToken);
      _notifier.NotifyNewTicket(ticket);
      return ticket;
    }

    public async ValueTask<Ticket?> UpdateTicket(Ticket item, CancellationToken cancellationToken = default)
    {
      var ticket = await _container.UpsertItemAsync(item, new PartitionKey(item.Id), cancellationToken: cancellationToken);
      _notifier.NotifyUpdatedTicket(ticket);
      return ticket;
    }
  }
}
