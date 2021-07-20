using MentorBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MentorBot.ExternDiscord;
using Microsoft.Azure.Cosmos;

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
      services.AddHostedService(o => o.GetRequiredService<DiscordContext>());
      services.AddTransient<IDiscordReactionHandler, TicketStore>();

      services.AddSingleton(options => new CosmosClient(Configuration.GetConnectionString("Cosmos")));

      services.AddTransient<TicketContext>();
      services.AddTransient<TicketStore>();
      services.AddTransient<MentorContext>();

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
