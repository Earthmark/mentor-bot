using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MentorBot.Discord;

public class DiscordHostedServiceProxy(DiscordContext ctx) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return ctx.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return ctx.StopAsync(cancellationToken);
    }
}