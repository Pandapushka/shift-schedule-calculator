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
        ValidateRequest(request);

        var scheduleId = Guid.NewGuid();
        var title = string.IsNullOrWhiteSpace(request.Title) ? scheduleId.ToString() : request.Title;

        // Если нет кастомного паттерна, создаём стандартный (все дневные смены)
        var shiftPattern = request.FiveTwoSchedule
            ? new List<string> { "day", "day", "day", "day", "day", "off", "off" }
            : request.ShiftPattern?.Any() == true 
            ? request.ShiftPattern 
            : CreateShiftPattern(request);

        var response = new ShiftScheduleResponse
        {
            Title = title,
            NumMonths = request.Months,
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
            ?? new Dictionary<DateTime, decimal>();

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
                var shiftTypeInCycle = GetShiftTypeForDate(currentDayDate, request, shiftPattern, cycleIndex);
                var hasOvertime = overtimeDates.ContainsKey(currentDayDate);

                if (hasOvertime)
                {
                    dayData.Status = "overtime";
                    response.Overtimes.Add(new OvertimeOutput { Date = currentDayDate, Hours = overtimeDates[currentDayDate] });
                }
                else
                {
                    dayData.Status = shiftTypeInCycle == "off" ? "off" : "work";
                }

                if (shiftTypeInCycle != "off")
                {
                    dayData.ShiftType = shiftTypeInCycle; // "day" или "night"
                    monthData.WorkCount++;
                    monthData.HoursCount += ToPayHours(request.HoursPerShift);
                    response.TotalWorkCount++;
                    response.TotalHours += ToPayHours(request.HoursPerShift);
                }

                monthData.Days.Add(dayData);
                cycleIndex++;
            }

            response.Months.Add(monthData);

            // Переходим к следующему месяцу
            currentDate = currentDate.AddMonths(1);
        }

        // Рассчитываем зарплату
        CalculateSalary(response, request, shiftPattern);
        RoundHourTotalsForDisplay(response);

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

    private void CalculateSalary(ShiftScheduleResponse response, ShiftScheduleRequest request, List<string> shiftPattern)
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

        // Добавляем часовую ставку в ответ для фронтенда
        response.HourlyRate = hourlyRate;

        // Расчет базовой зарплаты
        if (request.MonthlySalary.HasValue && request.MonthlySalary > 0)
        {
            response.BaseSalary = request.MonthlySalary.Value * request.Months;
        }
        else
        {
            response.BaseSalary = Math.Round(response.TotalHours * hourlyRate, 2);
        }

        // Расчет переработок согласно ТК РФ
        decimal overtimeSalary = 0;
        if (response.Overtimes?.Any() == true)
        {
            foreach (var overtime in response.Overtimes)
            {
                if (overtime.Hours <= 0) continue;

                // Определяем, является ли день выходным
                bool isOffDay = IsOffDay(overtime.Date, request, shiftPattern);
                
                decimal amount = 0;
                overtime.Breakdown.Clear();
                
                if (isOffDay)
                {
                    // Переработка в выходной день: все часы по 2x
                    overtime.Multiplier = 2.0m;
                    amount = ToPayHours(overtime.Hours) * hourlyRate * overtime.Multiplier;
                    overtime.Breakdown.Add(new OvertimeBreakdown
                    {
                        Hours = overtime.Hours,
                        Multiplier = 2.0m,
                        Amount = Math.Round(amount, 2)
                    });
                }
                else
                {
                    // Переработка в рабочий день: первые 2 часа по 1.5x, остальные по 2x
                    if (overtime.Hours <= 2)
                    {
                        // Все часы по 1.5x (≤ 2 часов)
                        overtime.Multiplier = 1.5m;
                        amount = ToPayHours(overtime.Hours) * hourlyRate * overtime.Multiplier;
                        overtime.Breakdown.Add(new OvertimeBreakdown
                        {
                            Hours = overtime.Hours,
                            Multiplier = 1.5m,
                            Amount = Math.Round(amount, 2)
                        });
                    }
                    else
                    {
                        // Первые 2 часа по 1.5x
                        decimal firstPart = 2 * hourlyRate * 1.5m;
                        overtime.Breakdown.Add(new OvertimeBreakdown
                        {
                            Hours = 2,
                            Multiplier = 1.5m,
                            Amount = Math.Round(firstPart, 2)
                        });
                        
                        // Остальные часы по 2x
                        var remainingHours = overtime.Hours - 2;
                        decimal secondPart = ToPayHours(remainingHours) * hourlyRate * 2.0m;
                        overtime.Breakdown.Add(new OvertimeBreakdown
                        {
                            Hours = remainingHours,
                            Multiplier = 2.0m,
                            Amount = Math.Round(secondPart, 2)
                        });
                        
                        amount = firstPart + secondPart;
                        overtime.Multiplier = 1.5m; // Основной коэффициент для таблицы
                    }
                }

                overtime.Amount = Math.Round(amount, 2);
                overtimeSalary += overtime.Amount;
            }
        }

        response.OvertimeSalary = Math.Round(overtimeSalary, 2);
        response.TotalSalary = Math.Round(response.BaseSalary + overtimeSalary, 2);
    }

    private bool IsOffDay(DateTime date, ShiftScheduleRequest request, List<string> shiftPattern)
    {
        if (request.FiveTwoSchedule)
        {
            return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        }

        // Вычисляем, сколько дней прошло от даты начала
        var daysDiff = (date - request.StartDate).Days;
        if (daysDiff < 0) return false; // Дата переработки раньше даты начала
        
        // Определяем позицию в цикле
        var cycleLength = shiftPattern.Count;
        var cycleIndex = daysDiff % cycleLength;
        
        // Проверяем, является ли этот день выходным в паттерне
        return shiftPattern[cycleIndex] == "off";
    }

    private static string GetShiftTypeForDate(
        DateTime date,
        ShiftScheduleRequest request,
        List<string> shiftPattern,
        int cycleIndex)
    {
        if (request.FiveTwoSchedule)
        {
            return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "off" : "day";
        }

        return shiftPattern[cycleIndex % shiftPattern.Count];
    }

    private static void ValidateRequest(ShiftScheduleRequest request)
    {
        ValidateHourMinuteValue(request.HoursPerShift, nameof(request.HoursPerShift));

        if (request.Overtimes is null)
        {
            return;
        }

        foreach (var overtime in request.Overtimes)
        {
            ValidateHourMinuteValue(overtime.Hours, nameof(overtime.Hours));
        }
    }

    private static void ValidateHourMinuteValue(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{fieldName} должно быть больше 0");
        }

        if (decimal.Round(value, 2) != value)
        {
            throw new ArgumentException($"{fieldName} должно использовать формат ЧЧ.ММ максимум с двумя знаками после точки");
        }

        var hours = decimal.Truncate(value);
        var minutes = (value - hours) * 100;

        if (minutes != decimal.Truncate(minutes) || minutes > 59)
        {
            throw new ArgumentException($"{fieldName}: минуты не могут быть больше 59");
        }

        if (hours > 24 || (hours == 24 && minutes > 0))
        {
            throw new ArgumentException($"{fieldName}: максимум 24 часа или 23.59 при указании минут");
        }
    }

    private static decimal ToPayHours(decimal hourMinuteValue)
    {
        var hours = decimal.Truncate(hourMinuteValue);
        var minutes = (hourMinuteValue - hours) * 100;
        return hours + minutes / 60;
    }

    private static void RoundHourTotalsForDisplay(ShiftScheduleResponse response)
    {
        response.TotalHours = Math.Round(response.TotalHours, 2);

        foreach (var month in response.Months)
        {
            month.HoursCount = Math.Round(month.HoursCount, 2);
        }
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

    private List<string> CreateShiftPattern(ShiftScheduleRequest request)
    {
        var pattern = new List<string>();
        
        if (request.OffFirst)
        {
            // Начинаем с выходных дней
            pattern.AddRange(Enumerable.Range(0, request.OffDays).Select(_ => "off"));
            pattern.AddRange(Enumerable.Range(0, request.WorkDays).Select(_ => "day"));
        }
        else
        {
            // Начинаем с рабочих дней
            pattern.AddRange(Enumerable.Range(0, request.WorkDays).Select(_ => "day"));
            pattern.AddRange(Enumerable.Range(0, request.OffDays).Select(_ => "off"));
        }
        
        return pattern;
    }
}
