using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PCM.API.Data;
using PCM.API.DTOs;
using PCM.API.Entities;

namespace PCM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Email hoặc mật khẩu không đúng"));

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Email hoặc mật khẩu không đúng"));

        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null)
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Không tìm thấy thông tin thành viên"));

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles, member);

        var response = new AuthResponseDto
        {
            Token = token,
            Email = user.Email!,
            FullName = member.FullName,
            Role = roles.FirstOrDefault() ?? "Member",
            MemberId = member.Id,
            WalletBalance = member.WalletBalance,
            Tier = member.Tier.ToString(),
            RankLevel = member.RankLevel,
            AvatarUrl = member.AvatarUrl
        };

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Đăng nhập thành công"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Email đã được sử dụng"));

        var user = new IdentityUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<AuthResponseDto>.Fail("Đăng ký thất bại", errors));
        }

        await _userManager.AddToRoleAsync(user, "Member");

        var member = new Member
        {
            FullName = dto.FullName,
            UserId = user.Id,
            JoinDate = DateTime.UtcNow,
            WalletBalance = 0,
            Tier = MemberTier.Standard,
            RankLevel = 3.0
        };

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var roles = new[] { "Member" };
        var token = GenerateJwtToken(user, roles, member);

        var response = new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            FullName = member.FullName,
            Role = "Member",
            MemberId = member.Id,
            WalletBalance = member.WalletBalance,
            Tier = member.Tier.ToString(),
            RankLevel = member.RankLevel
        };

        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Đăng ký thành công"));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserInfoDto>>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<UserInfoDto>.Fail("Không tìm thấy thông tin người dùng"));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<UserInfoDto>.Fail("Không tìm thấy người dùng"));

        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member == null)
            return NotFound(ApiResponse<UserInfoDto>.Fail("Không tìm thấy thông tin thành viên"));

        var roles = await _userManager.GetRolesAsync(user);
        var unreadCount = await _context.Notifications
            .CountAsync(n => n.ReceiverId == member.Id && !n.IsRead);

        var response = new UserInfoDto
        {
            MemberId = member.Id,
            Email = user.Email!,
            FullName = member.FullName,
            Role = roles.FirstOrDefault() ?? "Member",
            WalletBalance = member.WalletBalance,
            Tier = member.Tier.ToString(),
            RankLevel = member.RankLevel,
            AvatarUrl = member.AvatarUrl,
            JoinDate = member.JoinDate,
            TotalSpent = member.TotalSpent,
            UnreadNotifications = unreadCount
        };

        return Ok(ApiResponse<UserInfoDto>.Ok(response));
    }

    private string GenerateJwtToken(IdentityUser user, IList<string> roles, Member member)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "YourSuperSecretKeyForPCMApplication2026!@#$%"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, member.FullName),
            new("MemberId", member.Id.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "PCM.API",
            audience: _configuration["Jwt:Audience"] ?? "PCM.Mobile",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
