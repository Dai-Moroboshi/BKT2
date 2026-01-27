using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCM.API.Data;
using PCM.API.DTOs;

namespace PCM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourtsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CourtsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CourtDto>>>> GetCourts()
    {
        var courts = await _context.Courts
            .Where(c => c.IsActive)
            .Select(c => new CourtDto
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                Description = c.Description,
                PricePerHour = c.PricePerHour
            })
            .ToListAsync();

        return Ok(ApiResponse<List<CourtDto>>.Ok(courts));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CourtDto>>> GetCourt(int id)
    {
        var court = await _context.Courts.FindAsync(id);

        if (court == null)
            return NotFound(ApiResponse<CourtDto>.Fail("Không tìm thấy sân"));

        var result = new CourtDto
        {
            Id = court.Id,
            Name = court.Name,
            IsActive = court.IsActive,
            Description = court.Description,
            PricePerHour = court.PricePerHour
        };

        return Ok(ApiResponse<CourtDto>.Ok(result));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<CourtDto>>>> GetAllCourts()
    {
        var courts = await _context.Courts
            .Select(c => new CourtDto
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                Description = c.Description,
                PricePerHour = c.PricePerHour
            })
            .ToListAsync();

        return Ok(ApiResponse<List<CourtDto>>.Ok(courts));
    }
}
