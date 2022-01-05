using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController, Route("login")]
  public class LoginController : ControllerBase
  {
    private readonly IOptionsSnapshot<MentorOptions> _config;

    public LoginController(IOptionsSnapshot<MentorOptions> config)
    {
      _config = config;
    }

    [HttpPost(Name = "Login")]
    public async ValueTask<IActionResult> Login([FromForm, DataType(DataType.Password)] string accessToken)
    {
      if (accessToken != _config.Value.ModifyMentorsToken)
      {
        return BadRequest();
      }
      await HttpContext.SignInAsync(
        new ClaimsPrincipal(
          new ClaimsIdentity(new[]
          {
            new Claim(ClaimTypes.Role, "lead"),
          }, "token")));
      return Redirect("/");
    }
  }
}
