using MentorBot.ExternDiscord;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public class TicketStore
  {
    private readonly TicketContext _tick;
    private readonly DiscordContext _disc;

    public TicketStore(TicketContext tick, DiscordContext disc)
    {
      _tick = tick;
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
      ticket.Id = msg.Id;
      _tick.Tickets.Add(ticket);
      await _tick.SaveChangesAsync(cancellationToken);
      return ticket;
    }

    public Task<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
      return _tick.Tickets.AsNoTracking().FirstOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);
    }

    public async Task<Ticket?> TryCompleteTicket(ulong ticketId, ulong mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var ticket = await GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Status != TicketStatus.Requested || ticket.MentorDiscordId != mentorDiscordId)
      {
        return null;
      }
      ticket.Status = TicketStatus.Completed;
      ticket.Complete = DateTime.UtcNow;
      _tick.Tickets.Update(ticket);
      await _tick.SaveChangesAsync(cancellationToken);
      return ticket;
    }

    public async Task<Ticket?> TryClaimTicket(ulong ticketId, ulong mentorDiscordId, string mentorDiscordName, CancellationToken cancellationToken = default)
    {
      var ticket = await GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Status != TicketStatus.Requested)
      {
        return null;
      }
      ticket.Status = TicketStatus.Responding;
      ticket.Claimed = DateTime.UtcNow;
      ticket.Mentor = mentorDiscordName;
      ticket.MentorDiscordId = mentorDiscordId;
      _tick.Tickets.Update(ticket);
      await _tick.SaveChangesAsync(cancellationToken);
      return ticket;
    }

    public async Task<Ticket?> TryUnclaimTicket(ulong ticketId, ulong mentorDiscordId, CancellationToken cancellationToken = default)
    {
      var ticket = await GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Status != TicketStatus.Requested || ticket.MentorDiscordId != mentorDiscordId)
      {
        return null;
      }
      ticket.Status = TicketStatus.Requested;
      ticket.Claimed = null;
      ticket.Mentor = null;
      ticket.MentorDiscordId = null;
      ticket.MentorNeosId = null;
      _tick.Tickets.Update(ticket);
      await _tick.SaveChangesAsync(cancellationToken);
      return ticket;
    }
  }
}
