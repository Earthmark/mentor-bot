using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MentorBot.Extern;
using Microsoft.EntityFrameworkCore;

namespace MentorBot.Models;

public interface ITicketContext
{
    IAsyncEnumerable<Ticket> GetIncompleteTickets();
    ValueTask<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default);
    ValueTask<Ticket?> CreateTicketAsync(TicketCreate createArgs, CancellationToken cancellationToken = default);

    ValueTask<Ticket?> TryCompleteTicketAsync(ulong ticketId, string mentorToken,
        CancellationToken cancellationToken = default);

    ValueTask<Ticket?> TryCancelTicketAsync(ulong ticketId, CancellationToken cancellationToken = default);

    ValueTask<Ticket?> TryClaimTicketAsync(ulong ticketId, string mentorToken,
        CancellationToken cancellationToken = default);

    ValueTask<Ticket?> TryUnclaimTicketAsync(ulong ticketId, string mentorToken,
        CancellationToken cancellationToken = default);

    ValueTask<Ticket?> AssignDiscordIdAsync(ulong ticket, ulong discordId,
        CancellationToken cancellationToken = default);
}

public class TicketContext(ISignalContext ctx, IMentorContext mentorCtx, ITicketNotifier notifier, IResoniteApi neosApi)
    : ITicketContext
{
    public IAsyncEnumerable<Ticket> GetIncompleteTickets()
    {
        return ctx.Tickets.Where(t => t.Status == TicketStatus.Requested || t.Status == TicketStatus.Responding)
            .AsAsyncEnumerable();
    }

    public async ValueTask<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
        return await ctx.Tickets.SingleOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
    }

    public async ValueTask<Ticket?> CreateTicketAsync(TicketCreate createArgs,
        CancellationToken cancellationToken = default)
    {
        if (createArgs.UserId == null) return null;

        var user = await neosApi.GetUserAsync(createArgs.UserId, cancellationToken);
        if (user == null) return null;

        Ticket ticket = new(createArgs, user)
        {
            Status = TicketStatus.Requested,
            Created = DateTime.UtcNow
        };

        ctx.Add(ticket);
        await ctx.SaveChangesAsync(cancellationToken);
        notifier.NotifyNewTicket(ticket);
        return ticket;
    }

    public async ValueTask<Ticket?> TryClaimTicketAsync(ulong ticketId, string mentorToken,
        CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketAsync(ticketId, cancellationToken);
        var mentor = await mentorCtx.GetMentorByTokenAsync(mentorToken, cancellationToken);

        if (ticket == null || mentor == null) return null;
        if (ticket.Status != TicketStatus.Requested) return ticket;

        ticket.Mentor = mentor;
        ticket.Status = TicketStatus.Responding;
        ticket.Claimed = DateTime.UtcNow;

        ctx.Update(ticket);
        await ctx.SaveChangesAsync(cancellationToken);
        notifier.NotifyUpdatedTicket(ticket);

        return ticket;
    }

    public async ValueTask<Ticket?> TryUnclaimTicketAsync(ulong ticketId, string mentorToken,
        CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketAsync(ticketId, cancellationToken);

        if (ticket == null || ticket.Mentor?.Token != mentorToken) return null;
        if (ticket.Status != TicketStatus.Responding) return ticket;

        ticket.Status = TicketStatus.Requested;
        ticket.Claimed = null;
        ticket.Mentor = null;

        ctx.Update(ticket);
        await ctx.SaveChangesAsync(cancellationToken);
        notifier.NotifyUpdatedTicket(ticket);

        return ticket;
    }

    public async ValueTask<Ticket?> TryCompleteTicketAsync(ulong ticketId, string mentorToken,
        CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketAsync(ticketId, cancellationToken);

        if (ticket == null || ticket.Mentor?.Token != mentorToken) return null;
        if (ticket.Status != TicketStatus.Responding) return ticket;

        ticket.Status = TicketStatus.Completed;
        ticket.Complete = DateTime.UtcNow;

        ctx.Update(ticket);
        await ctx.SaveChangesAsync(cancellationToken);
        notifier.NotifyUpdatedTicket(ticket);

        return ticket;
    }

    public async ValueTask<Ticket?> TryCancelTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketAsync(ticketId, cancellationToken);

        if (ticket == null) return null;
        if (ticket.Status.IsTerminal()) return ticket;

        ticket.Status = TicketStatus.Canceled;
        ticket.Canceled = DateTime.UtcNow;

        ctx.Update(ticket);
        await ctx.SaveChangesAsync(cancellationToken);
        notifier.NotifyUpdatedTicket(ticket);

        return ticket;
    }

    public async ValueTask<Ticket?> AssignDiscordIdAsync(ulong ticketId, ulong discordId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await GetTicketAsync(ticketId, cancellationToken);
        if (ticket == null) return null;

        ticket.DiscordId = discordId;
        ctx.Update(ticket);
        await ctx.SaveChangesAsync(cancellationToken);
        return ticket;
    }
}