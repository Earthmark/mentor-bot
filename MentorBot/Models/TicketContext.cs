using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public interface ITicketContext
  {
    ValueTask<Ticket?> GetTicketAsync(ulong id, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> CreateTicketAsync(Ticket item, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> UpdateTicketAsync(ulong id, Func<Ticket, bool> filter, Action<Ticket> mutator, CancellationToken cancellationToken = default);
  }

  public class TicketContext : ITicketContext
  {
    private readonly SignalContext _ctx;
    private readonly ITicketNotifier _notifier;


    public TicketContext(SignalContext ctx, ITicketNotifier notifier)
    {
      _ctx = ctx;
      _notifier = notifier;
    }

    public async ValueTask<Ticket?> GetTicketAsync(ulong id, CancellationToken cancellationToken = default)
    {
      long cId = unchecked((long)id);
      return await _ctx.Tickets.AsNoTracking().SingleOrDefaultAsync(t => t._Id == cId, cancellationToken: cancellationToken);
    }

    public async ValueTask<Ticket?> CreateTicketAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
      _ctx.Tickets.Add(ticket);
      await _ctx.SaveChangesAsync(cancellationToken);
      _notifier.NotifyNewTicket(ticket);
      return ticket;
    }

    public async ValueTask<Ticket?> UpdateTicketAsync(ulong id, Func<Ticket, bool> filter, Action<Ticket> mutator, CancellationToken cancellationToken = default)
    {
      long cId = unchecked((long)id);
      var ticket = await _ctx.Tickets.SingleOrDefaultAsync(t => t._Id == cId, cancellationToken: cancellationToken);
      if (ticket == null || !filter(ticket))
      {
        return null;
      }
      mutator(ticket);
      _ctx.Tickets.Update(ticket);
      await _ctx.SaveChangesAsync(cancellationToken);
      _notifier.NotifyUpdatedTicket(ticket);
      return ticket;
    }
  }
}
