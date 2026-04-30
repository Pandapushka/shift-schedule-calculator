namespace Application.DTOs;

public class ShiftScheduleHistoryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ShiftScheduleResponse Chart { get; set; } = new();
}