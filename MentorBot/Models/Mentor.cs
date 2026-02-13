using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MentorBot.Models;

[Index(nameof(Token), IsUnique = true)]
public record Mentor
{
    [Key] public string ResoUserId { get; set; } = string.Empty;

    public ulong? DiscordId { get; set; }
    public string Name { get; set; } = string.Empty;

    [MaxLength(60)] public string? Token { get; set; } = string.Empty;

    public MentorDto ToDto()
    {
        return new MentorDto(this);
    }
}

public record MentorDto
{
    private readonly Mentor _mentor;

    public MentorDto(Mentor mentor)
    {
        _mentor = mentor;
    }

    public string Id => _mentor.ResoUserId;
    public string Name => _mentor.Name;
    public bool InProgram => !string.IsNullOrWhiteSpace(_mentor.Token);
}