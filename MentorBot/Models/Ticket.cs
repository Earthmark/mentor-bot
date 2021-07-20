using Discord;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MentorBot.Models
{
  public class TicketCreate
  {
    [FromQuery]
    public string Username { get; init; } = string.Empty;
    [FromQuery]
    public string UserId { get; init; } = string.Empty;
    [FromQuery]
    public string Lang { get; init; } = string.Empty;
    [FromQuery]
    public string Desc { get; init; } = string.Empty;
    [FromQuery]
    public string Session { get; init; } = string.Empty;
    [FromQuery]
    public string SessionId { get; init; } = string.Empty;
    [FromQuery]
    public string SessionUrl { get; init; } = string.Empty;
    [FromQuery]
    public string SessionWebUrl { get; init; } = string.Empty;

    public Ticket ToTicket()
    {
      return new Ticket
      {
        Username = Username,
        UserId = UserId,
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
  public class Ticket
  {
    [JsonProperty("status"), JsonConverter(typeof(StringEnumConverter))]
    public TicketStatus Status { get; set; } = TicketStatus.Requested;

    [JsonProperty("menteeName")]
    public string Username { get; init; } = string.Empty;
    [JsonProperty("menteeId")]
    public string UserId { get; init; } = string.Empty;
    [JsonProperty("language")]
    public string Lang { get; init; } = string.Empty;
    [JsonProperty("description")]
    public string Desc { get; init; } = string.Empty;
    [JsonProperty("sessionName")]
    public string Session { get; init; } = string.Empty;
    [JsonProperty("sessionId")]
    public string SessionId { get; init; } = string.Empty;
    [JsonProperty("sessionUrl")]
    public string SessionUrl { get; init; } = string.Empty;
    [JsonProperty("sessionWebUrl")]
    public string SessionWebUrl { get; init; } = string.Empty;

    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("mentorName")]
    public string? MentorName { get; set; } = null;
    [JsonProperty("mentorDiscordId")]
    public string? MentorDiscordId { get; set; } = null;
    [JsonProperty("mentorNeosId")]
    public string? MentorNeosId { get; set; } = null;

    [JsonProperty("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;
    [JsonProperty("claimed")]
    public DateTime? Claimed { get; set; } = null;
    [JsonProperty("complete")]
    public DateTime? Complete { get; set; } = null;
    [JsonProperty("canceled")]
    public DateTime? Canceled { get; set; } = null;

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
        Fields = Fields().Where(f => f != null).ToList(),
      }.Build();
    }

    private IEnumerable<EmbedFieldBuilder?> Fields()
    {
      static EmbedFieldBuilder? Field(string name, object? value, bool inline = false)
      {
        return value != null ? new EmbedFieldBuilder
        {
          Name = name,
          Value = value,
          IsInline = inline,
        } : null;
      }

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
      yield return Field("Created", Created);
      yield return Field("Claimed", Claimed);
      yield return Field("Completed", Complete);
      yield return Field("Canceled", Canceled);
    }
  }

  public enum TicketStatus
  {
    Requested,
    Responding,
    Completed,
    Canceled
  }

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter)]
  public class TicketCreateBindAttribute : BindAttribute
  {
    public TicketCreateBindAttribute() : base(
      nameof(Ticket.Username), nameof(Ticket.UserId), nameof(Ticket.Lang),
      nameof(Ticket.Desc), nameof(Ticket.Session), nameof(Ticket.SessionId),
      nameof(Ticket.SessionUrl), nameof(Ticket.SessionWebUrl))
    {
    }
  }
}
