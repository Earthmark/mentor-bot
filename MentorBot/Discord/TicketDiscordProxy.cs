using MentorBot.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Discord
{
  public class TicketDiscordProxyHost : IHostedService
  {
    private readonly ITicketNotifier _notifier;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketDiscordProxyHost> _logger;
    private readonly CancellationTokenSource _cancelSource = new();

    private IDisposable? _watchToken;

    public TicketDiscordProxyHost(IServiceProvider serviceProvider, ITicketNotifier notifier, ILogger<TicketDiscordProxyHost> logger)
    {
      _serviceProvider = serviceProvider;
      _notifier = notifier;
      _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      Interlocked.Exchange(ref _watchToken, _notifier.WatchTicketsUpdated(TicketUpdated))?.Dispose();
      return Task.CompletedTask;
    }

    public async void TicketUpdated(Ticket ticket)
    {
      try
      {
        await UpdateTicketInternal(ticket, _cancelSource.Token);

      }
      catch (Exception e)
      {
        _logger.LogWarning(e, "Error while updating an internal ticket.");
      }
      // This warning is not always true.
#pragma warning disable CS1058 // A previous catch clause already catches all exceptions
      catch
#pragma warning restore CS1058 // A previous catch clause already catches all exceptions
      {
        _logger.LogWarning("Abstract error while updating internal ticket, this is bad.");
      }
    }

    private async ValueTask UpdateTicketInternal(Ticket ticket, CancellationToken cancellationToken)
    {
      using var scope = _serviceProvider.CreateScope();
      await scope.ServiceProvider.GetRequiredService<ITicketDiscordProxy>().RectifyTicket(ticket, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      Interlocked.Exchange(ref _watchToken, null)?.Dispose();
      _cancelSource.Cancel();
      return Task.CompletedTask;
    }
  }

  public interface ITicketDiscordProxy
  {
    ValueTask RectifyTicket(Ticket ticket, CancellationToken cancellationToken = default);
  }
  public class TicketDiscordProxy : ITicketDiscordProxy
  {
    private readonly IDiscordContext _discCtx;
    private readonly ITicketContext _tickCtx;
    public TicketDiscordProxy(IDiscordContext discCtx, ITicketContext tickCtx)
    {
      _discCtx = discCtx;
      _tickCtx = tickCtx;
    }

    public async ValueTask RectifyTicket(Ticket ticket, CancellationToken cancellationToken = default)
    {
      if (ticket.DiscordId != null)
      {
        await _discCtx.UpdateMessageAsync(ticket.DiscordId.Value, ticket.ToEmbed(), cancellationToken);
      }
      else
      {
        var msg = await _discCtx.SendMessageAsync(ticket.ToEmbed(), cancellationToken);
        if (msg == null)
        {
          return;
        }
        ticket.DiscordId = msg.Id;
        await _tickCtx.AssignDiscordIdAsync(ticket.Id, msg.Id, cancellationToken);
      }
    }
  }
}
