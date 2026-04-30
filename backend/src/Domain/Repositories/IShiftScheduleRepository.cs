using Domain.Entities;

namespace Domain.Repositories;

public interface IShiftScheduleRepository
{
    Task<ShiftSchedule> AddAsync(ShiftSchedule shiftSchedule);
    Task<ShiftSchedule?> GetByIdAsync(Guid id);
    Task<IEnumerable<ShiftSchedule>> GetAllAsync();
    Task<IEnumerable<ShiftSchedule>> GetRecentByUserAsync(string userId, int limit);
    Task TrimUserSchedulesAsync(string userId, int keepCount);
}