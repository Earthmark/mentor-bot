using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController]
  public class MenteeController : ControllerBase
  {
    private readonly ILogger<MenteeController> _logger;

    private readonly ITicketContext _store;
    private readonly ITicketNotifier _notifier;

    public MenteeController(ILogger<MenteeController> logger, ITicketContext store, ITicketNotifier notifier)
    {
      _logger = logger;
      _store = store;
      _notifier = notifier;
    }

    [HttpPost("mentee"), Throttle(6, Name = "Ticket Create")]
    public async ValueTask<ActionResult<TicketDto>> Create([FromQuery] TicketCreate createArgs)
    {
      var ticket = await _store.CreateTicketAsync(createArgs, HttpContext.RequestAborted);
      if (ticket == null)
      {
        return NotFound();
      }

      return ticket.ToDto();
    }

    [HttpGet("mentee/{ticketId}"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask<ActionResult<TicketDto>> Get(ulong ticketId)
    {
      var ticket = await _store.GetTicketAsync(ticketId, HttpContext.RequestAborted);
      if (ticket == null)
      {
        return NotFound();
      }

      return ticket.ToDto();
    }

    [HttpGet("ws/mentee"), Throttle(6, Name = "WS Ticket Create")]
    public async ValueTask<ActionResult<TicketDto>> CreateTicket([FromQuery] TicketCreate createArgs, [FromQuery(Name = "ticket")] ulong? ticketId)
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }

      var ticket = ticketId == null ?
        await _store.CreateTicketAsync(createArgs, HttpContext.RequestAborted) :
        await _store.GetTicketAsync(ticketId.Value, HttpContext.RequestAborted);
      if (ticket == null)
      {
        return BadRequest();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      await WatchTicket(ticket, ws, HttpContext.RequestAborted);

      return new EmptyResult();
    }

    [HttpGet("ws/mentee/{ticketId}"), Throttle(3, Name = "WS Ticket Get")]
    public async ValueTask<ActionResult> Retrieve(ulong ticketId)
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }
      var ticket = await _store.GetTicketAsync(ticketId, HttpContext.RequestAborted);
      if (ticket == null)
      {
        return NotFound();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      await WatchTicket(ticket, ws, HttpContext.RequestAborted);

      return new EmptyResult();
    }

    private async ValueTask WatchTicket(Ticket ticket, WebSocket ws, CancellationToken cancellationToken = default)
    {
      // This token represents if the ticket can continue to allow updates. If this is false we stop trying to change things.
      var stopToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      var subToken = stopToken.Token;

      // This method is responsible for sending updates to listeners, and for watching when it should no longer send updates.
      async ValueTask SendTicket(Ticket tick)
      {
        var payload = UrlEncoder.Encode(tick.ToDto());
        var content = Encoding.UTF8.GetBytes(payload);

        await ws.SendAsync(content, WebSocketMessageType.Text, true, subToken);

        if (ticket.Status.IsTerminal())
        {
          stopToken.Cancel();
        }
      }

      using (_notifier.WatchTicketUpdated(ticket, async t =>
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
        await SendTicket(ticket);

        try
        {
          await foreach (var payload in ws.ReadMessages(subToken))
          {
            var body = UrlEncoder.Decode<MenteeRequest>(payload);
            if (body.Type == "cancel")
            {
              await _store.TryCancelTicketAsync(ticket.Id, cancellationToken);
            }
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

    public record MenteeRequest
    {
      public string Type { get; init; } = string.Empty;
    }
  }
}
