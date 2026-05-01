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
    public string? ShiftPattern { get; set; } // JSON array: ["day", "night", "off"]
    public string? DayHours { get; set; } = "08:00-20:00"; // Часы дневной смены
    public string? NightHours { get; set; } = "20:00-08:00"; // Часы ночной смены
    
    // Поля для расчета зарплаты
    public decimal? MonthlySalary { get; set; } // Месячный оклад
    public decimal? HourlyRate { get; set; } // Ставка за час
    public decimal BaseSalary { get; set; } // Базовая зарплата за рабочие часы
    public decimal OvertimeSalary { get; set; } // Зарплата за переработки
    public decimal TotalSalary { get; set; } // Итоговая зарплата с переработками
    
    // Связь с переработками
    public ICollection<Overtime> Overtimes { get; set; } = new List<Overtime>();
}