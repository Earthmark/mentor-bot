using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public class SignalContext : DbContext
  {
    public DbSet<Ticket> Tickets { get; set; }

    public IQueryable<Ticket> MetaTickets => Tickets.Include(t => t.Mentor);

    public Task<Ticket?> GetTicketAsync(ulong ticketId, CancellationToken cancellationToken = default)
    {
      return MetaTickets.SingleOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
    }

    public DbSet<Mentor> Mentors { get; set; }

    public Task<Mentor?> GetMentorByTokenAsync(string mentorToken, CancellationToken cancellationToken = default)
    {
      return Mentors.SingleOrDefaultAsync(t => t.Token == mentorToken, cancellationToken);
    }

    public Task<Mentor?> GetMentorByNeosIdAsync(string neosId, CancellationToken cancellationToken = default)
    {
      return Mentors.SingleOrDefaultAsync(t => t.NeosId == neosId, cancellationToken);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SignalContext(DbContextOptions<SignalContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
      : base(options)
    {
    }
  }
}

namespace Microsoft.Extensions.DependencyInjection
{
  using MentorBot.Models;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.Extensions.Configuration;

  public static class SignalContextExtensions
  {
    public static IServiceCollection AddSignalContexts(this IServiceCollection services, IConfiguration configuration)
    {
      return services.AddDbContext<SignalContext>(o =>
        o.UseSqlServer(configuration.GetConnectionString("SqlDb")))
        .AddTransient<ITicketContext, TicketContext>()
        .AddTransient<IMentorContext, MentorContext>();
    }

    public static IHealthChecksBuilder AddSignalHealthChecks(this IHealthChecksBuilder builder)
    {
      return builder.AddDbContextCheck<SignalContext>();
    }
  }
}