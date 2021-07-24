using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Extern
{
  public interface IDiscordReactionHandler
  {
    ValueTask Claim(ulong msg, IUser user, CancellationToken cancellationToken = default);
    ValueTask Unclaim(ulong msg, IUser user, CancellationToken cancellationToken = default);
    ValueTask Complete(ulong msg, IUser user, CancellationToken cancellationToken = default);
  }

  public class ReactionProcessor : IHostedService
  {
    private readonly IDiscordContext _ctx;
    private readonly DiscordOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReactionProcessor> _logger;

    private Action? _stop;

    public ReactionProcessor(IDiscordContext ctx, IOptions<DiscordOptions> options, IServiceProvider serviceProvider, ILogger<ReactionProcessor> logger)
    {
      _ctx = ctx;
      _options = options.Value;
      _serviceProvider = serviceProvider;
      _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      var addedHandler = ReactionHandler_Wrapped(ReactionAdded);
      var removedHandler = ReactionHandler_Wrapped(ReactionRemoved);
      _ctx.Client.ReactionAdded += addedHandler;
      _ctx.Client.ReactionRemoved += removedHandler;
      _stop = () =>
      {
        _ctx.Client.ReactionAdded -= addedHandler;
        _ctx.Client.ReactionRemoved -= removedHandler;
      };
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _stop?.Invoke();
      return Task.CompletedTask;
    }

    private Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task>
      ReactionHandler_Wrapped(Func<IDiscordReactionHandler, ulong, IUser, Reaction, CancellationToken, ValueTask> bodyGetter) =>
      (Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction) =>
      {
        _ = Task.Run(() => ReactionHandler(msg, channel, reaction, bodyGetter));
        return Task.CompletedTask;
      };

    private async Task ReactionHandler(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction, Func<IDiscordReactionHandler, ulong, IUser, Reaction, CancellationToken, ValueTask> bodyGetter)
    {
      if (channel.Id != _options.Channel || _ctx.Client.Rest.CurrentUser.Id == reaction.UserId)
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
        var user = reaction.User.GetValueOrDefault() ?? await _ctx.Client.Rest.GetUserAsync(reaction.UserId);
        using var scope = _serviceProvider.CreateScope();
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

    private async ValueTask ReactionAdded(IDiscordReactionHandler handler, ulong ticketId, IUser user, Reaction reaction, CancellationToken cancellationToken)
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

    private async ValueTask ReactionRemoved(IDiscordReactionHandler handler, ulong ticketId, IUser user, Reaction reaction, CancellationToken cancellationToken)
    {
      if (reaction == Reaction.Claim)
      {
        await handler.Unclaim(ticketId, user, cancellationToken);
      }
    }

  }
}
