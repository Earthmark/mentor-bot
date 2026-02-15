using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MentorBot.Controllers;

[ApiController]
[Route("login")]
public class LoginController(IOptionsSnapshot<MentorOptions> config) : ControllerBase
{
    [HttpPost(Name = "Login")]
    public async ValueTask<IActionResult> Login([FromForm][DataType(DataType.Password)] string accessToken)
    {
        if (accessToken != config.Value.ModifyMentorsToken) return BadRequest();

        await HttpContext.SignInAsync(
            new ClaimsPrincipal(
                new ClaimsIdentity([
                    new Claim(ClaimTypes.Role, "lead")
                ], "token")));

        return Redirect("/");
    }
}