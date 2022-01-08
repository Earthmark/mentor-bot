using Microsoft.AspNetCore.TestHost;
using System.Threading.Tasks;
using Xunit;

namespace MentorBot.Tests.Integration
{
  /// <summary>
  /// This ensures the mentor signal panels are compatible with the service,
  /// be careful when changing these tests.
  /// </summary>
  [Collection("Integration collection")]
  public class MenteeNeosLegacyJsonFormatTests : IClassFixture<SignalWebApplicationFactory>
  {
    private readonly WebSocketClient _wsClient;

    public MenteeNeosLegacyJsonFormatTests(SignalWebApplicationFactory factory)
    {
      _wsClient = factory.Server.CreateWebSocketClient();
    }

    [Fact]
    public async Task CreateAndCancelTicketSequence()
    {
      string ticket;
      using (var mentee = await _wsClient.BindUser("/ws/mentee?userId=U-User"))
      {
        var msg = await mentee.ReadAsync();
        ticket = Assert.Contains("ticket", msg);
        Assert.Equal("requested", Assert.Contains("status", msg));
      }

      using (var mentee = await _wsClient.BindUser($"/ws/mentee/{ticket}"))
      {
        var msg = await mentee.ReadAsync();
        Assert.Equal(ticket, Assert.Contains("ticket", msg));
        Assert.Equal("requested", Assert.Contains("status", msg));

        await mentee.SendAsync("type=cancel");
        var msg2 = await mentee.ReadAsync();
        Assert.Equal(ticket, Assert.Contains("ticket", msg2));
        Assert.Equal("canceled", Assert.Contains("status", msg2));
      }
    }

    [Fact]
    public async Task CreateClaimAndCancelTicketSequence()
    {
      using var mentor = await _wsClient.BindUser("/ws/mentor/MENTOR");

      string ticket;
      using (var mentee = await _wsClient.BindUser("/ws/mentee?userId=U-User"))
      {
        var msg = await mentee.ReadAsync();
        ticket = Assert.Contains("ticket", msg);
        Assert.Equal("requested", Assert.Contains("status", msg));
      }

      var mentorMsg = await mentor.ReadAsync(ticket);

      using (var mentee = await _wsClient.BindUser($"/ws/mentee/{ticket}"))
      {
        var msg = await mentee.ReadAsync();
        Assert.Equal(ticket, Assert.Contains("ticket", msg));
        Assert.Equal("requested", Assert.Contains("status", msg));

        await mentor.SendAsync($"ticket={ticket}&type=claim");

        var msg2 = await mentee.ReadAsync();
        Assert.Equal(ticket, Assert.Contains("ticket", msg2));
        Assert.Equal("responding", Assert.Contains("status", msg2));
        Assert.Equal("Mentor", Assert.Contains("mentor", msg2));

        await mentee.SendAsync("type=cancel");
        var msg3 = await mentee.ReadAsync();
        Assert.Equal(ticket, Assert.Contains("ticket", msg3));
        Assert.Equal("canceled", Assert.Contains("status", msg3));
      }
    }

    [Fact]
    public async Task CreateClaimAndCompleteTicketSequence()
    {
      using var mentor = await _wsClient.BindUser("/ws/mentor/MENTOR");

      string ticket;
      using (var mentee = await _wsClient.BindUser("/ws/mentee?userId=U-User"))
      {
        var msg = await mentee.ReadAsync();
        ticket = Assert.Contains("ticket", msg);
        Assert.Equal("requested", Assert.Contains("status", msg));
      }

      using (var mentee = await _wsClient.BindUser($"/ws/mentee/{ticket}"))
      {
        var msg = await mentee.ReadAsync();
        Assert.Equal(2, msg.Count);
        Assert.Equal(ticket, Assert.Contains("ticket", msg));
        Assert.Equal("requested", Assert.Contains("status", msg));

        await mentor.SendAsync($"ticket={ticket}&type=claim");

        var msg2 = await mentee.ReadAsync();
        Assert.Equal(3, msg2.Count);
        Assert.Equal(ticket, Assert.Contains("ticket", msg2));
        Assert.Equal("responding", Assert.Contains("status", msg2));
        Assert.Equal("Mentor", Assert.Contains("mentor", msg2));

        await mentor.SendAsync($"ticket={ticket}&type=complete");

        var msg3 = await mentee.ReadAsync();
        Assert.Equal(3, msg3.Count);
        Assert.Equal(ticket, Assert.Contains("ticket", msg3));
        Assert.Equal("completed", Assert.Contains("status", msg3));
        Assert.Equal("Mentor", Assert.Contains("mentor", msg3));
      }
    }
  }
}
