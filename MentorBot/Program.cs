using MentorBot.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace MentorBot
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      using var host = CreateHostBuilder(args).Build();
      await using(var scope = host.Services.CreateAsyncScope())
      {
        var client = scope.ServiceProvider.GetRequiredService<CosmosClient>();
        var opts = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<CosmosOptions>>();
        await CosmosDbCreator.EnsureCreated(client, opts);
      }
      await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
      .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
  }
}
