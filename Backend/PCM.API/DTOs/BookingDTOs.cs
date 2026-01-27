using System.ComponentModel.DataAnnotations;

namespace PCM.API.DTOs;

// ============ COURT DTOs ============

public class CourtDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public decimal PricePerHour { get; set; }
}

// ============ BOOKING DTOs ============

public class BookingDto
{
    public int Id { get; set; }
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
}

public class CreateBookingDto
{
    [Required]
    public int CourtId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}

public class CreateRecurringBookingDto
{
    [Required]
    public int CourtId { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    public string RecurrenceRule { get; set; } = string.Empty; // VD: "Weekly;Tue,Thu"

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class CalendarSlotDto
{
    public int? BookingId { get; set; }
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty; // "available", "booked", "mine", "holding"
    public string? BookedBy { get; set; }
    public decimal Price { get; set; }
}

public class CalendarQueryDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int? CourtId { get; set; }
}
