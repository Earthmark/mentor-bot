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
  public interface IDiscordReactionHandler
  {
    Task ReactionAdded(ulong msg, IUser user, string reaction, CancellationToken cancellationToken = default);
    Task ReactionRemoved(ulong msg, IUser user, string reaction, CancellationToken cancellationToken = default);
  }

  public class DiscordContext : IHostedService, IHealthCheck, IDisposable
  {
    private readonly DiscordSocketClient _client;
    private readonly DiscordOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiscordContext> _logger;
    private ITextChannel? _channel;
    private bool isReady;

    public DiscordContext(IOptions<DiscordOptions> options, IServiceProvider serviceProvider, ILogger<DiscordContext> logger)
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
      _client.ReactionAdded += ReactionHandler_Wrapped(h => h.ReactionAdded);
      _client.ReactionRemoved += ReactionHandler_Wrapped(h => h.ReactionRemoved);
      _client.Ready += () => Task.FromResult(isReady = true);
    }

    public async Task<IUserMessage?> SendTicketMessage(Ticket ticket, CancellationToken cancellationToken = default)
    {
      if (_channel == null)
      {
        return null;
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
        HealthCheckResult.Healthy("Discord service is ready and bound."):
        HealthCheckResult.Unhealthy("Discord bot api has disconnected."));
    }

    private Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
      ReactionHandler_Wrapped(Func<IDiscordReactionHandler, Func<ulong, IUser, string, CancellationToken, Task>> bodyGetter) =>
      (Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction) =>
    {
      _ = Task.Run(() => ReactionHandler(msg, channel, reaction, bodyGetter));
      return Task.CompletedTask;
    };

    private async Task ReactionHandler(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction, Func<IDiscordReactionHandler, Func<ulong, IUser, string, CancellationToken, Task>> bodyGetter)
    {
      if (channel.Id != _options.Channel)
      {
        return;
      }

      try
      {
        var user = reaction.User.GetValueOrDefault() ?? await _client.Rest.GetUserAsync(reaction.UserId);
        await using var scope = _serviceProvider.CreateAsyncScope();
        foreach (var handler in scope.ServiceProvider.GetServices<IDiscordReactionHandler>())
        {
          await bodyGetter(handler)(msg.Id, user, reaction.Emote.Name, new CancellationToken());
        }
      }
      catch (Exception e)
      {
        _logger.LogWarning(e, "Error while handing reaction.");
      }
    }

    public void Dispose()
    {
      _client.Dispose();
    }
  }
}
