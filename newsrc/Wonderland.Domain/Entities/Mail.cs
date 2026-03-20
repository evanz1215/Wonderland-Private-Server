namespace Wonderland.Domain.Entities;

public class Mail
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Attachment
    public ushort? AttachmentItemId { get; set; }
    public ushort AttachmentQuantity { get; set; }
    public int AttachmentGold { get; set; }

    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
