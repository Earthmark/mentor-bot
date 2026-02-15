using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MentorBot.Models;

public interface ISignalContext : IDisposable
{
    IQueryable<Ticket> Tickets { get; }
    IQueryable<Mentor> Mentors { get; }
    void Add(Ticket ticket);
    void Update(Ticket ticket);
    void Add(Mentor ticket);
    void Update(Mentor ticket);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    void EnsureCreated();
}

public class SignalContext : DbContext, ISignalContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SignalContext(DbContextOptions<SignalContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; }

    public DbSet<Mentor> Mentors { get; set; }

    IQueryable<Ticket> ISignalContext.Tickets => Tickets.Include(t => t.Mentor);

    IQueryable<Mentor> ISignalContext.Mentors => Mentors;

    void ISignalContext.Add(Ticket ticket)
    {
        Tickets.Add(ticket);
    }

    void ISignalContext.Add(Mentor mentor)
    {
        Mentors.Add(mentor);
    }

    void ISignalContext.Update(Ticket ticket)
    {
        Tickets.Update(ticket);
    }

    void ISignalContext.Update(Mentor mentor)
    {
        Mentors.Update(mentor);
    }

    Task ISignalContext.SaveChangesAsync(CancellationToken cancellationToken)
    {
        return SaveChangesAsync(cancellationToken);
    }

    public void EnsureCreated()
    {
        Database.EnsureCreated();
    }
}