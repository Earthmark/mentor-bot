using MentorBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class SignalContextExtensions
{
    public static IServiceCollection AddSignalContexts(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDbContext<ISignalContext, SignalContext>(o =>
                o.UseSqlServer(configuration.GetConnectionString("SqlDb")))
            .AddTransient<ITicketContext, TicketContext>()
            .AddTransient<IMentorContext, MentorContext>();
    }

    public static IHealthChecksBuilder AddSignalHealthChecks(this IHealthChecksBuilder builder)
    {
        return builder.AddDbContextCheck<SignalContext>();
    }
}