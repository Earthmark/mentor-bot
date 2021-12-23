using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MentorBot.Models
{
  public interface ITicketNotifier
  {
    int GlobalWatchers { get; }
    IDisposable WatchTicketAdded(Action<Ticket> handler);
    IDisposable WatchTicketsUpdated(Action<Ticket> handler);
    IDisposable WatchTicketUpdated(ulong ticketId, Action<Ticket> handler);
    void NotifyNewTicket(Ticket ticket);
    void NotifyUpdatedTicket(Ticket ticket);
  }

  // Notifies subscribers to new and updated tickets.
  public class TicketNotifier : ITicketNotifier
  {
    private class TicketWatcher
    {
      public event Action<Ticket>? TicketUpdated;

      public void InvokeTicketUpdated(Ticket ticket)
      {
        TicketUpdated?.Invoke(ticket);
      }
    }

    private readonly ConcurrentDictionary<ulong, TicketWatcher> _ticketMonitor = new();

    private int _globalWatchers;

    public int GlobalWatchers => _globalWatchers;

    private event Action<Ticket>? TicketAdded;
    private event Action<Ticket>? TicketUpdated;

    public void NotifyNewTicket(Ticket ticket)
    {
      TicketAdded?.Invoke(ticket);
      NotifyUpdatedTicket(ticket);
    }

    public void NotifyUpdatedTicket(Ticket ticket)
    {
      TicketUpdated?.Invoke(ticket);
      if (_ticketMonitor.TryGetValue(ticket.Id, out var watcher))
      {
        watcher.InvokeTicketUpdated(ticket);
      }
    }

    private class DisposeableFunc : IDisposable
    {
      private readonly Action _disposer;

      public DisposeableFunc(Action disposer)
      {
        _disposer = disposer;
      }

      public void Dispose()
      {
        _disposer?.Invoke();
      }
    }

    public IDisposable WatchTicketAdded(Action<Ticket> handler)
    {
      TicketAdded += handler;
      Interlocked.Increment(ref _globalWatchers);
      return new DisposeableFunc(() =>
      {
        Interlocked.Decrement(ref _globalWatchers);
        TicketAdded -= handler;
      });
    }

    public IDisposable WatchTicketsUpdated(Action<Ticket> handler)
    {
      TicketUpdated += handler;
      return new DisposeableFunc(() => TicketUpdated -= handler);
    }

    public IDisposable WatchTicketUpdated(ulong ticketId, Action<Ticket> handler)
    {
      var monitor = _ticketMonitor.GetOrAdd(ticketId, id => new TicketWatcher());
      monitor.TicketUpdated += handler;
      return new DisposeableFunc(() => monitor.TicketUpdated -= handler);
    }
  }
}
