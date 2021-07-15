using MentorBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System;
using MentorBot.ExternDiscord;

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
      services.AddSingleton<DiscordContext>();
      services.AddHostedService(o => o.GetRequiredService<DiscordContext>());
      services.AddTransient<IDiscordReactionHandler, DiscordReactionMentorHandler>();

      services.AddDbContext<TicketContext>(options =>
      {
        switch (Configuration.GetValue<string>("Provider"))
        {
          case "sqlite":
            options.UseSqlite(Configuration.GetConnectionString("TicketSqlLiteContext"));
            break;
          case "npgsql":
            options.UseNpgsql(Configuration.GetConnectionString("TicketNpgsqlContext"));
            break;
          default:
            throw new InvalidOperationException("No database provider selected.");
        }
      });

      services.AddTransient<TicketStore>();

      services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mentor Signal", Version = "v1" }));
      services.AddHealthChecks().AddDbContextCheck<TicketContext>();
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
