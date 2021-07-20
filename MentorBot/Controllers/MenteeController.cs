using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController]
  public class MenteeController : ControllerBase
  {
    private readonly ILogger<MenteeController> _logger;

    private readonly TicketStore _store;

    public MenteeController(ILogger<MenteeController> logger, TicketStore store)
    {
      _logger = logger;
      _store = store;
    }

    [HttpPost("mentee"), Throttle(6, Name = "Ticket Create")]
    public async ValueTask<ActionResult<Ticket>> Create([TicketCreateBind] TicketCreate createArgs)
    {
      var ticket = await _store.CreateTicket(createArgs.ToTicket(), HttpContext.RequestAborted);
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
    public async ValueTask<ActionResult<Ticket>> CreateTicket([TicketCreateBind] TicketCreate createArgs)
    {
      if (HttpContext.WebSockets.IsWebSocketRequest)
      {
        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _logger.LogInformation("Websocket connection established.");
        return Ok();
      }
      else
      {
        return BadRequest();
      }
    }

    [HttpGet("ws/mentee/{ticketId}"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask Retrieve(ulong ticketId)
    {
      if (HttpContext.WebSockets.IsWebSocketRequest)
      {
        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _logger.LogInformation("Websocket connection established.");
      }
      else
      {
        HttpContext.Response.StatusCode = 400;
      }
    }
  }
}
