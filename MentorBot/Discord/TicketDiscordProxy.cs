using System.Threading;
using System.Threading.Tasks;
using MentorBot.Models;

namespace MentorBot.Discord;

public interface ITicketDiscordProxy
{
    ValueTask RectifyTicket(Ticket ticket, CancellationToken cancellationToken = default);
}

public class TicketDiscordProxy(IDiscordContext discCtx, ITicketContext tickCtx) : ITicketDiscordProxy
{
    public async ValueTask RectifyTicket(Ticket ticket, CancellationToken cancellationToken = default)
    {
        if (ticket.DiscordId != null)
        {
            await discCtx.UpdateMessageAsync(ticket.DiscordId.Value, ticket.ToEmbed(), cancellationToken);
        }
        else
        {
            var msg = await discCtx.SendMessageAsync(ticket.ToEmbed(), cancellationToken);
            if (msg == null) return;
            ticket.DiscordId = msg.Id;
            await tickCtx.AssignDiscordIdAsync(ticket.Id, msg.Id, cancellationToken);
        }
    }
}