namespace MentorBot.ExternDiscord
{
  public class DiscordOptions
  {
    public string Token { get; set; } = string.Empty;
    public ulong Channel { get; set; }
    public string ClaimEmote { get; set; } = string.Empty;
    public string CompleteEmote { get; set; } = string.Empty;
  }
}
