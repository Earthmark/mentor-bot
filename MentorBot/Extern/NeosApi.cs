using MentorBot.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace MentorBot.Extern
{
  public interface INeosApi
  {
    ValueTask<User?> GetUser(string userId, CancellationToken cancellationToken = default);
  }

  public class NeosApi : INeosApi
  {
    private readonly HttpClient _client;

    public NeosApi(HttpClient client)
    {
      _client = client;
    }

    public async ValueTask<User?> GetUser(string userId, CancellationToken cancellationToken = default)
    {
      try
      {
        var user = await _client.GetFromJsonAsync<NeosUser>($"api/users/{Uri.EscapeDataString(userId)}", cancellationToken);
        if (user != null && !string.IsNullOrWhiteSpace(user.Id) && !string.IsNullOrWhiteSpace(user.Username))
        {
          return new User
          {
            Id = user.Id,
            Name = user.Username
          };
        }
      }
      catch
      {
      }
      return null;
    }

    private record NeosUser
    {
      [JsonProperty("id")]
      [JsonPropertyName("id")]
      public string? Id { get; set; }

      [JsonProperty("username")]
      [JsonPropertyName("username")]
      public string? Username { get; set; }
    }
  }
}

namespace Microsoft.Extensions.DependencyInjection
{
  using MentorBot.Extern;
  public static class NeosApiExtensions
  {
    public static IHttpClientBuilder AddNeosHttpClient(this IServiceCollection services)
    {
      return services.AddHttpClient<INeosApi, NeosApi>(c =>
      {
        c.BaseAddress = new Uri("https://api.neos.com/");
        c.DefaultRequestHeaders.Add("User-Agent", "MentorBotService");
      });
    }
  }
}
