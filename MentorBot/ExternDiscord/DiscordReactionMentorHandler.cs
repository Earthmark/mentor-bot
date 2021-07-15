using Discord;
using MentorBot.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.ExternDiscord
{
  public class DiscordReactionMentorHandler : IDiscordReactionHandler
  {
    private readonly TicketStore _store;
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordReactionMentorHandler> _logger;

    public DiscordReactionMentorHandler(TicketStore context, IOptionsSnapshot<DiscordOptions> options, ILogger<DiscordReactionMentorHandler> logger)
    {
      _store = context;
      _options = options.Value;
      _logger = logger;
    }

    public async Task ReactionAdded(ulong msg, IUser user, string reaction, CancellationToken cancellationToken = default)
    {
      if (_options.ClaimEmote == reaction)
      {
        _logger.LogTrace("Processing claim of ticket {0} by {1} ({2})", msg, user.Username, user.Id);
        await _store.TryClaimTicket(msg, user.Id, user.Username, cancellationToken);
      }
      else if (_options.CompleteEmote == reaction)
      {
        _logger.LogTrace("Processing completion of ticket {0} by {1} ({2})", msg, user.Username, user.Id);
        await _store.TryCompleteTicket(msg, user.Id, cancellationToken);
      }
    }

    public async Task ReactionRemoved(ulong msg, IUser user, string reaction, CancellationToken cancellationToken = default)
    {
      if (_options.ClaimEmote == reaction)
      {
        _logger.LogTrace("Processing unclaim of ticket {0} by {1} ({2})", msg, user.Username, user.Id);
        await _store.TryUnclaimTicket(msg, user.Id, cancellationToken);
      }
    }
  }
}
