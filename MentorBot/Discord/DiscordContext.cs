using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Discord
{
  public class DiscordOptions
  {
    public bool UpdateTickets { get; set; } = true;
    public string Token { get; set; } = string.Empty;
    public ulong Channel { get; set; }
  }

  public interface IDiscordContext
  {
    bool ConnectedAndReady { get; }
    ValueTask<IUserMessage?> SendMessageAsync(Embed embed, CancellationToken cancellationToken = default);
    ValueTask<IUserMessage?> UpdateMessageAsync(ulong id, Embed embed, CancellationToken cancellationToken = default);
  }

  public class DiscordContext : IDiscordContext, IHostedService, IDisposable
  {
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordContext> _logger;

    public DiscordSocketClient Client { get; }
    public ITextChannel? Channel { get; private set; }
    public bool Ready { get; private set; }

    public bool ConnectedAndReady => Client.ConnectionState == ConnectionState.Connected && Ready;

    public DiscordContext(IOptions<DiscordOptions> options, ILogger<DiscordContext> logger)
    {
      Client = new DiscordSocketClient(new DiscordSocketConfig
      {
        MessageCacheSize = 50,
        LogLevel = LogSeverity.Debug
      });
      Client.Log += Client_Log;
      _options = options.Value;
      _logger = logger;
      Client.Ready += () => Task.FromResult(Ready = true);
    }

    private Task Client_Log(LogMessage arg)
    {
      switch (arg.Severity)
      {
        case LogSeverity.Critical:
          _logger.Log(LogLevel.Critical, arg.Exception, arg.Message);
          break;
        case LogSeverity.Error:
          _logger.Log(LogLevel.Error, arg.Exception, arg.Message);
          break;
        case LogSeverity.Warning:
          _logger.Log(LogLevel.Warning, arg.Exception, arg.Message);
          break;
        case LogSeverity.Info:
          _logger.Log(LogLevel.Information, arg.Exception, arg.Message);
          break;
        case LogSeverity.Verbose:
          _logger.Log(LogLevel.Trace, arg.Exception, arg.Message);
          break;
        case LogSeverity.Debug:
          _logger.Log(LogLevel.Debug, arg.Exception, arg.Message);
          break;
      }
      return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      await Client.LoginAsync(TokenType.Bot, _options.Token);
      await Client.StartAsync();
      Channel = await Client.Rest.GetChannelAsync(_options.Channel) as ITextChannel;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
      await Client.StopAsync();
      await Client.LogoutAsync();
    }

    public void Dispose()
    {
      Client.Dispose();
    }

    public async ValueTask<IUserMessage?> SendMessageAsync(Embed embed, CancellationToken cancellationToken = default)
    {
      if (Channel == null)
      {
        throw new InvalidOperationException("channel not bound to discord context.");
      }
      var msg = await Channel.SendMessageAsync(embed: embed, options: new RequestOptions
      {
        CancelToken = cancellationToken
      });
      return msg;
    }

    public async ValueTask<IUserMessage?> UpdateMessageAsync(ulong id, Embed embed, CancellationToken cancellationToken = default)
    {
      if (Channel == null)
      {
        throw new InvalidOperationException("channel not bound to discord context.");
      }
      return await Channel.ModifyMessageAsync(id, props => props.Embed = embed, new RequestOptions
      {
        CancelToken = cancellationToken
      });
    }
  }

  public class DiscordHostedServiceProxy : IHostedService
  {
    private readonly DiscordContext _context;

    public DiscordHostedServiceProxy(DiscordContext ctx)
    {
      _context = ctx;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      return _context.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return _context.StopAsync(cancellationToken);
    }
  }
}

namespace Microsoft.Extensions.DependencyInjection
{
  using MentorBot.Discord;
  using Microsoft.Extensions.Configuration;

  public static class DiscordContextExtensions
  {
    public static IServiceCollection AddDiscordContext(this IServiceCollection services, IConfiguration config)
    {
      var discordContext = config.GetSection("Discord");
      services.Configure<DiscordOptions>(discordContext);
      var options = discordContext.Get<DiscordOptions>();
      if (!string.IsNullOrWhiteSpace(options.Token))
      {
        services.AddSingleton<DiscordContext>()
          .AddSingleton<IDiscordContext, DiscordContext>(o => o.GetRequiredService<DiscordContext>())
          .AddHostedService<DiscordHostedServiceProxy>()
          .AddHostedService<TicketDiscordProxyHost>()
          .AddTransient<ITicketDiscordProxy, TicketDiscordProxy>();
      }

      return services;
    }

    public static IHealthChecksBuilder AddDiscordCheck(this IHealthChecksBuilder builder)
    {
      return builder.AddCheck<DiscordHealthCheck>("discord");
    }
  }
}
