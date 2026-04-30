namespace Domain.Entities;

public class ShiftSchedule
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? UserId { get; set; }
    public DateTime StartDate { get; set; }
    public int WorkDays { get; set; }
    public int OffDays { get; set; }
    public int HoursPerShift { get; set; }
    public int Months { get; set; }
    public bool NightFirst { get; set; }
    public string CalendarJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}