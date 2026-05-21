using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Repositories;

var repository = new InMemoryShiftScheduleRepository();
var service = new ShiftScheduleService(repository);

await TestBasicScheduleCalculation(service);
await TestFiveTwoSchedule(service);
await TestOvertimePaymentByRussianLaborCode(service);
await TestAuthenticatedScheduleHistory(service, repository);

Console.WriteLine("Release smoke tests passed.");

static async Task TestBasicScheduleCalculation(ShiftScheduleService service)
{
    var result = await service.CalculateAsync(new ShiftScheduleRequest
    {
        StartDate = new DateTime(2026, 1, 1),
        WorkDays = 2,
        OffDays = 2,
        HoursPerShift = 11,
        Months = 1,
        ShiftPattern = ["day", "night", "off", "off"]
    });

    AssertEqual(1, result.Months.Count, "Expected one calculated month.");
    AssertEqual(16, result.TotalWorkCount, "2/2 schedule from 2026-01-01 should have 16 work shifts in January.");
    AssertEqual(176m, result.TotalHours, "16 shifts x 11 hours should be 176 hours.");
    AssertEqual("day", result.Months[0].Days[0].ShiftType, "First work day should be day shift.");
    AssertEqual("night", result.Months[0].Days[1].ShiftType, "Second work day should be night shift.");
    AssertEqual("off", result.Months[0].Days[2].Status, "Third day should be off.");
}

static async Task TestOvertimePaymentByRussianLaborCode(ShiftScheduleService service)
{
    var result = await service.CalculateAsync(new ShiftScheduleRequest
    {
        StartDate = new DateTime(2026, 1, 1),
        WorkDays = 2,
        OffDays = 2,
        HoursPerShift = 11,
        Months = 1,
        HourlyRate = 1000m,
        ShiftPattern = ["day", "day", "off", "off"],
        Overtimes =
        [
            new OvertimeInput { Date = new DateTime(2026, 1, 1), Hours = 5 },
            new OvertimeInput { Date = new DateTime(2026, 1, 3), Hours = 4 },
            new OvertimeInput { Date = new DateTime(2026, 1, 3), Hours = 1 }
        ]
    });

    AssertEqual(16, result.TotalWorkCount, "Work-day overtime should not remove the regular shift from work count.");
    AssertEqual(176m, result.TotalHours, "Work-day overtime should not remove regular shift hours from base hours.");
    AssertEqual(176000m, result.BaseSalary, "Base salary should include all regular shift hours even when a work day has overtime.");

    var workDayOvertime = result.Overtimes!.Single(o => o.Date.Date == new DateTime(2026, 1, 1));
    AssertEqual(5, workDayOvertime.Hours, "Work-day overtime hours should be preserved.");
    AssertEqual(9000m, workDayOvertime.Amount, "Work-day overtime should be first 2 hours at 1.5x and remaining 3 hours at 2x.");
    AssertEqual(2, workDayOvertime.Breakdown.Count, "Work-day overtime over 2 hours should have two payment brackets.");
    AssertEqual(3000m, workDayOvertime.Breakdown[0].Amount, "First 2 work-day overtime hours should be paid at 1.5x.");
    AssertEqual(6000m, workDayOvertime.Breakdown[1].Amount, "Remaining 3 work-day overtime hours should be paid at 2x.");

    var offDayOvertime = result.Overtimes!.Single(o => o.Date.Date == new DateTime(2026, 1, 3));
    AssertEqual(5, offDayOvertime.Hours, "Duplicate overtime entries for the same date should be summed.");
    AssertEqual(10000m, offDayOvertime.Amount, "Off-day work should be paid at 2x for all hours.");
    AssertEqual(19000m, result.OvertimeSalary, "Total overtime salary should include work-day and off-day overtime.");

    var minuteResult = await service.CalculateAsync(new ShiftScheduleRequest
    {
        StartDate = new DateTime(2026, 1, 1),
        WorkDays = 1,
        OffDays = 1,
        HoursPerShift = 11.50m,
        Months = 1,
        HourlyRate = 1000m,
        Overtimes =
        [
            new OvertimeInput { Date = new DateTime(2026, 1, 1), Hours = 2.30m }
        ]
    });

    AssertEqual(16, minuteResult.TotalWorkCount, "1/1 schedule should have 16 work shifts in January.");
    AssertEqual(189.33m, minuteResult.TotalHours, "11.50 should mean 11 hours 50 minutes, not 11.5 decimal hours.");
    AssertEqual(189333.33m, minuteResult.BaseSalary, "Base salary should pay 11h50m shifts by actual minutes.");
    AssertEqual(4000m, minuteResult.OvertimeSalary, "2.30 overtime should mean first 2h at 1.5x plus 30m at 2x.");
}

