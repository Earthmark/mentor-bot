using Discord;
using MentorBot.Extern;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public interface ITicketStore
  {
    ValueTask<Ticket?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> CreateTicket(TicketCreate createArgs, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryCompleteTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryClaimTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryUnclaimTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default);
  }

  public class TicketStore : ITicketStore, IDiscordReactionHandler
  {
    private readonly ITicketContext _tick;
    private readonly IMentorContext _ment;
    private readonly IDiscordContext _disc;
    private readonly INeosApi _neosApi;

    public TicketStore(ITicketContext tick, IMentorContext ment, IDiscordContext disc, INeosApi neosApi)
    {
      _tick = tick;
      _ment = ment;
      _disc = disc;
      _neosApi = neosApi;
    }

    public async ValueTask<Ticket?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
      return await _tick.GetTicket(ticketId, cancellationToken);
    }

    public async ValueTask<Ticket?> CreateTicket(TicketCreate createArgs, CancellationToken cancellationToken = default)
    {
      if(string.IsNullOrWhiteSpace(createArgs.UserId))
      {
        return null;
      }

      var user = await _neosApi.GetUser(createArgs.UserId, cancellationToken);
      if (user == null)
      {
        return null;
      }

      Ticket ticket = createArgs.Populate(new Ticket
      {
        UserId = user.Id,
        Username = user.Name,
        Status = TicketStatus.Requested,
        Created = DateTime.UtcNow
      });

      var msg = await _disc.SendTicketMessage(ticket, cancellationToken);
      if (msg == null)
      {
        return null;
      }

      ticket = ticket with
      {
        Id = msg.Id.ToString()
      };
      return await _tick.CreateTicket(ticket, cancellationToken);
    }

    public async ValueTask<Ticket?> TryCompleteTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var ticket = await GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Status != TicketStatus.Responding || ticket.MentorDiscordId != mentorDiscordId)
      {
        return null;
      }
      ticket = ticket with
      {
        Status = TicketStatus.Completed,
        Complete = DateTime.UtcNow,
      };
      return await _tick.UpdateTicket(ticket, cancellationToken);
    }

    public async ValueTask<Ticket?> TryClaimTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var mentorTask = _ment.GetMentor(mentorDiscordId, cancellationToken);
      var ticketTask = GetTicketAsync(ticketId, cancellationToken);
      var mentor = await mentorTask;
      var ticket = await ticketTask;
      if (mentor == null || ticket == null || ticket.Status != TicketStatus.Requested)
      {
        return null;
      }
      ticket = ticket with
      {
        Status = TicketStatus.Responding,
        Claimed = DateTime.UtcNow,
        MentorName = mentor.Name,
        MentorDiscordId = mentor.DiscordId,
        MentorNeosId = mentor.NeosId,
      };
      return await _tick.UpdateTicket(ticket, cancellationToken);
    }

    public async ValueTask<Ticket?> TryUnclaimTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var ticket = await GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Status != TicketStatus.Responding || ticket.MentorDiscordId != mentorDiscordId)
      {
        return null;
      }
      ticket = ticket with
      {
        Status = TicketStatus.Requested,
        Claimed = null,
        MentorName = null,
        MentorDiscordId = null,
        MentorNeosId = null,
      };
      return await _tick.UpdateTicket(ticket, cancellationToken);
    }

    async ValueTask IDiscordReactionHandler.Claim(ulong msg, IUser user, CancellationToken cancellationToken)
      => await TryClaimTicket(msg.ToString(), user.Id.ToString(), cancellationToken);

    async ValueTask IDiscordReactionHandler.Complete(ulong msg, IUser user, CancellationToken cancellationToken)
      => await TryCompleteTicket(msg.ToString(), user.Id.ToString(), cancellationToken);

    async ValueTask IDiscordReactionHandler.Unclaim(ulong msg, IUser user, CancellationToken cancellationToken)
      => await TryUnclaimTicket(msg.ToString(), user.Id.ToString(), cancellationToken);
  }
}
