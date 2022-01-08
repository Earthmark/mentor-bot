using Microsoft.AspNetCore.TestHost;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MentorBot.Tests.Integration
{
  [Collection("Integration collection")]
  public class MentorJsonFormatTests : IClassFixture<SignalWebApplicationFactory>
  {
    private readonly WebSocketClient _wsClient;

    public MentorJsonFormatTests(SignalWebApplicationFactory factory)
    {
      _wsClient = factory.Server.CreateWebSocketClient();
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
      Assert.Equal(ticket, Assert.Contains("ticket", mentorMsg));
      Assert.Equal("requested", Assert.Contains("status", mentorMsg));
      Assert.True(DateTime.TryParse(Assert.Contains("created", mentorMsg), out var _));
      Assert.Equal("U-User", Assert.Contains("userId", mentorMsg));
      Assert.Equal("User", Assert.Contains("userName", mentorMsg));

      using (var mentee = await _wsClient.BindUser($"/ws/mentee/{ticket}"))
      {
        var msg = await mentee.ReadAsync();
        Assert.Equal(ticket, Assert.Contains("ticket", msg));
        Assert.Equal("requested", Assert.Contains("status", msg));

        await mentor.SendAsync($"ticket={ticket}&type=claim");
        var mentorMsg2 = await mentor.ReadAsync(ticket);
        Assert.Equal(ticket, Assert.Contains("ticket", mentorMsg2));
        Assert.Equal("responding", Assert.Contains("status", mentorMsg2));
        Assert.True(DateTime.TryParse(Assert.Contains("created", mentorMsg2), out var _));
        Assert.Equal("U-User", Assert.Contains("userId", mentorMsg2));
        Assert.Equal("User", Assert.Contains("userName", mentorMsg2));

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

      var mentorMsg = await mentor.ReadAsync(ticket);
      Assert.Equal(5, mentorMsg.Count);
      Assert.Equal(ticket, Assert.Contains("ticket", mentorMsg));
      Assert.Equal("requested", Assert.Contains("status", mentorMsg));
      Assert.True(DateTime.TryParse(Assert.Contains("created", mentorMsg), out var _));
      Assert.Equal("U-User", Assert.Contains("userId", mentorMsg));
      Assert.Equal("User", Assert.Contains("userName", mentorMsg));

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
