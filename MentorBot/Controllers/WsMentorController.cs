using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController, Route("ws/mentor")]
  public class WsMentorController : ControllerBase
  {
    private readonly ITicketNotifier _notifier;
    private readonly IServiceProvider _provider;

    public WsMentorController(ITicketNotifier notifier, IServiceProvider provider)
    {
      _notifier = notifier;
      _provider = provider;
    }

    [HttpGet("{mentorToken}"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask<ActionResult> MentorWatcher([FromRoute] string mentorToken)
    {
      using(var scope = _provider.CreateScope())
      {
        var ctx = scope.ServiceProvider.GetRequiredService<IMentorContext>();
        var mentor = await ctx.GetMentorByTokenAsync(mentorToken, HttpContext.RequestAborted);
        if (mentor == null)
        {
          return Unauthorized();
        }
      }

      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      byte[] msg = Encoding.UTF8.GetBytes("Ping");

      var sender = ws.MessageSender<MentorTicketDto>(HttpContext.RequestAborted);

      using (_notifier.WatchTicketAdded(ticket => sender(ticket.ToMentorDto())))
      {
        try
        {
          await foreach (var payload in ws.ReadMessages<MentorRequest>(HttpContext.RequestAborted))
          {
            using var scope = _provider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ITicketContext>();

            var newTicket = payload.Type switch
            {
              MentorRequestKind.Claim => await ctx.TryClaimTicketAsync(payload.Ticket, mentorToken, HttpContext.RequestAborted),
              MentorRequestKind.Unclaim => await ctx.TryUnclaimTicketAsync(payload.Ticket, mentorToken, HttpContext.RequestAborted),
              MentorRequestKind.Complete => await ctx.TryCompleteTicketAsync(payload.Ticket, mentorToken, HttpContext.RequestAborted),
              _ => null,
            };
            if (newTicket != null)
            {
              await sender(newTicket.ToMentorDto());
            }
          }
        }
        catch (OperationCanceledException)
        {
          // Ticket was a terminal state.
        }

        return new EmptyResult();
      }
    }

    public class MentorRequest
    {
      [JsonConverter(typeof(StringEnumConverter))]
      public MentorRequestKind Type { get; set; }
      public ulong Ticket { get; set; }
    }

    public enum MentorRequestKind
    {
      Unknown,
      Claim,
      Unclaim,
      Complete
    }
  }
}
