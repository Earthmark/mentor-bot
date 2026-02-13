using System;
using MentorBot.Extern;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ResoniteApiExtensions
{
    public static IServiceCollection AddResoniteHttpClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<IResoniteApi, ResoniteApi>(c =>
        {
            c.BaseAddress = new Uri("https://api.neos.com/");
            c.DefaultRequestHeaders.Add("User-Agent", "MentorBotService");
        });
        services.AddSingleton<IResoniteApiAuthKeeper, ResoniteApiAuthKeeper>();

        services.AddOptions<ResoniteApiOptions>().BindConfiguration("ResoniteApi");

        return services;
    }
}