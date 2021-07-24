namespace MentorBot.Extern
{
  public class DiscordOptions
  {
    public bool UpdateTickets { get; set; } = true;
    public string Token { get; set; } = string.Empty;
    public ulong Channel { get; set; }
    public string ClaimEmote { get; set; } = string.Empty;
    public string CompleteEmote { get; set; } = string.Empty;
  }
}
