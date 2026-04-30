using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ShiftScheduleRepository : IShiftScheduleRepository
{
    private readonly ApplicationDbContext _context;

    public ShiftScheduleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShiftSchedule> AddAsync(ShiftSchedule shiftSchedule)
    {
        _context.ShiftSchedules.Add(shiftSchedule);
        await _context.SaveChangesAsync();
        return shiftSchedule;
    }

    public async Task<ShiftSchedule?> GetByIdAsync(Guid id)
    {
        return await _context.ShiftSchedules.FindAsync(id);
    }

    public async Task<IEnumerable<ShiftSchedule>> GetAllAsync()
    {
        return await _context.ShiftSchedules.ToListAsync();
    }

    public async Task<IEnumerable<ShiftSchedule>> GetRecentByUserAsync(string userId, int limit)
    {
        return await _context.ShiftSchedules
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task TrimUserSchedulesAsync(string userId, int keepCount)
    {
        var extraSchedules = await _context.ShiftSchedules
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(keepCount)
            .ToListAsync();

        if (extraSchedules.Any())
        {
            _context.ShiftSchedules.RemoveRange(extraSchedules);
            await _context.SaveChangesAsync();
        }
    }
}