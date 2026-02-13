using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MentorBot.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentorBot.Extern;

public class ResoniteApiOptions
{
    public string VariableName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public interface IResoniteApi
{
    ValueTask<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    ValueTask SetCloudVarAuthTokenAsync(string token, string user, CancellationToken cancellationToken = default);
}

public class ResoniteApi(
    HttpClient client,
    IResoniteApiAuthKeeper authManager,
    IOptions<ResoniteApiOptions> options,
    ILogger<ResoniteApi> logger)
    : IResoniteApi
{
    public async ValueTask<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await client.GetFromJsonAsync<ResoniteUser>($"api/users/{Uri.EscapeDataString(userId)}",
                cancellationToken);
            if (user != null && !string.IsNullOrWhiteSpace(user.Id) && !string.IsNullOrWhiteSpace(user.Username))
                return new User
                {
                    Id = user.Id,
                    Name = user.Username
                };
        }
        catch
        {
        }

        return null;
    }

    public async ValueTask SetCloudVarAuthTokenAsync(string token, string user, CancellationToken cancellationToken)
    {
        var authToken = await authManager.GetOrRefreshToken(RefreshToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(authToken))
        {
            logger.LogWarning("Failed to set ID for user.");
            throw new InvalidOperationException("Failed to set token cloud variable for user.");
        }

        var opts = options.Value;
        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authToken);

        var url = $"api/groups/{user}/vars/{opts.VariableName}";
        var resp = await client.PutAsJsonAsync(url, new NeosSetCloudVar
        {
            OwnerId = user,
            Value = token
        }, cancellationToken);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<(string token, DateTime expiry)> RefreshToken()
    {
        var opts = options.Value;
        var response = await client.PostAsJsonAsync("api/userSessions", new LoginRequest
        {
            Username = opts.UserName,
            Password = opts.Password
        });
        response.EnsureSuccessStatusCode();
        var resp = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (resp == null) return ("", DateTime.MinValue);
        return ($"neos {resp.UserId}:{resp.Token}", resp.Expire);
    }

    private class ResoniteUser
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
    }

    private class NeosSetCloudVar
    {
        public string OwnerId { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
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