static async Task TestFiveTwoSchedule(ShiftScheduleService service)
{
    var result = await service.CalculateAsync(new ShiftScheduleRequest
    {
        StartDate = new DateTime(2026, 1, 1),
        WorkDays = 2,
        OffDays = 2,
        HoursPerShift = 8,
        Months = 1,
        FiveTwoSchedule = true,
        HourlyRate = 1000m,
        Overtimes =
        [
            new OvertimeInput { Date = new DateTime(2026, 1, 3), Hours = 2 }
        ]
    });

    AssertEqual(22, result.TotalWorkCount, "Base 5/2 schedule should count only Monday-Friday work days in January 2026.");
    AssertEqual(176m, result.TotalHours, "Base 5/2 schedule should exclude Saturdays and Sundays from regular hours.");
    AssertEqual("overtime", result.Months[0].Days[2].Status, "Saturday overtime should be marked as overtime in base 5/2 schedule.");
    AssertEqual<string?>(null, result.Months[0].Days[2].ShiftType, "Saturday should not be counted as a regular shift in base 5/2 schedule.");
    AssertEqual("off", result.Months[0].Days[3].Status, "Sunday should be off in base 5/2 schedule.");
    AssertEqual(4000m, result.OvertimeSalary, "Overtime on Saturday in base 5/2 schedule should be paid at 2x.");
}

static async Task TestAuthenticatedScheduleHistory(
    ShiftScheduleService service,
    InMemoryShiftScheduleRepository repository)
{
    const string userId = "release-user";

    for (var i = 1; i <= 6; i++)
    {
        await service.CalculateAsync(new ShiftScheduleRequest
        {
            StartDate = new DateTime(2026, i, 1),
            WorkDays = 1,
            OffDays = 1,
            HoursPerShift = 8,
            Months = 1,
            Title = $"Schedule {i}"
        }, userId);
    }

    var history = (await service.GetRecentSchedulesAsync(userId)).ToList();
    AssertEqual(5, history.Count, "Only five recent schedules should be kept per authenticated user.");
    AssertEqual(5, repository.Schedules.Count, "Repository should be trimmed to five schedules.");
    AssertEqual("Schedule 6", history[0].Title, "History should be ordered from newest to oldest.");
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
    }
}

internal sealed class InMemoryShiftScheduleRepository : IShiftScheduleRepository
{
    public List<ShiftSchedule> Schedules { get; } = [];

    public Task<ShiftSchedule> AddAsync(ShiftSchedule shiftSchedule)
    {
        Schedules.Add(shiftSchedule);
        return Task.FromResult(shiftSchedule);
    }

    public Task<ShiftSchedule?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(Schedules.SingleOrDefault(s => s.Id == id));
    }

    public Task<IEnumerable<ShiftSchedule>> GetAllAsync()
    {
        return Task.FromResult(Schedules.AsEnumerable());
    }

    public Task<IEnumerable<ShiftSchedule>> GetRecentByUserAsync(string userId, int limit)
    {
        var schedules = Schedules
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit);

        return Task.FromResult(schedules);
    }

    public Task TrimUserSchedulesAsync(string userId, int keepCount)
    {
        var extraSchedules = Schedules
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(keepCount)
            .ToList();

        foreach (var schedule in extraSchedules)
        {
            Schedules.Remove(schedule);
        }

        return Task.CompletedTask;
    }
}
