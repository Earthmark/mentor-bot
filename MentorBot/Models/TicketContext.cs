using MentorBot.Extern;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public interface ITicketContext
  {
    IQueryable<Ticket> GetIncompleteTickets();
    ValueTask<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> CreateTicketAsync(TicketCreate createArgs, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryCompleteTicketAsync(ulong ticketId, string mentorToken, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryCancelTicketAsync(ulong ticketId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryClaimTicketAsync(ulong ticketId, string mentorToken, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> TryUnclaimTicketAsync(ulong ticketId, string mentorToken, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> AssignDiscordIdAsync(ulong ticket, ulong discordId, CancellationToken cancellationToken = default);
  }

  public class TicketContext : ITicketContext
  {
    private readonly SignalContext _ctx;
    private readonly ITicketNotifier _notifier;
    private readonly INeosApi _neosApi;

    public TicketContext(SignalContext ctx, ITicketNotifier notifier, INeosApi neosApi)
    {
      _ctx = ctx;
      _notifier = notifier;
      _neosApi = neosApi;
    }

    public IQueryable<Ticket> GetIncompleteTickets()
    {
      return _ctx.Tickets.Where(t => t.Status == TicketStatus.Requested || t.Status == TicketStatus.Responding);
    }

    public async ValueTask<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
      return await _ctx.GetTicketAsync(ticketId, cancellationToken);
    }

    public async ValueTask<Ticket?> CreateTicketAsync(TicketCreate createArgs, CancellationToken cancellationToken = default)
    {
      if (createArgs.UserId == null)
      {
        return null;
      }

      User? user = await _neosApi.GetUserAsync(createArgs.UserId, cancellationToken);
      if (user == null)
      {
        return null;
      }

      Ticket ticket = new(createArgs, user)
      {
        Status = TicketStatus.Requested,
        Created = DateTime.UtcNow
      };

      _ctx.Tickets.Add(ticket);
      await _ctx.SaveChangesAsync(cancellationToken);
      _notifier.NotifyNewTicket(ticket);
      return ticket;
    }

    public async ValueTask<Ticket?> TryClaimTicketAsync(ulong ticketId, string mentorToken, CancellationToken cancellationToken = default)
    {
      var ticket = await _ctx.GetTicketAsync(ticketId, cancellationToken);
      var mentor = await _ctx.GetMentorByTokenAsync(mentorToken, cancellationToken);
      if (ticket == null || mentor == null)
      {
        return null;
      }
      if (ticket.Status == TicketStatus.Requested)
      {
        ticket.Mentor = mentor;
        ticket.Status = TicketStatus.Responding;
        ticket.Claimed = DateTime.UtcNow;

        _ctx.Tickets.Update(ticket);
        await _ctx.SaveChangesAsync(cancellationToken);
        _notifier.NotifyUpdatedTicket(ticket);
      }
      return ticket;
    }

    public async ValueTask<Ticket?> TryUnclaimTicketAsync(ulong ticketId, string mentorToken, CancellationToken cancellationToken = default)
    {
      var ticket = await _ctx.GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Mentor?.Token != mentorToken)
      {
        return null;
      }
      if (ticket.Status == TicketStatus.Responding)
      {
        ticket.Status = TicketStatus.Requested;
        ticket.Claimed = null;
        ticket.Mentor = null;

        _ctx.Tickets.Update(ticket);
        await _ctx.SaveChangesAsync(cancellationToken);
        _notifier.NotifyUpdatedTicket(ticket);
      }
      return ticket;
    }

    public async ValueTask<Ticket?> TryCompleteTicketAsync(ulong ticketId, string mentorToken, CancellationToken cancellationToken = default)
    {
      var ticket = await _ctx.GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null || ticket.Mentor?.Token != mentorToken)
      {
        return null;
      }
      if (ticket.Status == TicketStatus.Responding)
      {
        ticket.Status = TicketStatus.Completed;
        ticket.Complete = DateTime.UtcNow;

        _ctx.Tickets.Update(ticket);
        await _ctx.SaveChangesAsync(cancellationToken);
        _notifier.NotifyUpdatedTicket(ticket);
      }
      return ticket;
    }

    public async ValueTask<Ticket?> TryCancelTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
      var ticket = await _ctx.GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null)
      {
        return null;
      }
      if (!ticket.Status.IsTerminal())
      {
        ticket.Status = TicketStatus.Canceled;
        ticket.Canceled = DateTime.UtcNow;

        _ctx.Tickets.Update(ticket);
        await _ctx.SaveChangesAsync(cancellationToken);
        _notifier.NotifyUpdatedTicket(ticket);
      }
      return ticket;
    }

    public async ValueTask<Ticket?> AssignDiscordIdAsync(ulong ticketId, ulong discordId, CancellationToken cancellationToken = default)
    {
      var ticket = await _ctx.GetTicketAsync(ticketId, cancellationToken);
      if (ticket == null)
      {
        return null;
      }
      ticket.DiscordId = discordId;
      _ctx.Tickets.Update(ticket);
      await _ctx.SaveChangesAsync(cancellationToken);
      return ticket;
    }
  }
}
