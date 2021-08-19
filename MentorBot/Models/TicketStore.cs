using Discord;
using MentorBot.Extern;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public interface ITicketStore
  {
    ValueTask<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> CreateTicketAsync(TicketCreate createArgs, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryCompleteTicketAsync(ulong ticketId, ulong mentorDiscordId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryCancelTicketAsync(ulong ticketId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryClaimTicketAsync(ulong ticketId, ulong mentorDiscordId, string mentorName, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryUnclaimTicketAsync(ulong ticketId, ulong mentorDiscordId, CancellationToken cancellationToken = default);
  }

  public class TicketStore : ITicketStore, IDiscordReactionHandler
  {
    private readonly ITicketContext _tick;
    private readonly IDiscordContext _disc;
    private readonly INeosApi _neosApi;

    public TicketStore(ITicketContext tick,IDiscordContext disc, INeosApi neosApi)
    {
      _tick = tick;
      _disc = disc;
      _neosApi = neosApi;
    }

    public async ValueTask<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
      return await _tick.GetTicketAsync(ticketId, cancellationToken);
    }

    public async ValueTask<Ticket?> CreateTicketAsync(TicketCreate createArgs, CancellationToken cancellationToken = default)
    {
      if (createArgs.UserId == null)
      {
        return null;
      }

      User? user = await _neosApi.GetUser(createArgs.UserId, cancellationToken);
      if (user == null)
      {
        return null;
      }

      Ticket ticket = new(createArgs, user)
      {
        Status = TicketStatus.Requested,
        Created = DateTime.UtcNow
      };

      var msg = await _disc.SendTicketMessage(ticket, cancellationToken);
      if (msg == null)
      {
        return null;
      }

      ticket.Id = msg.Id;
      return await _tick.CreateTicketAsync(ticket, cancellationToken);
    }

    public async ValueTask<Ticket?> TryCompleteTicketAsync(ulong ticketId, ulong mentorDiscordId, CancellationToken cancellationToken = default)
    {
      return await _tick.UpdateTicketAsync(ticketId,
        ticket => ticket.Status == TicketStatus.Responding && ticket.Mentor?.DiscordId == mentorDiscordId,
        ticket =>
        {
          ticket.Status = TicketStatus.Completed;
          ticket.Complete = DateTime.UtcNow;
        },
        cancellationToken);
    }

    public async ValueTask<Ticket?> TryCancelTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
      return await _tick.UpdateTicketAsync(ticketId,
        ticket => !ticket.Status.IsTerminal(),
        ticket =>
        {
          ticket.Status = TicketStatus.Canceled;
          ticket.Canceled = DateTime.UtcNow;
        }, cancellationToken);
    }

    public async ValueTask<Ticket?> TryClaimTicketAsync(ulong ticketId, ulong mentorDiscordId, string mentorName, CancellationToken cancellationToken = default)
    {
      return await _tick.UpdateTicketAsync(ticketId,
        ticket => ticket.Status == TicketStatus.Requested,
        ticket =>
        {
          ticket.Mentor = new Mentor
          {
            DiscordId = mentorDiscordId,
            Name = mentorName,
          };
          ticket.Status = TicketStatus.Responding;
          ticket.Claimed = DateTime.UtcNow;
        }, cancellationToken);
    }

    public async ValueTask<Ticket?> TryUnclaimTicketAsync(ulong ticketId, ulong mentorDiscordId, CancellationToken cancellationToken = default)
    {
      return await _tick.UpdateTicketAsync(ticketId,
        ticket => ticket.Status == TicketStatus.Responding && ticket.Mentor?.DiscordId == mentorDiscordId,
        ticket =>
        {
          ticket.Status = TicketStatus.Requested;
          ticket.Claimed = null;
          ticket.Mentor = null;
        }, cancellationToken);
    }

    async ValueTask IDiscordReactionHandler.Claim(ulong msg, IUser user, CancellationToken cancellationToken)
      => await TryClaimTicketAsync(msg, user.Id, user.Username, cancellationToken);

    async ValueTask IDiscordReactionHandler.Complete(ulong msg, IUser user, CancellationToken cancellationToken)
      => await TryCompleteTicketAsync(msg, user.Id, cancellationToken);

    async ValueTask IDiscordReactionHandler.Unclaim(ulong msg, IUser user, CancellationToken cancellationToken)
      => await TryUnclaimTicketAsync(msg, user.Id, cancellationToken);
  }
}
