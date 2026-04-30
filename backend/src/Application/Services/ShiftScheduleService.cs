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

        var response = new ShiftScheduleResponse
        {
            Title = title
        };

        var currentDate = request.StartDate;
        var cycleLength = request.WorkDays + request.OffDays;
        var isWorkDay = true;
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
                var status = "empty";
                if (currentDate.Day == day && currentDate.Month == monthData.Month && currentDate.Year == monthData.Year)
                {
                    if (isWorkDay)
                    {
                        status = "work";
                        monthData.WorkCount++;
                        monthData.HoursCount += request.HoursPerShift;
                        response.TotalWorkCount++;
                        response.TotalHours += request.HoursPerShift;
                    }
                    else
                    {
                        status = "off";
                    }

                    cycleIndex++;
                    if (cycleIndex >= cycleLength)
                    {
                        cycleIndex = 0;
                        isWorkDay = !isWorkDay;
                    }
                    else if (cycleIndex < request.WorkDays)
                    {
                        isWorkDay = true;
                    }
                    else
                    {
                        isWorkDay = false;
                    }

                    currentDate = currentDate.AddDays(1);
                }

                monthData.Days.Add(new DayData { Day = day, Status = status });
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