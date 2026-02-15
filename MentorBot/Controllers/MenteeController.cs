using System.Threading.Tasks;
using MentorBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace MentorBot.Controllers;

[ApiController]
[Route("mentee")]
public class MenteeController(ITicketContext store) : ControllerBase
{
    [HttpPost]
    public async ValueTask<ActionResult<TicketDto>> Create([FromQuery] TicketCreate createArgs)
    {
        var ticket = await store.CreateTicketAsync(createArgs, HttpContext.RequestAborted);
        if (ticket == null) return NotFound();

        return ticket.ToDto();
    }

    [HttpGet("{ticketId}")]
    public async ValueTask<ActionResult<TicketDto>> Get(ulong ticketId)
    {
        var ticket = await store.GetTicketAsync(ticketId, HttpContext.RequestAborted);
        if (ticket == null) return NotFound();

        return ticket.ToDto();
    }
}