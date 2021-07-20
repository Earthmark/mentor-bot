using Discord;
using MentorBot.ExternDiscord;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public class TicketStore : IDiscordReactionHandler
  {
    private readonly TicketContext _tick;
    private readonly MentorContext _ment;
    private readonly DiscordContext _disc;

    public TicketStore(TicketContext tick, MentorContext ment, DiscordContext disc)
    {
      _tick = tick;
      _ment = ment;
      _disc = disc;
    }

    public async Task<Ticket?> CreateTicket(Ticket ticket, CancellationToken cancellationToken = default)
    {
      ticket.Status = TicketStatus.Requested;
      ticket.Created = DateTime.UtcNow;
      var msg = await _disc.SendTicketMessage(ticket, cancellationToken);
      if (msg == null)
      {
        return null;
      }
      ticket.Id = msg.Id.ToString();
      return await _tick.CreateTicket(ticket, cancellationToken);
    }

    public Task<Ticket?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
      return _tick.GetTicket(ticketId, cancellationToken);
    }

    public async Task<Ticket?> TryCompleteTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var ticket = await GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Status != TicketStatus.Responding || ticket.MentorDiscordId != mentorDiscordId)
      {
        return null;
      }
      ticket.Status = TicketStatus.Completed;
      ticket.Complete = DateTime.UtcNow;
      return await _tick.UpdateTicket(ticket, cancellationToken);
    }

    public async Task<Ticket?> TryClaimTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var mentorTask = _ment.GetMentor(mentorDiscordId, cancellationToken);
      var ticketTask = GetTicketAsync(ticketId, cancellationToken);
      var mentor = await mentorTask;
      if (mentor == null)
      {
        return null;
      }
      var ticket = await ticketTask;
      if (ticket == null || ticket.Status != TicketStatus.Requested)
      {
        return null;
      }
      ticket.Status = TicketStatus.Responding;
      ticket.Claimed = DateTime.UtcNow;
      ticket.MentorName = mentor.Name;
      ticket.MentorDiscordId = mentor.DiscordId;
      ticket.MentorNeosId = mentor.NeosId;
      return await _tick.UpdateTicket(ticket);
    }

    public async Task<Ticket?> TryUnclaimTicket(string ticketId, string mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var ticket = await GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Status != TicketStatus.Responding || ticket.MentorDiscordId != mentorDiscordId)
      {
        return null;
      }
      ticket.Status = TicketStatus.Requested;
      ticket.Claimed = null;
      ticket.MentorName = null;
      ticket.MentorDiscordId = null;
      ticket.MentorNeosId = null;
      return await _tick.UpdateTicket(ticket);
    }

    Task IDiscordReactionHandler.Claim(ulong msg, IUser user, CancellationToken cancellationToken)
      => TryClaimTicket(msg.ToString(), user.Id.ToString(), cancellationToken);

    Task IDiscordReactionHandler.Complete(ulong msg, IUser user, CancellationToken cancellationToken)
      => TryCompleteTicket(msg.ToString(), user.Id.ToString(), cancellationToken);

    Task IDiscordReactionHandler.Unclaim(ulong msg, IUser user, CancellationToken cancellationToken)
      => TryUnclaimTicket(msg.ToString(), user.Id.ToString(), cancellationToken);
  }
}
