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

    public MentorController(IMentorContext ctx)
    {
      _ctx = ctx;
    }

    [HttpGet]
    public IAsyncEnumerable<MentorDto> Get()
    {
      return _ctx.Mentors().Select(m => m.ToDto());
    }

    [HttpGet("{neosId}")]
    public async ValueTask<ActionResult<MentorDto?>> Get(string neosId)
    {
      var mentor = await _ctx.GetMentorByNeosIdAsync(neosId, HttpContext.RequestAborted);
      if (mentor == null)
      {
        return NotFound();
      }

      return mentor.ToDto();
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
