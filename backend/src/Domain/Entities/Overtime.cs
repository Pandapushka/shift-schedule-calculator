namespace Domain.Entities;

public class Overtime
{
    public Guid Id { get; set; }
    public Guid ShiftScheduleId { get; set; }
    public DateTime OvertimeDate { get; set; }
    public int Hours { get; set; } // Количество часов переработки
    public decimal Amount { get; set; } // Рассчитанная сумма для этой переработки
    public string? Notes { get; set; } // Дополнительные заметки

    public ShiftSchedule? ShiftSchedule { get; set; }
}
