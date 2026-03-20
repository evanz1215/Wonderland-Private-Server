namespace Wonderland.Domain.Entities;

/// <summary>
/// User account entity — maps to the 'user' table
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CipherPassword { get; set; } = string.Empty;
    public Enums.GmStatus GmLevel { get; set; }
    public int ImPoints { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public ICollection<Character> Characters { get; set; } = [];
}
