using Microsoft.EntityFrameworkCore;

namespace MentorBot.Models
{
  public class SignalContext : DbContext
  {
    public DbSet<Ticket> Tickets { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SignalContext(DbContextOptions<SignalContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
      : base(options)
    {
    }
  }
}
