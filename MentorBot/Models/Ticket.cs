using Discord;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MentorBot.Models
{
  public record TicketCreate
  {
    [FromQuery]
    public string? Name { get; init; }
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

    public Ticket Populate(Ticket ticket)
    {
      return ticket with
      {
        Lang = Lang,
        Desc = Desc,
        Session = Session,
        SessionId = SessionId,
        SessionUrl = SessionUrl,
        SessionWebUrl = SessionWebUrl,
      };
    }
  }

  [JsonObject]
  public record Ticket
  {
    [JsonProperty("status"), JsonConverter(typeof(StringEnumConverter))]
    public TicketStatus Status { get; set; } = TicketStatus.Requested;

    [JsonProperty("menteeName")]
    public string? Username { get; init; }
    [JsonProperty("menteeId")]
    public string UserId { get; init; } = string.Empty;
    [JsonProperty("language")]
    public string? Lang { get; init; }
    [JsonProperty("description")]
    public string? Desc { get; init; }
    [JsonProperty("sessionName")]
    public string? Session { get; init; }
    [JsonProperty("sessionId")]
    public string? SessionId { get; init; }
    [JsonProperty("sessionUrl")]
    public string? SessionUrl { get; init; }
    [JsonProperty("sessionWebUrl")]
    public string? SessionWebUrl { get; init; }

    [JsonProperty("id")]
    public string Id { get; init; } = string.Empty;

    [JsonProperty("mentorName")]
    public string? MentorName { get; init; } = null;
    [JsonProperty("mentorDiscordId")]
    public string? MentorDiscordId { get; init; } = null;
    [JsonProperty("mentorNeosId")]
    public string? MentorNeosId { get; init; } = null;

    [JsonProperty("created")]
    public DateTime Created { get; init; } = DateTime.UtcNow;
    [JsonProperty("claimed")]
    public DateTime? Claimed { get; init; } = null;
    [JsonProperty("complete")]
    public DateTime? Complete { get; init; } = null;
    [JsonProperty("canceled")]
    public DateTime? Canceled { get; init; } = null;

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
        yield return Field("User", Username, true);
        yield return Field("User Neos Id", UserId, true);
        yield return Field("Language", Lang);
        yield return Field("Description", Desc);
        yield return Field("Session", Session);
        yield return Field("Session ID", SessionId);
        yield return Field("Session Url", SessionUrl);
        yield return Field("Session Web Url", SessionWebUrl);
        yield return Field("Mentor Name", MentorName, true);
        yield return Field("Mentor Discord Link", MentorDiscordId, true);
        yield return Field("Mentor Neos Id", MentorNeosId, true);
        yield return Field("Created", Created.ToString("u"));
        yield return Field("Claimed", Claimed?.ToString("u"));
        yield return Field("Completed", Complete?.ToString("u"));
        yield return Field("Canceled", Canceled?.ToString("u"));
      }

      foreach(var item in NullableFields())
      {
        if (item.HasValue)
        {
          yield return item.Value;
        }
      }
    }
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
