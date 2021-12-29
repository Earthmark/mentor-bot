using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController, Route("mentor")]
  public class MentorController : ControllerBase
  {
    private readonly IMentorContext _ctx;
    private readonly IOptionsSnapshot<MentorOptions> _config;

    public MentorController(IMentorContext ctx, IOptionsSnapshot<MentorOptions> config)
    {
      _ctx = ctx;
      _config = config;
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

    private bool HasTokenAccess(string comparand)
    {
      return !string.IsNullOrWhiteSpace(_config.Value.ModifyMentorsToken) &&
        _config.Value.ModifyMentorsToken == comparand;
    }

    [HttpPost("authorize", Name = "AuthorizeMentor")]
    public async ValueTask<ActionResult<MentorDto?>> AuthorizeMentor([FromForm]string neosId, [FromForm, DataType(DataType.Password)] string accessToken)
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

    [HttpPost("unauthorize", Name = "UnauthorizeMentor")]
    public async ValueTask<ActionResult<MentorDto?>> UnauthorizeMentor([FromForm] string neosId, [FromForm, DataType(DataType.Password)] string accessToken)
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
  }
}
