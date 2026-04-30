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
            NightHours = request.NightHours
        };

        var currentDate = request.StartDate;
        var cycleLength = shiftPattern.Count;
        var cycleIndex = 0;

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

                if (currentDate.Day == day && currentDate.Month == monthData.Month && currentDate.Year == monthData.Year)
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
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(entity);
            await _repository.TrimUserSchedulesAsync(userId, 5);
        }

        return response;
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