using MentorBot.Discord;
using MentorBot.Extern;
using MentorBot.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;

namespace MentorBot.Tests.Integration
{
  public class SignalWebApplicationFactory : WebApplicationFactory<Program>
  {
    public Mock<INeosApi> NeosApi { get; } = new();
    public Mock<IDiscordContext> Discord { get; } = new();
    public Mock<ITokenGenerator> TokenGen { get; } = new();

    public SignalContext CreateDbContext() =>
      new(Services.GetRequiredService<DbContextOptions<SignalContext>>());

    public SignalWebApplicationFactory()
    {
      TokenGen.Setup(t => t.CreateToken()).Returns("TOKEN");
      AddNeosUser("U-User", "User");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      base.ConfigureWebHost(builder);
      builder.ConfigureServices(services =>
      {
        // Prevent discord from spinning up.
        Remove<IHostedService, DiscordHostedServiceProxy>(services);

        Replace(services, new DbContextOptionsBuilder<SignalContext>()
          .UseSqlite("Filename=integration.db").Options);

        services.AddDbContext<SignalContext>();

        Replace(services, NeosApi.Object);
        Replace(services, Discord.Object);
        Replace(services, TokenGen.Object);

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<SignalContext>();
        var logger = scopedServices
            .GetRequiredService<ILogger<SignalWebApplicationFactory>>();

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        try
        {
          db.Add(new Mentor
          {
            NeosId = "U-Mentor",
            Name = "Mentor",
            Token = "MENTOR"
          });
          db.SaveChangesAsync().Wait();
        }
        catch (Exception ex)
        {
          logger.LogError(ex, "An error occurred seeding the " +
              "database with test messages. Error: {Message}", ex.Message);
        }
      });
    }

    private static void Remove<T>(IServiceCollection services)
    {
      var descriptor = services.SingleOrDefault(
          d => d.ServiceType ==
              typeof(T));
      services.Remove(descriptor);
    }

    private static void Remove<TInterface, TImplementation>(IServiceCollection services)
    {
      var descriptor = services.Single(
          d => d.ServiceType ==
              typeof(TInterface) && d.ImplementationType == typeof(TImplementation));
      services.Remove(descriptor);
    }

    private static void Replace<T>(IServiceCollection services, T instance) where T : class
    {
      Remove<T>(services);
      services.AddSingleton(instance);
    }

    public void AddNeosUser(string userId, string userName)
    {
      NeosApi.Setup(a => a.GetUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new User()
      {
        Id = userId,
        Name = userName
      });
    }
  }
}
