using MentorBot.Models;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace MentorBot.Extern
{
  public class NeosApiOptions
  {
    public string VariableName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }

  public interface INeosApi
  {
    ValueTask<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    ValueTask SetCloudVarAuthTokenAsync(string token, string user, CancellationToken cancellationToken = default);
  }

  public class NeosApi : INeosApi
  {
    private readonly HttpClient _client;
    private readonly INeosApiAuthKeeper _authManager;
    private readonly IOptions<NeosApiOptions> _options;
    private readonly ILogger<NeosApi> _logger;

    public NeosApi(HttpClient client, INeosApiAuthKeeper authManager, IOptions<NeosApiOptions> options, ILogger<NeosApi> logger)
    {
      _client = client;
      _authManager = authManager;
      _options = options;
      _logger = logger;
    }

    public async ValueTask<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
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

    private class NeosUser
    {
      public string? Id { get; set; }
      public string? Username { get; set; }
    }

    public async ValueTask SetCloudVarAuthTokenAsync(string token, string user, CancellationToken cancellationToken)
    {
      var authToken = await _authManager.GetOrRefreshToken(RefreshToken, cancellationToken);
      if (string.IsNullOrWhiteSpace(authToken))
      {
        _logger.LogWarning("Failed to set ID for user.");
        throw new InvalidOperationException("Failed to set token cloud variable for user.");
      }
      var opts = _options.Value;
      _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authToken);

      var url = $"api/groups/{user}/vars/{opts.VariableName}";
      var resp = await _client.PutAsJsonAsync(url, new NeosSetCloudVar
      {
        OwnerId = user,
        Value = token,
      }, cancellationToken);
      resp.EnsureSuccessStatusCode();
    }

    private class NeosSetCloudVar
    {
      public string OwnerId { get; set; } = string.Empty;
      public string Value { get; set; } = string.Empty;
    }

    private async Task<(string token, DateTime expiry)> RefreshToken()
    {
      var opts = _options.Value;
      var response = await _client.PostAsJsonAsync($"api/userSessions", new LoginRequest
      {
        Username = opts.UserName,
        Password = opts.Password,
      });
      response.EnsureSuccessStatusCode();
      var resp = await response.Content.ReadFromJsonAsync<LoginResponse>();
      if (resp == null)
      {
        return ("", DateTime.MinValue);
      }
      return ($"neos {resp.UserId}:{resp.Token}", resp.Expire);
    }

    private class LoginRequest
    {
      public string Username { get; set; } = string.Empty;
      public string Password { get; set; } = string.Empty;
    }

    private class LoginResponse
    {
      public string? Token { get; set; }
      public DateTime Expire { get; set; }
      public string? UserId { get; set; }
    }
  }
}

namespace Microsoft.Extensions.DependencyInjection
{
  using MentorBot.Extern;
  using Microsoft.Extensions.Configuration;

  public static class NeosApiExtensions
  {
    public static IServiceCollection AddNeosHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
      services.AddHttpClient<INeosApi, NeosApi>(c =>
      {
        c.BaseAddress = new Uri("https://api.neos.com/");
        c.DefaultRequestHeaders.Add("User-Agent", "MentorBotService");
      });
      services.AddSingleton<INeosApiAuthKeeper, NeosApiAuthKeeper>();

      services.Configure<NeosApiOptions>(configuration.GetSection("neosApi"));

      return services;
    }
  }
}
