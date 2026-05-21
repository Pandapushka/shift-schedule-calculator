namespace Application.DTOs;

public class SupportTicketRequest
{
    public string Message { get; set; } = string.Empty;
    public int Rating { get; set; }
}

public class SupportReplyRequest
{
    public string Reply { get; set; } = string.Empty;
}

public class SupportTicketResponse
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? AdminReply { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
