using Newtonsoft.Json;

namespace MentorBot.Models
{
  public record User
  {
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
  }
}
