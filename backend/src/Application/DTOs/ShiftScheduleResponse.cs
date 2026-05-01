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
    
    // Зарплата и переработки
    public decimal? MonthlySalary { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal BaseSalary { get; set; } // Базовая зарплата за рабочие часы
    public decimal OvertimeSalary { get; set; } // Зарплата за переработки
    public decimal TotalSalary { get; set; } // Итоговая зарплата
    public List<OvertimeOutput>? Overtimes { get; set; } = new();
}

public class OvertimeOutput
{
    public DateTime Date { get; set; }
    public int Hours { get; set; }
    public decimal Multiplier { get; set; } // 1.5 или 2
    public decimal Amount { get; set; } // Сумма за эту переработку
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
    public string Status { get; set; } = string.Empty; // "work", "off", "empty", "overtime"
    public string? ShiftType { get; set; } // "day", "night", null for off days
}