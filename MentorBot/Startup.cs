using MentorBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MentorBot.Extern;
using Microsoft.Azure.Cosmos;
using System;

namespace MentorBot
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
      services.Configure<DiscordOptions>(Configuration.GetSection("Discord"));
      services.Configure<CosmosOptions>(Configuration.GetSection("Cosmos"));

      services.AddSingleton<DiscordContext>();
      services.AddSingleton<IDiscordContext, DiscordContext>(o => o.GetRequiredService<DiscordContext>());
      services.AddHostedService(o => o.GetRequiredService<DiscordContext>());

      services.AddHostedService<ReactionProcessor>();

      services.AddHttpClient<INeosApi, NeosApi>(c =>
      {
        c.BaseAddress = new Uri("https://api.neos.com/");
        c.DefaultRequestHeaders.Add("User-Agent", "MentorBotService");
      });

      services.AddSingleton(options => new CosmosClient(Configuration.GetConnectionString("Cosmos")));

      services.AddTransient<ITicketContext, TicketContext>();
      services.AddTransient<ITicketStore, TicketStore>();
      services.AddTransient<IDiscordReactionHandler, TicketStore>();
      services.AddTransient<IMentorContext, MentorContext>();

      services.AddSingleton<ITicketNotifier, TicketNotifier>();

      services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mentor Signal", Version = "v1" }));
      services.AddHealthChecks().AddCheck<CosmosHealthCheck>("database");
      services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mentor Signal v1"));
      }

      if (!env.IsDevelopment())
      {
        app.UseHttpsRedirection();
      }

      app.UseRouting();
      app.UseWebSockets();
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapHealthChecks("/health");
        endpoints.MapControllers();
      });
    }
  }
}
