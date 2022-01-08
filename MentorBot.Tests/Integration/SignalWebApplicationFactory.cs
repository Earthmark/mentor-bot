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
        services.TryRemove<IHostedService, DiscordHostedServiceProxy>();

        services.Replace(new DbContextOptionsBuilder<SignalContext>()
          .UseSqlite("Filename=integration.db").Options);

        services.AddDbContext<SignalContext>();

        services.Replace(NeosApi.Object);
        services.Replace(Discord.Object);
        services.Replace(TokenGen.Object);

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

    public void AddNeosUser(string userId, string userName)
    {
      NeosApi.Setup(a => a.GetUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new User()
      {
        Id = userId,
        Name = userName
      });
    }
  }

  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection Remove<IService>(this IServiceCollection services)
    {
      var descriptor = services.Single(
          d => d.ServiceType ==
              typeof(IService));
      services.Remove(descriptor);
      return services;
    }
    public static IServiceCollection Remove<TInterface, TImplementation>(this IServiceCollection services)
    {
      var descriptor = services.Single(
          d => d.ServiceType ==
              typeof(TInterface) && d.ImplementationType == typeof(TImplementation));
      services.Remove(descriptor);
      return services;
    }
    public static IServiceCollection TryRemove<IService>(this IServiceCollection services)
    {
      var descriptor = services.SingleOrDefault(
          d => d.ServiceType ==
              typeof(IService));
      services.Remove(descriptor);
      return services;
    }
    public static IServiceCollection TryRemove<TInterface, TImplementation>(this IServiceCollection services)
    {
      var descriptor = services.SingleOrDefault(
          d => d.ServiceType ==
              typeof(TInterface) && d.ImplementationType == typeof(TImplementation));
      services.Remove(descriptor);
      return services;
    }
    public static IServiceCollection Replace<IService>(this IServiceCollection services, IService instance) where IService : class
    {
      services.TryRemove<IService>();
      services.AddSingleton(instance);
      return services;
    }
  }
}
