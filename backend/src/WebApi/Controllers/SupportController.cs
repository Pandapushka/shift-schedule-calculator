using Application.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public SupportController(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets()
    {
        var isAdmin = IsAdmin();
        var query = _dbContext.SupportTickets.AsNoTracking();

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SupportTicketResponse
            {
                Id = t.Id,
                UserEmail = isAdmin ? t.UserEmail : string.Empty,
                Message = t.Message,
                Rating = t.Rating,
                AdminReply = t.AdminReply,
                CreatedAt = t.CreatedAt,
                AnsweredAt = t.AnsweredAt,
                Status = t.Status
            })
            .ToListAsync();

        return Ok(tickets);
    }

    [Authorize]
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] SupportTicketRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = GetUserEmail();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Текст отзыва обязателен");
        }

        if (request.Message.Length > 2000)
        {
            return BadRequest("Отзыв не может быть длиннее 2000 символов");
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest("Оценка должна быть от 1 до 5");
        }

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserEmail = email,
            Message = request.Message.Trim(),
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow,
            Status = "open"
        };

        _dbContext.SupportTickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(ticket));
    }

    [Authorize]
    [HttpPost("tickets/{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] SupportReplyRequest request)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Reply))
        {
            return BadRequest("Текст ответа обязателен");
        }

        if (request.Reply.Length > 2000)
        {
            return BadRequest("Ответ не может быть длиннее 2000 символов");
        }

        var ticket = await _dbContext.SupportTickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        ticket.AdminReply = request.Reply.Trim();
        ticket.AnsweredAt = DateTime.UtcNow;
        ticket.Status = "answered";

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(ticket));
    }

    private bool IsAdmin()
    {
        var adminEmail = _configuration["Admin:Email"] ?? "admin@admin.ru";
        return string.Equals(GetUserEmail(), adminEmail, StringComparison.OrdinalIgnoreCase);
    }

    private string GetUserEmail()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? string.Empty;
    }

    private static SupportTicketResponse ToResponse(SupportTicket ticket)
    {
        return new SupportTicketResponse
        {
            Id = ticket.Id,
            UserEmail = ticket.UserEmail,
            Message = ticket.Message,
            Rating = ticket.Rating,
            AdminReply = ticket.AdminReply,
            CreatedAt = ticket.CreatedAt,
            AnsweredAt = ticket.AnsweredAt,
            Status = ticket.Status
        };
    }
}
