using MentorBot.Discord;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class DiscordContextExtensions
{
    public static IServiceCollection AddDiscordContext(this IServiceCollection services, IConfiguration config)
    {
        var discordContext = config.GetSection("Discord");
        services.Configure<DiscordOptions>(discordContext);
        var options = discordContext.Get<DiscordOptions>();
        if (options?.UpdateTickets ?? false)
        {
            services.AddSingleton<DiscordContext>()
                .AddSingleton<IDiscordContext, DiscordContext>(o => o.GetRequiredService<DiscordContext>())
                .AddHostedService<DiscordHostedServiceProxy>()
                .AddHostedService<TicketDiscordProxyHost>()
                .AddTransient<ITicketDiscordProxy, TicketDiscordProxy>();

            services.AddHealthChecks().AddCheck<DiscordHealthCheck>("discord");
        }

        return services;
    }
}