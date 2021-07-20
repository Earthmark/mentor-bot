namespace MentorBot.Models
{
  public class CosmosOptions
  {
    public string Database { get; set; } = "mentor-bot";
    public string MentorsContainer { get; set; } = "mentors";
    public string TicketsContainer { get; set; } = "tickets";
  }
}
