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
    public string? ShiftType { get; set; } = "standard"; // "standard", "rotating", "custom"
    public int BreakMinutes { get; set; } = 0;
    public List<string>? ShiftPattern { get; set; } // Например: ["day", "night", "day", "off"] для цикла
    public string? DayHours { get; set; } = "08:00-20:00"; // Часы дневной смены
    public string? NightHours { get; set; } = "20:00-08:00"; // Часы ночной смены
    
    // Поля для расчета зарплаты
    public decimal? MonthlySalary { get; set; } // Месячный оклад
    public decimal? HourlyRate { get; set; } // Ставка за час
    
    // Переработки
    public List<OvertimeInput>? Overtimes { get; set; } // Список переработок
}

public class OvertimeInput
{
    public DateTime Date { get; set; }
    public int Hours { get; set; }
}