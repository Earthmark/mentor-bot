using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentorBot.Models
{
  [Owned]
  public class Mentor
  {
    [JsonIgnore]
    public long _DiscordId { get; set; }

    [JsonProperty("id"), NotMapped]
    public ulong DiscordId
    {
      get => unchecked((ulong)_DiscordId);
      set => _DiscordId = unchecked((long) value);
    }
    [JsonProperty("neosId")]
    public string NeosId { get; set; } = string.Empty;
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
  }

}
