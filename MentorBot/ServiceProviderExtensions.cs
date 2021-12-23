using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace MentorBot
{
  public static class ServiceProviderExtensions
  {
    public static async ValueTask WithScopedServiceAsync<TService>(this IServiceProvider provider, Func<TService, ValueTask> body) where TService : notnull
    {
      using var scope = provider.CreateScope();
      var service = scope.ServiceProvider.GetRequiredService<TService>();
      await body(service);
    }

    public static async ValueTask<TResult> WithScopedServiceAsync<TService, TResult>(this IServiceProvider provider, Func<TService, ValueTask<TResult>> body) where TService : notnull
    {
      using var scope = provider.CreateScope();
      var service = scope.ServiceProvider.GetRequiredService<TService>();
      return await body(service);
    }

    public static void WithScopedService<TService>(this IServiceProvider provider, Action<TService> body) where TService : notnull
    {
      using var scope = provider.CreateScope();
      var service = scope.ServiceProvider.GetRequiredService<TService>();
      body(service);
    }
  }
}
