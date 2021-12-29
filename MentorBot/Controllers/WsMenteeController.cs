using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController, Route("ws/mentee")]
  public class WsMenteeController : ControllerBase
  {
    private readonly ILogger<MenteeController> _logger;

    private readonly ITicketNotifier _notifier;
    private readonly IServiceProvider _provider;
    private readonly IOptions<JsonOptions> _jsonOpts;

    public WsMenteeController(ILogger<MenteeController> logger, ITicketNotifier notifier, IServiceProvider provider, IOptions<JsonOptions> jsonOpts)
    {
      _logger = logger;
      _notifier = notifier;
      _provider = provider;
      _jsonOpts = jsonOpts;
    }

    [HttpGet, Throttle(6, Name = "WS Ticket Create")]
    public async ValueTask<ActionResult<TicketDto>> CreateTicket([FromQuery] TicketCreate createArgs, [FromQuery(Name = "ticket")] ulong? ticketId)
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }

      ticketId = await _provider.WithScopedServiceAsync(async (ITicketContext ctx) =>
      {
        var ticket = ticketId == null ?
          await ctx.CreateTicketAsync(createArgs, HttpContext.RequestAborted) :
          await ctx.GetTicketAsync(ticketId.Value, HttpContext.RequestAborted);
        return ticket?.Id;
      });
      if (ticketId == null)
      {
        return BadRequest();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      await WatchTicket(ticketId.Value, ws, HttpContext.RequestAborted);

      return new EmptyResult();
    }

    [HttpGet("{ticketId}"), Throttle(3, Name = "WS Ticket Get")]
    public async ValueTask<ActionResult> WatchTicket(ulong ticketId)
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }

      var foundTicketId = await _provider.WithScopedServiceAsync(async (ITicketContext ctx) =>
      {
        var ticket = 
          await ctx.GetTicketAsync(ticketId, HttpContext.RequestAborted);
        return ticket?.Id;
      });
      if (foundTicketId == null)
      {
        return NotFound();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      await WatchTicket(ticketId, ws, HttpContext.RequestAborted);

      return new EmptyResult();
    }

    private async ValueTask WatchTicket(ulong ticketId, WebSocket ws, CancellationToken cancellationToken = default)
    {
      // This token represents if the ticket can continue to allow updates. If this is false we stop trying to change things.
      var stopToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      var subToken = stopToken.Token;

      var sender = ws.MessageSender<TicketDto>(_jsonOpts.Value.JsonSerializerOptions, cancellationToken);

      // This method is responsible for sending updates to listeners, and for watching when it should no longer send updates.
      async ValueTask SendTicket(Ticket tick)
      {
        await sender(tick.ToDto());

        if (tick.Status.IsTerminal())
        {
          stopToken.Cancel();
        }
      }

      using (_notifier.WatchTicketUpdated(ticketId, async t =>
      {
        try
        {
          await SendTicket(t);
        }
        catch (Exception e)
        {
          _logger.LogWarning(e, "Exception while reporting ticket update to websocket.");
        }
      }))
      {
        bool ticketDone = await _provider.WithScopedServiceAsync(async (ITicketContext ctx) =>
        {
          var ticket = await ctx.GetTicketAsync(ticketId, cancellationToken);
          if (ticket != null)
          {
            await SendTicket(ticket);
          }
          return ticket == null;
        });

        try
        {
          await foreach (var payload in ws.ReadMessages<MenteeRequest>(_jsonOpts.Value.JsonSerializerOptions, subToken))
          {
            await _provider.WithScopedServiceAsync(async (ITicketContext ctx) =>
            {
              switch (payload.Type)
              {
                case MenteeRequestKind.Cancel:
                  await ctx.TryCancelTicketAsync(ticketId, cancellationToken);
                  break;
              }
            });
          }
        }
        catch (OperationCanceledException)
        {
          // Ticket was a terminal state.
        }
      }

      // This is due to a bug with the Neos websocket handler, it won't recieve messages that arrive just before the socket is closed.
      // This uses the outer token as we only bail out of the outer handler closes as well.
      try
      {
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
      }
      catch (OperationCanceledException) { }
    }

    public class MenteeRequest
    {
      public MenteeRequestKind Type { get; init; }
    }

    public enum MenteeRequestKind
    {
      Unknown,
      Cancel
    }
  }
}
