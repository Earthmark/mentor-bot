using MentorBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController, Route("mentor")]
  public class MentorController : ControllerBase
  {
    private readonly IMentorContext _ctx;
    private readonly ITicketContext _ticketCtx;

    public MentorController(IMentorContext ctx, ITicketContext ticketCtx)
    {
      _ctx = ctx;
      _ticketCtx = ticketCtx;
    }

    [HttpGet]
    public IAsyncEnumerable<MentorDto> Get()
    {
      return _ctx.Mentors().Select(m => m.ToDto());
    }

    [HttpGet("{mentorToken}")]
    public async ValueTask<ActionResult<MentorDto?>> Get(string mentorToken)
    {
      var mentor = await _ctx.GetMentorByTokenAsync(mentorToken, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return NotFound();
      }

      return mentor.ToDto();
    }

    [HttpGet("{mentorToken}/tickets")]
    public async ValueTask<ActionResult<IAsyncEnumerable<MentorTicketDto>>> GetTicketsAsMentor(string mentorToken)
    {
      var mentor = await _ctx.GetMentorByTokenAsync(mentorToken, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return NotFound();
      }

      return Ok(_ticketCtx.GetIncompleteTickets().Select(t => t.ToMentorDto()));
    }

    [HttpPost("authorize", Name = "AuthorizeMentor"), Authorize]
    public async ValueTask<ActionResult<MentorDto?>> AuthorizeMentor([FromForm]string neosId)
    {
      var mentor = await _ctx.AddMentorAsync(neosId, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return NotFound();
      }

      return mentor.ToDto();
    }

    [HttpPost("unauthorize", Name = "UnauthorizeMentor"), Authorize]
    public async ValueTask<ActionResult<MentorDto?>> UnauthorizeMentor([FromForm] string neosId)
    {
      var mentor = await _ctx.RemoveMentorAccess(neosId, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return NotFound();
      }

      return mentor.ToDto();
    }
  }
}
