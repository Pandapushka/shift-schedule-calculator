using Application.DTOs;

namespace Application.Services;

public interface IShiftScheduleService
{
    Task<ShiftScheduleResponse> CalculateAsync(ShiftScheduleRequest request, string? userId = null);
    Task<IEnumerable<ShiftScheduleHistoryResponse>> GetRecentSchedulesAsync(string userId, int limit = 5);
}