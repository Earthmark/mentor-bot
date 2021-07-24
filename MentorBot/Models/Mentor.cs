using Newtonsoft.Json;

namespace MentorBot.Models
{
  public class Mentor
  {
    [JsonProperty("id")]
    public string DiscordId { get; set; } = string.Empty;
    [JsonProperty("neosId")]
    public string NeosId { get; set; } = string.Empty;
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
  }

}
