using Discord;
using Discord.WebSocket;
using MentorBot.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Extern
{
  public interface IDiscordContext
  {
    DiscordSocketClient Client { get; }
    bool Ready { get; }
    ValueTask<IUserMessage?> SendTicketMessage(Ticket ticket, CancellationToken cancellationToken = default);
  }

  public enum Reaction
  {
    Claim,
    Complete
  }

  public class DiscordContext : IDiscordContext, IHostedService, IDisposable
  {
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordContext> _logger;
    private readonly IDisposable _watchDispose;

    public DiscordSocketClient Client { get; }
    public ITextChannel? Channel { get; private set; }
    public bool Ready { get; private set; }

    public DiscordContext(ITicketNotifier notifier, IOptions<DiscordOptions> options, ILogger<DiscordContext> logger)
    {
      Client = new DiscordSocketClient(new DiscordSocketConfig
      {
        MessageCacheSize = 50,
        LogLevel = LogSeverity.Debug
      });
      Client.Log += Client_Log;
      _options = options.Value;
      _logger = logger;
      Client.Ready += () => Task.FromResult(Ready = true);
      _watchDispose = notifier.WatchTicketsUpdated(HeadlessTicketUpdate);
    }

    private async void HeadlessTicketUpdate(Ticket ticket)
    {
      try
      {
        if (_options.UpdateTickets)
        {
          await UpdateTicket(ticket);
        }
      }
      catch(Exception e)
      {
        _logger.LogWarning(e, "Error while dealing with detached ticket handler.");
      }
    }

    public async ValueTask<IUserMessage?> SendTicketMessage(Ticket ticket, CancellationToken cancellationToken = default)
    {
      if (Channel == null)
      {
        throw new InvalidOperationException("channel not bound to discord context.");
      }
      var msg = await Channel.SendMessageAsync(embed: ticket.ToEmbed(), options: new RequestOptions
      {
        CancelToken = cancellationToken
      });
      await msg.AddReactionsAsync(new IEmote[]
      {
        new Emoji(_options.ClaimEmote),
        new Emoji(_options.CompleteEmote),
      });
      return msg;
    }

    public async ValueTask<IUserMessage?> UpdateTicket(Ticket ticket, CancellationToken cancellationToken = default)
    {
      if (Channel == null)
      {
        throw new InvalidOperationException("channel not bound to discord context.");
      }
      return  await Channel.ModifyMessageAsync(ticket.Id, props => props.Embed = ticket.ToEmbed(), new RequestOptions
      {
        CancelToken = cancellationToken
      });
    }

    private Task Client_Log(LogMessage arg)
    {
      switch (arg.Severity)
      {
        case LogSeverity.Critical:
          _logger.Log(LogLevel.Critical, arg.Exception, arg.Message);
          break;
        case LogSeverity.Error:
          _logger.Log(LogLevel.Error, arg.Exception, arg.Message);
          break;
        case LogSeverity.Warning:
          _logger.Log(LogLevel.Warning, arg.Exception, arg.Message);
          break;
        case LogSeverity.Info:
          _logger.Log(LogLevel.Information, arg.Exception, arg.Message);
          break;
        case LogSeverity.Verbose:
          _logger.Log(LogLevel.Trace, arg.Exception, arg.Message);
          break;
        case LogSeverity.Debug:
          _logger.Log(LogLevel.Debug, arg.Exception, arg.Message);
          break;
      }
      return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      await Client.LoginAsync(TokenType.Bot, _options.Token);
      await Client.StartAsync();
      Channel = await Client.Rest.GetChannelAsync(_options.Channel) as ITextChannel;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
      await Client.StopAsync();
      await Client.LogoutAsync();
    }

    public void Dispose()
    {
      Client.Dispose();
      _watchDispose.Dispose();
    }
  }
}

namespace Microsoft.Extensions.DependencyInjection
{
  using MentorBot.Extern;
  using Microsoft.Extensions.Configuration;

  public static class DiscordContextExtensions
  {
    public static IServiceCollection AddDiscordContext(this IServiceCollection services, IConfiguration config)
    {
      return services.Configure<DiscordOptions>(config.GetSection("Discord"))
        .AddSingleton<DiscordContext>()
        .AddSingleton<IDiscordContext, DiscordContext>(o => o.GetRequiredService<DiscordContext>())
        .AddHostedService(o => o.GetRequiredService<DiscordContext>());
    }
  }
}
