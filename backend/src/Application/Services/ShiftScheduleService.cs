using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using System.Text.Json;

namespace Application.Services;

public class ShiftScheduleService : IShiftScheduleService
{
    private readonly IShiftScheduleRepository _repository;

    public ShiftScheduleService(IShiftScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<ShiftScheduleResponse> CalculateAsync(ShiftScheduleRequest request, string? userId = null)
    {
        var scheduleId = Guid.NewGuid();
        var title = string.IsNullOrWhiteSpace(request.Title) ? scheduleId.ToString() : request.Title;

        // Если нет кастомного паттерна, создаём стандартный (все дневные смены)
        var shiftPattern = request.ShiftPattern?.Any() == true 
            ? request.ShiftPattern 
            : Enumerable.Range(0, request.WorkDays).Select(_ => "day").Concat(
                  Enumerable.Range(0, request.OffDays).Select(_ => "off")).ToList();

        var response = new ShiftScheduleResponse
        {
            Title = title,
            ShiftPattern = JsonSerializer.Serialize(shiftPattern),
            DayHours = request.DayHours,
            NightHours = request.NightHours,
            MonthlySalary = request.MonthlySalary,
            HourlyRate = request.HourlyRate,
            Overtimes = new List<OvertimeOutput>() // Инициализируем явно
        };

        var currentDate = request.StartDate;
        var cycleLength = shiftPattern.Count;
        var cycleIndex = 0;
        
        // Создаём набор дат переработок для быстрого поиска и суммируем повторы по дате
        var overtimeDates = request.Overtimes?
            .GroupBy(o => o.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Hours))
            ?? new Dictionary<DateTime, int>();

        for (int month = 0; month < request.Months; month++)
        {
            var monthData = new MonthData
            {
                Year = currentDate.Year,
                Month = currentDate.Month,
                Days = new List<DayData>()
            };

            var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var dayData = new DayData { Day = day, Status = "empty", ShiftType = null };
                var currentDayDate = new DateTime(monthData.Year, monthData.Month, day);

                // Проверяем переработку
                if (overtimeDates.ContainsKey(currentDayDate))
                {
                    dayData.Status = "overtime";
                    response.Overtimes.Add(new OvertimeOutput { Date = currentDayDate, Hours = overtimeDates[currentDayDate] });
                }
                else if (currentDate.Day == day && currentDate.Month == monthData.Month && currentDate.Year == monthData.Year)
                {
                    var shiftTypeInCycle = shiftPattern[cycleIndex % cycleLength];

                    if (shiftTypeInCycle == "off")
                    {
                        dayData.Status = "off";
                    }
                    else
                    {
                        dayData.Status = "work";
                        dayData.ShiftType = shiftTypeInCycle; // "day" или "night"
                        monthData.WorkCount++;
                        monthData.HoursCount += request.HoursPerShift;
                        response.TotalWorkCount++;
                        response.TotalHours += request.HoursPerShift;
                    }

                    cycleIndex++;
                    currentDate = currentDate.AddDays(1);
                }

                monthData.Days.Add(dayData);
            }

            response.Months.Add(monthData);
        }

        // Рассчитываем зарплату
        CalculateSalary(response, request);

        if (!string.IsNullOrEmpty(userId))
        {
            var entity = new ShiftSchedule
            {
                Id = scheduleId,
                Title = title,
                UserId = userId,
                StartDate = request.StartDate,
                WorkDays = request.WorkDays,
                OffDays = request.OffDays,
                HoursPerShift = request.HoursPerShift,
                Months = request.Months,
                NightFirst = request.NightFirst,
                ShiftPattern = JsonSerializer.Serialize(shiftPattern),
                DayHours = request.DayHours,
                NightHours = request.NightHours,
                CalendarJson = JsonSerializer.Serialize(response),
                CreatedAt = DateTime.UtcNow,
                MonthlySalary = request.MonthlySalary,
                HourlyRate = request.HourlyRate,
                BaseSalary = response.BaseSalary,
                OvertimeSalary = response.OvertimeSalary,
                TotalSalary = response.TotalSalary
            };

            if (response.Overtimes?.Any() == true)
            {
                entity.Overtimes = response.Overtimes.Select(ot => new Overtime
                {
                    Id = Guid.NewGuid(),
                    ShiftScheduleId = scheduleId,
                    OvertimeDate = ot.Date,
                    Hours = ot.Hours,
                    Amount = ot.Amount
                }).ToList();
            }

            await _repository.AddAsync(entity);
            await _repository.TrimUserSchedulesAsync(userId, 5);
        }

        return response;
    }

    private void CalculateSalary(ShiftScheduleResponse response, ShiftScheduleRequest request)
    {
        decimal hourlyRate = 0;

        // Определяем часовую ставку
        if (request.MonthlySalary.HasValue && request.MonthlySalary > 0)
        {
            // Средняя рабочая часов в месяц (для РФ обычно 160)
            const int averageWorkHoursPerMonth = 160;
            hourlyRate = request.MonthlySalary.Value / averageWorkHoursPerMonth;
        }
        else if (request.HourlyRate.HasValue && request.HourlyRate > 0)
        {
            hourlyRate = request.HourlyRate.Value;
        }
        else
        {
            hourlyRate = 0;
        }

        // Расчет базовой зарплаты
        response.BaseSalary = response.TotalHours * hourlyRate;

        // Расчет переработок согласно ТК РФ
        decimal overtimeSalary = 0;
        if (response.Overtimes?.Any() == true)
        {
            foreach (var overtime in response.Overtimes)
            {
                if (overtime.Hours <= 0) continue;

                // По ТК РФ:
                // - Первые 2 часа переработки: оплачиваются не менее чем в полуторном размере (1.5x)
                // - Остальные часы: оплачиваются не менее чем в двойном размере (2x)
                
                decimal amount = 0;
                if (overtime.Hours <= 2)
                {
                    overtime.Multiplier = 1.5m;
                    amount = overtime.Hours * hourlyRate * overtime.Multiplier;
                }
                else
                {
                    // Первые 2 часа по 1.5x
                    amount += 2 * hourlyRate * 1.5m;
                    // Остальные часы по 2x
                    overtime.Multiplier = 2.0m;
                    amount += (overtime.Hours - 2) * hourlyRate * overtime.Multiplier;
                }

                overtime.Amount = Math.Round(amount, 2);
                overtimeSalary += overtime.Amount;
            }
        }

        response.OvertimeSalary = Math.Round(overtimeSalary, 2);
        response.TotalSalary = Math.Round(response.BaseSalary + overtimeSalary, 2);
    }

    public async Task<IEnumerable<ShiftScheduleHistoryResponse>> GetRecentSchedulesAsync(string userId, int limit = 5)
    {
        var schedules = await _repository.GetRecentByUserAsync(userId, limit);

        return schedules.Select(s => new ShiftScheduleHistoryResponse
        {
            Id = s.Id,
            Title = s.Title ?? s.Id.ToString(),
            CreatedAt = s.CreatedAt,
            Chart = JsonSerializer.Deserialize<ShiftScheduleResponse>(s.CalendarJson) ?? new ShiftScheduleResponse
            {
                Title = s.Title ?? s.Id.ToString()
            }
        });
    }
}