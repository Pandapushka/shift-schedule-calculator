namespace Application.DTOs;

public class ShiftScheduleRequest
{
    public DateTime StartDate { get; set; }
    public int WorkDays { get; set; }
    public int OffDays { get; set; }
    public int HoursPerShift { get; set; }
    public int Months { get; set; }
    public bool NightFirst { get; set; }
    public string? Title { get; set; }
}