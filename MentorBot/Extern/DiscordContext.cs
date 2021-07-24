using Discord;
using Discord.WebSocket;
using MentorBot.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
    ValueTask<IUserMessage?> SendTicketMessage(Ticket ticket, CancellationToken cancellationToken = default);
  }

  public enum Reaction
  {
    Claim,
    Complete
  }

  public class DiscordContext : IDiscordContext, IHostedService, IHealthCheck, IDisposable
  {
    private readonly DiscordSocketClient _client;
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordContext> _logger;
    private readonly IDisposable _watchDispose;
    private ITextChannel? _channel;
    private bool isReady;

    public DiscordSocketClient Client => _client;

    public DiscordContext(ITicketNotifier notifier, IOptions<DiscordOptions> options, ILogger<DiscordContext> logger)
    {
      _client = new DiscordSocketClient(new DiscordSocketConfig
      {
        MessageCacheSize = 50,
        LogLevel = LogSeverity.Debug
      });
      _client.Log += Client_Log;
      _options = options.Value;
      _logger = logger;
      _client.Ready += () => Task.FromResult(isReady = true);
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
      if (_channel == null)
      {
        throw new InvalidOperationException("channel not bound to discord context.");
      }
      var msg = await _channel.SendMessageAsync(embed: ticket.ToEmbed(), options: new RequestOptions
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
      if (_channel == null)
      {
        throw new InvalidOperationException("channel not bound to discord context.");
      }
      return ulong.TryParse(ticket.Id, out var id) ? await _channel.ModifyMessageAsync(id, props => props.Embed = ticket.ToEmbed(), new RequestOptions
      {
        CancelToken = cancellationToken
      }) : null;
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
      await _client.LoginAsync(TokenType.Bot, _options.Token);
      await _client.StartAsync();
      _channel = await _client.Rest.GetChannelAsync(_options.Channel) as ITextChannel;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
      await _client.StopAsync();
      await _client.LogoutAsync();
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(_client.ConnectionState == ConnectionState.Connected && isReady ?
        HealthCheckResult.Healthy("Discord service is ready and bound.") :
        HealthCheckResult.Unhealthy("Discord bot api has disconnected."));
    }

    public void Dispose()
    {
      _client.Dispose();
      _watchDispose.Dispose();
    }
  }
}
