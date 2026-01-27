namespace PCM.API.DTOs;

// ============ NOTIFICATION DTOs ============

public class NotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateNotificationDto
{
    public int ReceiverId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string? LinkUrl { get; set; }
}

// ============ NEWS DTOs ============

public class NewsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ImageUrl { get; set; }
}

public class CreateNewsDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public string? ImageUrl { get; set; }
}

// ============ COMMON DTOs ============

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors
    };
}

public class DashboardDto
{
    public decimal WalletBalance { get; set; }
    public double RankLevel { get; set; }
    public int UpcomingBookings { get; set; }
    public int UpcomingMatches { get; set; }
    public int UnreadNotifications { get; set; }
    public List<BookingDto> NextBookings { get; set; } = new();
    public List<MatchDto> NextMatches { get; set; } = new();
    public List<NewsDto> PinnedNews { get; set; } = new();
}

public class AdminDashboardDto
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public decimal TotalRevenue { get; set; }
    public int BookingsThisMonth { get; set; }
    public int ActiveTournaments { get; set; }
    public FinancialReportDto FinancialReport { get; set; } = new();
}
