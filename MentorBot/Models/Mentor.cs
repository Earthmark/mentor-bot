using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MentorBot.Models
{
  [Index(nameof(Token), IsUnique = true)]
  public class Mentor
  {
    [Key]
    public string NeosId { get; set; } = string.Empty;
    public ulong? DiscordId { get; set; }
    public string Name { get; set; } = string.Empty;
    [MaxLength(60)]
    public string? Token { get; set; } = string.Empty;

    public MentorDto ToDto() => new(this);
  }

  public class MentorDto
  {
    private readonly Mentor _mentor;
    public MentorDto(Mentor mentor)
    {
      _mentor = mentor;
    }

    public string Id => _mentor.NeosId;
    public string Name => _mentor.Name;
    public bool InProgram => !string.IsNullOrWhiteSpace(_mentor.Token);
  }
}
