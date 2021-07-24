using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

    private readonly ITicketStore _store;
    private readonly ITicketNotifier _notifier;

    public MenteeController(ILogger<MenteeController> logger, ITicketStore store, ITicketNotifier notifier)
    {
      _logger = logger;
      _store = store;
      _notifier = notifier;
    }

    [HttpPost("mentee"), Throttle(6, Name = "Ticket Create")]
    public async ValueTask<ActionResult<Ticket>> Create([FromQuery] TicketCreate createArgs)
    {
      var ticket = await _store.CreateTicket(createArgs, HttpContext.RequestAborted);
      if (ticket == null)
      {
        return NotFound();
      }
      return ticket;
    }

    [HttpGet("mentee/{ticketId}"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask<ActionResult<Ticket>> Get(ulong ticketId)
    {
      var ticket = await _store.GetTicketAsync(ticketId.ToString(), HttpContext.RequestAborted);
      if (ticket == null)
      {
        return NotFound();
      }
      return ticket;
    }

    [HttpGet("ws/mentee"), Throttle(6, Name = "Ticket Create")]
    public async ValueTask<ActionResult<Ticket>> CreateTicket([FromQuery] TicketCreate createArgs)
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }
      var ticket = await _store.CreateTicket(createArgs, HttpContext.RequestAborted);
      if (ticket == null)
      {
        return BadRequest();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      await WatchTicket(ticket, ws, HttpContext.RequestAborted);

      return new EmptyResult();
    }

    [HttpGet("ws/mentee/{ticketId}"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask<ActionResult> Retrieve(string ticketId)
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

    [HttpGet("ws/mentor"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask<ActionResult> MentorWatcher()
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      byte[] msg = Encoding.UTF8.GetBytes("Ping");

      using var _ = _notifier.WatchTicketAdded(ticket =>
      {
        ws.SendAsync(msg, WebSocketMessageType.Text, true, HttpContext.RequestAborted);
      });

      var buffer = new byte[1024 * 4];
      WebSocketReceiveResult result;
      do
      {
        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), HttpContext.RequestAborted);
      } while (result.MessageType != WebSocketMessageType.Close);

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
        var objTicket = JObject.FromObject(tick);
        var fields = Enumerable.Select<KeyValuePair<string, JToken>, KeyValuePair<string, string>>(objTicket,
          kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString()));
        var body = await new FormUrlEncodedContent(fields!).ReadAsByteArrayAsync(subToken);

        await ws.SendAsync(body, WebSocketMessageType.Text, true, subToken);

        if (ticket.Status.IsTerminal())
        {
          stopToken.Cancel();
        }
      }

      using var _ = _notifier.WatchTicketUpdated(ticket, async t => {
        try
        {
          await SendTicket(t);
        }
        catch(Exception e)
        {
          _logger.LogWarning(e, "Exception while reporting ticket update to websocket.");
        }
      });

      await SendTicket(ticket);

      try
      {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result;
        do
        {
          result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), subToken);
        } while (result.MessageType != WebSocketMessageType.Close && !subToken.IsCancellationRequested);
      }
      catch (OperationCanceledException)
      {
        // Ticket was a terminal state.
      }

      // This is due to a bug with the Neos websocket handler, it won't recieve messages that arrive just before the socket is closed.
      // This uses the outer token as we only bail out of the outer handler closes as well.
      await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }
  }
}
