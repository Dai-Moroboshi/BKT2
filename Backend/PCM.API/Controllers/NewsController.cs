using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCM.API.Data;
using PCM.API.DTOs;
using PCM.API.Entities;

namespace PCM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NewsDto>>>> GetNews(
        [FromQuery] bool? pinnedOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.News.AsQueryable();

        if (pinnedOnly == true)
        {
            query = query.Where(n => n.IsPinned);
        }

        var news = await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NewsDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                IsPinned = n.IsPinned,
                CreatedDate = n.CreatedDate,
                ImageUrl = n.ImageUrl
            })
            .ToListAsync();

        return Ok(ApiResponse<List<NewsDto>>.Ok(news));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<NewsDto>>> GetNewsItem(int id)
    {
        var news = await _context.News.FindAsync(id);

        if (news == null)
            return NotFound(ApiResponse<NewsDto>.Fail("Không tìm thấy tin tức"));

        var result = new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            IsPinned = news.IsPinned,
            CreatedDate = news.CreatedDate,
            ImageUrl = news.ImageUrl
        };

        return Ok(ApiResponse<NewsDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<NewsDto>>> CreateNews([FromBody] CreateNewsDto dto)
    {
        var news = new News
        {
            Title = dto.Title,
            Content = dto.Content,
            IsPinned = dto.IsPinned,
            ImageUrl = dto.ImageUrl,
            CreatedDate = DateTime.UtcNow
        };

        _context.News.Add(news);
        await _context.SaveChangesAsync();

        var result = new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            IsPinned = news.IsPinned,
            CreatedDate = news.CreatedDate,
            ImageUrl = news.ImageUrl
        };

        return Ok(ApiResponse<NewsDto>.Ok(result, "Tạo tin tức thành công"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<NewsDto>>> UpdateNews(int id, [FromBody] CreateNewsDto dto)
    {
        var news = await _context.News.FindAsync(id);

        if (news == null)
            return NotFound(ApiResponse<NewsDto>.Fail("Không tìm thấy tin tức"));

        news.Title = dto.Title;
        news.Content = dto.Content;
        news.IsPinned = dto.IsPinned;
        news.ImageUrl = dto.ImageUrl;

        await _context.SaveChangesAsync();

        var result = new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            IsPinned = news.IsPinned,
            CreatedDate = news.CreatedDate,
            ImageUrl = news.ImageUrl
        };

        return Ok(ApiResponse<NewsDto>.Ok(result, "Cập nhật tin tức thành công"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNews(int id)
    {
        var news = await _context.News.FindAsync(id);

        if (news == null)
            return NotFound(ApiResponse<bool>.Fail("Không tìm thấy tin tức"));

        _context.News.Remove(news);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Ok(true, "Xóa tin tức thành công"));
    }
}
