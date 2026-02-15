using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MentorBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MentorBot.Controllers;

[ApiController]
[Route("mentor")]
public class MentorController(IMentorContext ctx, ITicketContext ticketCtx, IOptionsSnapshot<MentorOptions> options)
    : ControllerBase
{
    [HttpGet]
    public IAsyncEnumerable<MentorDto> Get()
    {
        return ctx.Mentors().Select(m => m.ToDto());
    }

    [HttpGet("{mentorToken}")]
    public async ValueTask<ActionResult<MentorDto?>> Get(string mentorToken)
    {
        var mentor = await ctx.GetMentorByTokenAsync(mentorToken, HttpContext.RequestAborted);
        if (mentor == null) return NotFound();

        return mentor.ToDto();
    }

    [HttpGet("{mentorToken}/tickets")]
    public async ValueTask<ActionResult<IAsyncEnumerable<MentorTicketDto>>> GetTicketsAsMentor(string mentorToken)
    {
        var mentor = await ctx.GetMentorByTokenAsync(mentorToken, HttpContext.RequestAborted);
        if (mentor == null) return NotFound();

        return Ok(ticketCtx.GetIncompleteTickets().Select(t => t.ToMentorDto()));
    }

    [HttpPost("authorize", Name = "AuthorizeMentor")]
    [Authorize]
    public async ValueTask<ActionResult<MentorDto?>> AuthorizeMentor([FromForm] string resoniteId)
    {
        if (string.IsNullOrEmpty(options.Value.ModifyMentorsToken)) return Forbid();

        var mentor = await ctx.AddMentorAsync(resoniteId, HttpContext.RequestAborted);
        if (mentor == null) return NotFound();

        return mentor.ToDto();
    }

    [HttpPost("unauthorize", Name = "UnauthorizeMentor")]
    [Authorize]
    public async ValueTask<ActionResult<MentorDto?>> UnauthorizeMentor([FromForm] string resoniteId)
    {
        if (string.IsNullOrEmpty(options.Value.ModifyMentorsToken)) return Forbid();

        var mentor = await ctx.RemoveMentorAccess(resoniteId, HttpContext.RequestAborted);
        if (mentor == null) return NotFound();

        return mentor.ToDto();
    }
}