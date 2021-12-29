using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MentorBot.Controllers
{
  [ApiController, Route("ws/mentor")]
  public class WsMentorController : ControllerBase
  {
    private readonly ITicketNotifier _notifier;
    private readonly IServiceProvider _provider;
    private readonly IOptions<JsonOptions> _jsonOpts;

    public WsMentorController(ITicketNotifier notifier, IServiceProvider provider, IOptions<JsonOptions> jsonOpts)
    {
      _notifier = notifier;
      _provider = provider;
      _jsonOpts = jsonOpts;
    }

    [HttpGet("{mentorToken}"), Throttle(3, Name = "Ticket Get")]
    public async ValueTask<ActionResult> MentorWatcher([FromRoute] string mentorToken)
    {
      if (!HttpContext.WebSockets.IsWebSocketRequest)
      {
        return BadRequest();
      }

      if (await _provider.WithScopedServiceAsync(async (IMentorContext ctx) =>
        await ctx.GetMentorByTokenAsync(mentorToken, HttpContext.RequestAborted) == null))
      {
        return Unauthorized();
      }

      using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

      var sender = ws.MessageSender<MentorTicketDto>(_jsonOpts.Value.JsonSerializerOptions, HttpContext.RequestAborted);

      using (_notifier.WatchTicketsUpdated(ticket => sender(ticket.ToMentorDto())))
      {
        try
        {
          // Pulse all existing tickets
          await _provider.WithScopedServiceAsync(async (ITicketContext ctx) =>
          {
            await foreach(var ticket in ctx.GetIncompleteTickets().ToAsyncEnumerable())
            {
              await sender(ticket.ToMentorDto());
            }
          });

          await foreach (var payload in ws.ReadMessages<MentorRequest>(_jsonOpts.Value.JsonSerializerOptions, HttpContext.RequestAborted))
          {
            await _provider.WithScopedServiceAsync(async (ITicketContext ctx) =>
            {
              switch(payload.Type)
              {
                case MentorRequestKind.Claim:
                  await ctx.TryClaimTicketAsync(payload.Ticket, mentorToken, HttpContext.RequestAborted);
                  break;
                case MentorRequestKind.Unclaim:
                  await ctx.TryUnclaimTicketAsync(payload.Ticket, mentorToken, HttpContext.RequestAborted);
                  break;
                case MentorRequestKind.Complete:
                  await ctx.TryCompleteTicketAsync(payload.Ticket, mentorToken, HttpContext.RequestAborted);
                  break;
              };
            });
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
