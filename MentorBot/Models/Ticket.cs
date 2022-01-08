using Discord;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MentorBot.Models
{
  public class TicketCreate
  {
    [FromQuery]
    public string? UserId { get; init; }
    [FromQuery]
    public string? Lang { get; init; }
    [FromQuery]
    public string? Desc { get; init; }
    [FromQuery]
    public string? Session { get; init; }
    [FromQuery]
    public string? SessionId { get; init; }
    [FromQuery]
    public string? SessionUrl { get; init; }
    [FromQuery]
    public string? SessionWebUrl { get; init; }
  }

  public record Ticket
  {
    public TicketStatus Status { get; set; } = TicketStatus.Requested;
    public User User { get; set; } = new User();
    public string? Lang { get; set; }
    public string? Desc { get; set; }
    public string? Session { get; set; }
    public string? SessionId { get; set; }
    public string? SessionUrl { get; set; }
    public string? SessionWebUrl { get; set; }

    [Key]
    public ulong Id { get; set; }

    public ulong? DiscordId { get; set; }

    public Mentor? Mentor { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Claimed { get; set; }
    public DateTime? Complete { get; set; }
    public DateTime? Canceled { get; set; }

    public Ticket()
    {
    }

    public Ticket(TicketCreate createArgs, User user)
    {
      User = user;
      Lang = createArgs.Lang;
      Desc = createArgs.Desc;
      Session = createArgs.Session;
      SessionId = createArgs.SessionId;
      SessionUrl = createArgs.SessionUrl;
      SessionWebUrl = createArgs.SessionWebUrl;
    }

    private static string StatusToTitle(TicketStatus status)
    {
      return status switch
      {
        TicketStatus.Requested => "Mentor Requested",
        TicketStatus.Responding => "Mentor Responding",
        TicketStatus.Completed => "Request Completed",
        TicketStatus.Canceled => "Request Canceled",
        _ => throw new InvalidOperationException("No mapping of enum"),
      };
    }

    public Embed ToEmbed()
    {
      return new EmbedBuilder
      {
        Title = StatusToTitle(Status),
        Fields = EmbedFields().ToList(),
      }.Build();
    }

    private IEnumerable<EmbedFieldBuilder?> EmbedFields()
    {
      foreach(var (name, value, inline) in Fields())
      {
        yield return new EmbedFieldBuilder
        {
          Name = name,
          Value = value,
          IsInline = inline,
        };
      }
    }

    private static (string Name, string Value, bool Inline)? Field(string name, string? value, bool inline = false)
    {
      return !string.IsNullOrWhiteSpace(value) ? (name, value, inline) : null;
    }

    public IEnumerable<(string Name, string Value, bool Inline)> Fields()
    {
      IEnumerable<(string Name, string Value, bool Inline)?> NullableFields()
      {
        yield return Field("Ticket Number", Id.ToString());
        yield return Field("User", User.Name, true);
        yield return Field("User Neos Id", User.Id, true);
        yield return Field("Language", Lang);
        yield return Field("Description", Desc);
        yield return Field("Session", Session);
        yield return Field("Session ID", SessionId);
        yield return Field("Session Url", SessionUrl);
        yield return Field("Session Web Url", SessionWebUrl);
        yield return Field("Mentor Name", Mentor?.Name, true);
        yield return Field("Mentor Discord Link", Mentor?.DiscordId?.ToString(), true);
        yield return Field("Mentor Neos ID", Mentor?.NeosId.ToString(), true);
        yield return Field("Created", Created.ToDiscordTimecode("f"));
        yield return Field("Claimed", Claimed?.ToDiscordTimecode("f"));
        yield return Field("Completed", Complete?.ToDiscordTimecode("f"));
        yield return Field("Canceled", Canceled?.ToDiscordTimecode("f"));
      }
      return NullableFields().OnlyNotNull();
    }

    public TicketDto ToDto() => new(this);
    public MentorTicketDto ToMentorDto() => new(this);
  }

  public record TicketDto {
    protected readonly Ticket _ticket;
    public TicketDto(Ticket ticket)
    {
      _ticket = ticket;
    }

    public ulong Ticket => _ticket.Id;
    public string? Mentor => _ticket.Mentor?.Name;
    public TicketStatus Status => _ticket.Status;
  }

  public record MentorTicketDto : TicketDto
  {
    public MentorTicketDto(Ticket ticket) : base(ticket)
    {
    }

    public DateTime Created => _ticket.Created;
    public string? Lang => _ticket.Lang;
    public string? Desc => _ticket.Desc;
    public string? Session => _ticket.Session;
    public string? SessionId => _ticket.SessionId;
    public string? MentorId => _ticket.Mentor?.NeosId;
    public string? UserId => _ticket.User.Id;
    public string UserName => _ticket.User.Name;
  }

  public enum TicketStatus
  {
    Requested,
    Responding,
    Completed,
    Canceled
  }

  public static class TicketStatusExtensions
  {
    public static bool IsTerminal(this TicketStatus status)
    {
      return status switch
      {
        TicketStatus.Requested => false,
        TicketStatus.Responding => false,
        TicketStatus.Completed => true,
        TicketStatus.Canceled => true,
        _ => false,
      };
    }
  }
}
