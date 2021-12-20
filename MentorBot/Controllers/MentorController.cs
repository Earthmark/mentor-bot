using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController]
  public class MentorController : ControllerBase
  {
    private readonly IMentorContext _ctx;
    private readonly IOptionsSnapshot<MentorOptions> _config;
    private readonly ITicketNotifier _notifier;

    public MentorController(IMentorContext ctx, IOptionsSnapshot<MentorOptions> config, ITicketNotifier notifier)
    {
      _ctx = ctx;
      _config = config;
      _notifier = notifier;
    }

    [HttpGet("mentor")]
    public IAsyncEnumerable<MentorDto> Get()
    {
      return _ctx.Mentors().Select(m => m.ToDto());
    }

    [HttpGet("mentor/{discordId}")]
    public async ValueTask<ActionResult<MentorDto?>> Get(string neosId)
    {
      var mentor = await _ctx.GetMentorAsync(neosId, HttpContext.RequestAborted);
      if (mentor != null)
      {
        return mentor.ToDto();
      }
      return NotFound();
    }

    private bool HasTokenAccess(string comparand)
    {
      return !string.IsNullOrWhiteSpace(_config.Value.ModifyMentorsToken) &&
        _config.Value.ModifyMentorsToken == comparand;
    }

    [HttpPost("mentor")]
    public async ValueTask<ActionResult<MentorDto?>> Post([FromQuery] string accessToken, [FromQuery] string neosId)
    {
      if (!HasTokenAccess(accessToken))
      {
        return Unauthorized();
      }

      var mentor = await _ctx.AddMentorAsync(neosId, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return NotFound();
      }

      return mentor.ToDto();
    }

    [HttpDelete("mentor/{discordId}")]
    public async ValueTask<ActionResult<MentorDto?>> RemoveAccess([FromQuery] string accessToken, [FromQuery] string neosId)
    {
      if (!HasTokenAccess(accessToken))
      {
        return Unauthorized();
      }

      var mentor = await _ctx.RemoveMentorAccess(neosId, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return NotFound();
      }

      return mentor.ToDto();
    }

    [HttpGet("ws/mentor/{mentorToken}"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask<ActionResult> MentorWatcher([FromRoute] string mentorToken)
    {
      var mentor = await _ctx.GetMentorByTokenAsync(mentorToken, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return Unauthorized();
      }

      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      byte[] msg = Encoding.UTF8.GetBytes("Ping");

      using (_notifier.WatchTicketAdded(ticket =>
        ws.SendAsync(msg, WebSocketMessageType.Text, true, HttpContext.RequestAborted)))
      {
        try
        {
          await foreach (var payload in ws.ReadMessages(HttpContext.RequestAborted))
          {
          }
        }
        catch (OperationCanceledException)
        {
          // Ticket was a terminal state.
        }

        return new EmptyResult();
      }
    }
  }
}
