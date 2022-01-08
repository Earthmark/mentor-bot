using MentorBot.Extern;
using MentorBot.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MentorBot.Tests.Models
{
  public class MentorContextTests
  {
    private readonly TestSignalContext _ctx = new(nameof(MentorContextTests));
    private readonly Mock<INeosApi> _neosApi = new();
    private readonly Mock<ITokenGenerator> _tokenGenerator = new();

    protected DbContextOptions<SignalContext> ContextOptions { get; }

    public MentorContextTests()
    {
    }

    [Fact]
    public async Task SetsRemoteTokenOnAuthorize()
    {
      _neosApi.Setup(a => a.GetUserAsync("U-ID", It.IsAny<CancellationToken>())).ReturnsAsync(new User
      {
        Id = "U-ID",
        Name = "User"
      }).Verifiable();
      _neosApi.Setup(a => a.SetCloudVarAuthTokenAsync("MAGIC", "U-ID", It.IsAny<CancellationToken>()))
        .Returns(ValueTask.CompletedTask).Verifiable();
      _tokenGenerator.Setup(t => t.CreateToken()).Returns("MAGIC").Verifiable();

      using ISignalContext ctx = _ctx.CreateContext();
      IMentorContext mentorCtx = new MentorContext(ctx, _neosApi.Object, _tokenGenerator.Object);
      await mentorCtx.AddMentorAsync("U-ID");

      Assert.Equal(new Mentor
      {
        NeosId = "U-ID",
        Name = "User",
        Token = "MAGIC"
      }, await mentorCtx.GetMentorByNeosIdAsync("U-ID"));
      _neosApi.Verify();
      _tokenGenerator.Verify();
    }

    [Fact]
    public async Task ResetsRemoteTokenOnAuthorize()
    {
      using (ISignalContext init = _ctx.CreateContext())
      {
        init.Add(new Mentor
        {
          NeosId = "U-ID",
          Name = "User",
          Token = "NOT MAGIC",
          DiscordId = 1234567890
        });
        await init.SaveChangesAsync();
      }
      _neosApi.Setup(a => a.GetUserAsync("U-ID", It.IsAny<CancellationToken>())).ReturnsAsync(new User
      {
        Id = "U-ID",
        Name = "User"
      }).Verifiable();
      _neosApi.Setup(a => a.SetCloudVarAuthTokenAsync("MAGIC", "U-ID", It.IsAny<CancellationToken>()))
        .Returns(ValueTask.CompletedTask).Verifiable();
      _tokenGenerator.Setup(t => t.CreateToken()).Returns("MAGIC").Verifiable();

      using ISignalContext ctx = _ctx.CreateContext();
      IMentorContext mentorCtx = new MentorContext(ctx, _neosApi.Object, _tokenGenerator.Object);
      await mentorCtx.AddMentorAsync("U-ID");

      Assert.Equal(new Mentor
      {
        NeosId = "U-ID",
        Name = "User",
        Token = "MAGIC",
        DiscordId = 1234567890
      }, await mentorCtx.GetMentorByNeosIdAsync("U-ID"));
      _neosApi.Verify();
      _tokenGenerator.Verify();
    }


  }
}
