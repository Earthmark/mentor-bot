using Discord;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace MentorBot.Models
{
  public record TicketCreate
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

  public class Ticket
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
    public long _Id { get; set; }

    [NotMapped]
    public ulong Id
    {
      get => unchecked((ulong)_Id);
      set => _Id = unchecked((long)value);
    }

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
        yield return Field("User", User.Name, true);
        yield return Field("User Neos Id", User.Id, true);
        yield return Field("Language", Lang);
        yield return Field("Description", Desc);
        yield return Field("Session", Session);
        yield return Field("Session ID", SessionId);
        yield return Field("Session Url", SessionUrl);
        yield return Field("Session Web Url", SessionWebUrl);
        yield return Field("Mentor Name", Mentor?.Name, true);
        yield return Field("Mentor Discord Link", Mentor?.DiscordId.ToString(), true);
        //yield return Field("Mentor Neos Id",
        //  !string.IsNullOrWhiteSpace(Mentor?.Name) && string.IsNullOrWhiteSpace(Mentor.NeosId) ?
        //  "<UNREGISTERED>" :
        //  Mentor?.NeosId, true);
        yield return Field("Created", Created.ToString("u"));
        yield return Field("Claimed", Claimed?.ToString("u"));
        yield return Field("Completed", Complete?.ToString("u"));
        yield return Field("Canceled", Canceled?.ToString("u"));
      }
      return NullableFields().OnlyNotNull();
    }



    public ApiSafeTicket ApiProtectedFields()
    {
      return new ApiSafeTicket
      {
        Id = Id,
        MentorName = Mentor?.Name,
        Status = Status
      };
    }
  }

  public record ApiSafeTicket{

    [JsonProperty("id")]
    public ulong Id { get; init; }
    [JsonProperty("mentorName")]
    public string? MentorName { get; init; }
    [JsonProperty("status"), JsonConverter(typeof(StringEnumConverter))]
    public TicketStatus Status { get; init; }
  }

  public enum TicketStatus
  {
    [EnumMember(Value = "requested")]
    Requested,
    [EnumMember(Value = "responding")]
    Responding,
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "canceled")]
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
