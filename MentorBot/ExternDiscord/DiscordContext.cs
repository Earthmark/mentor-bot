using Discord;
using Discord.WebSocket;
using MentorBot.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.ExternDiscord
{
  public enum Reaction
  {
    Claim,
    Complete
  }

  public interface IDiscordReactionHandler
  {
    Task Claim(ulong msg, IUser user, CancellationToken cancellationToken = default);
    Task Unclaim(ulong msg, IUser user, CancellationToken cancellationToken = default);
    Task Complete(ulong msg, IUser user, CancellationToken cancellationToken = default);
  }

  public class DiscordOptions
  {
    public string Token { get; set; } = string.Empty;
    public ulong Channel { get; set; }
    public string ClaimEmote { get; set; } = string.Empty;
    public string CompleteEmote { get; set; } = string.Empty;
  }

  public class DiscordContext : IHostedService, IHealthCheck, IDisposable
  {
    private readonly DiscordSocketClient _client;
    private readonly DiscordOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordContext> _logger;
    private readonly IDisposable _watchDispose;
    private ITextChannel? _channel;
    private bool isReady;

    public DiscordContext(ITicketNotifier notifier, IOptions<DiscordOptions> options, IServiceProvider serviceProvider, ILogger<DiscordContext> logger)
    {
      _client = new DiscordSocketClient(new DiscordSocketConfig
      {
        MessageCacheSize = 50,
        LogLevel = LogSeverity.Debug
      });
      _client.Log += Client_Log;
      _options = options.Value;
      _serviceProvider = serviceProvider;
      _logger = logger;
      _client.ReactionAdded += ReactionHandler_Wrapped(ReactionAdded);
      _client.ReactionRemoved += ReactionHandler_Wrapped(ReactionRemoved);
      _client.Ready += () => Task.FromResult(isReady = true);
      _watchDispose = notifier.WatchTicketsUpdated(HeadlessTicketUpdate);
    }

    private async void HeadlessTicketUpdate(Ticket ticket)
    {
      try
      {
        await UpdateTicket(ticket);
      }
      catch(Exception e)
      {
        _logger.LogWarning(e, "Error while dealing with detached ticket handler.");
      }
    }

    public async Task<IUserMessage?> SendTicketMessage(Ticket ticket, CancellationToken cancellationToken = default)
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

    public async Task<IUserMessage?> UpdateTicket(Ticket ticket, CancellationToken cancellationToken = default)
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


    private Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
      ReactionHandler_Wrapped(Func<IDiscordReactionHandler, ulong, IUser, Reaction, CancellationToken, Task> bodyGetter) =>
      (Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction) =>
      {
        _ = Task.Run(() => ReactionHandler(msg, channel, reaction, bodyGetter));
        return Task.CompletedTask;
      };

    private async Task ReactionHandler(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction, Func<IDiscordReactionHandler, ulong, IUser, Reaction, CancellationToken, Task> bodyGetter)
    {
      if (channel.Id != _options.Channel || _client.Rest.CurrentUser.Id == reaction.UserId)
      {
        return;
      }

      Reaction rea;
      if (reaction.Emote.Name == _options.ClaimEmote)
      {
        rea = Reaction.Claim;
      }
      else if (reaction.Emote.Name == _options.CompleteEmote)
      {
        rea = Reaction.Complete;
      }
      else
      {
        return;
      }

      try
      {
        var user = reaction.User.GetValueOrDefault() ?? await _client.Rest.GetUserAsync(reaction.UserId);
        await using var scope = _serviceProvider.CreateAsyncScope();
        foreach (var handler in scope.ServiceProvider.GetServices<IDiscordReactionHandler>())
        {
          await bodyGetter(handler, msg.Id, user, rea, new CancellationToken());
        }
      }
      catch (Exception e)
      {
        _logger.LogWarning(e, "Error while handing reaction.");
      }
    }

    private async Task ReactionAdded(IDiscordReactionHandler handler, ulong ticketId, IUser user, Reaction reaction, CancellationToken cancellationToken)
    {
      switch (reaction)
      {
        case Reaction.Claim:
          await handler.Claim(ticketId, user, cancellationToken);
          break;
        case Reaction.Complete:
          await handler.Complete(ticketId, user, cancellationToken);
          break;
      }
    }

    private async Task ReactionRemoved(IDiscordReactionHandler handler, ulong ticketId, IUser user, Reaction reaction, CancellationToken cancellationToken)
    {
      if (reaction == Reaction.Claim)
      {
        await handler.Unclaim(ticketId, user, cancellationToken);
      }
    }

    public void Dispose()
    {
      _client.Dispose();
      _watchDispose.Dispose();
    }
  }
}
