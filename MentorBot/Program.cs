using MentorBot.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace MentorBot
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      using var host = CreateHostBuilder(args).Build();
      await using (var scope = host.Services.CreateAsyncScope())
      {
        var context = scope.ServiceProvider.GetRequiredService<TicketContext>();
        await context.Database.EnsureCreatedAsync();
      }
      await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
      .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
  }
}
