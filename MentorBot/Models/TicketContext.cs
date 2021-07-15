using Microsoft.EntityFrameworkCore;

namespace MentorBot.Models
{
  public class TicketContext : DbContext
  {
#pragma warning disable CS8618
    public TicketContext(DbContextOptions<TicketContext> options)
#pragma warning restore CS8618
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; }
  }
}
