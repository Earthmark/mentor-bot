using System;
using System.Threading;
using System.Threading.Tasks;
using MentorBot.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MentorBot.Discord;

public class TicketDiscordProxyHost(
    IServiceProvider serviceProvider,
    ITicketNotifier notifier,
    ILogger<TicketDiscordProxyHost> logger)
    : IHostedService
{
    private readonly CancellationTokenSource _cancelSource = new();

    private IDisposable? _watchToken;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _watchToken, notifier.WatchTicketsUpdated(TicketUpdated))?.Dispose();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _watchToken, null)?.Dispose();
        _cancelSource.Cancel();
        return Task.CompletedTask;
    }

    private async void TicketUpdated(Ticket ticket)
    {
        try
        {
            await UpdateTicketInternal(ticket, _cancelSource.Token);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Error while updating an internal ticket.");
        }
        // This warning is not always true.
#pragma warning disable CS1058 // A previous catch clause already catches all exceptions
        catch
#pragma warning restore CS1058 // A previous catch clause already catches all exceptions
        {
            logger.LogWarning("Abstract error while updating internal ticket, this is bad.");
        }
    }

    private async ValueTask UpdateTicketInternal(Ticket ticket, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ITicketDiscordProxy>().RectifyTicket(ticket, cancellationToken);
    }
}