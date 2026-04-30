using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftScheduleController : ControllerBase
{
    private readonly IShiftScheduleService _service;

    public ShiftScheduleController(IShiftScheduleService service)
    {
        _service = service;
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] ShiftScheduleRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.CalculateAsync(request, userId);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var history = await _service.GetRecentSchedulesAsync(userId);
        return Ok(history);
    }
}