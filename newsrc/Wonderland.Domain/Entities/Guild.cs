namespace Wonderland.Domain.Entities;

public class Guild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LeaderId { get; set; }
    public int? ViceLeaderId { get; set; }
    public string Rules { get; set; } = string.Empty;
    public byte Level { get; set; } = 1;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<GuildMember> Members { get; set; } = [];
}

public class GuildMember
{
    public int Id { get; set; }
    public int GuildId { get; set; }
    public int CharId { get; set; }
    public byte Rank { get; set; }
    public DateTime JoinedAt { get; set; }

    // Navigation
    public Guild? Guild { get; set; }
    public Character? Character { get; set; }
}
