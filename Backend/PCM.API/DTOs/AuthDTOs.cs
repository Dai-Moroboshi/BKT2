using System.ComponentModel.DataAnnotations;

namespace PCM.API.DTOs;

// ============ AUTH DTOs ============

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public decimal WalletBalance { get; set; }
    public string Tier { get; set; } = string.Empty;
    public double RankLevel { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UserInfoDto
{
    public int MemberId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; }
    public string Tier { get; set; } = string.Empty;
    public double RankLevel { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime JoinDate { get; set; }
    public decimal TotalSpent { get; set; }
    public int UnreadNotifications { get; set; }
}
