namespace Application.DTOs;

public class ShiftScheduleResponse
{
    public string? Title { get; set; }
    public List<MonthData> Months { get; set; } = new();
    public int TotalWorkCount { get; set; }
    public int TotalHours { get; set; }
    public string? ShiftPattern { get; set; } // JSON: ["day", "night", "off"]
    public string? DayHours { get; set; }
    public string? NightHours { get; set; }
}

public class MonthData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<DayData> Days { get; set; } = new();
    public int WorkCount { get; set; }
    public int HoursCount { get; set; }
}

public class DayData
{
    public int Day { get; set; }
    public string Status { get; set; } = string.Empty; // "work", "off", "empty"
    public string? ShiftType { get; set; } // "day", "night", null for off days
}