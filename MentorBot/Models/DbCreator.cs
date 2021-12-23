using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MentorBot.Models
{
  public static class DbCreator
  {
    public static void EnsureDatabaseCreated(this IHost host)
    {
      using var scope = host.Services.CreateScope();
      scope.ServiceProvider.GetRequiredService<SignalContext>()
        .Database.EnsureCreated();
    }
  }
}
