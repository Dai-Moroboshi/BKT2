using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PCM.API.Data;
using PCM.API.DTOs;
using PCM.API.Entities;
using PCM.API.Hubs;

namespace PCM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<PcmHub> _hubContext;

    public BookingsController(ApplicationDbContext context, IHubContext<PcmHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<ApiResponse<List<CalendarSlotDto>>>> GetCalendar([FromQuery] CalendarQueryDto query)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var bookingsQuery = _context.Bookings
            .Include(b => b.Court)
            .Include(b => b.Member)
            .Where(b => b.StartTime >= query.From && b.EndTime <= query.To)
            .Where(b => b.Status != BookingStatus.Cancelled);

        if (query.CourtId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b => b.CourtId == query.CourtId.Value);
        }

        var bookings = await bookingsQuery.ToListAsync();

        var slots = bookings.Select(b => new CalendarSlotDto
        {
            BookingId = b.Id,
            CourtId = b.CourtId,
            CourtName = b.Court?.Name ?? "",
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            Status = b.MemberId == memberId ? "mine" :
                    b.Status == BookingStatus.Holding ? "holding" : "booked",
            BookedBy = b.Member?.FullName,
            Price = b.TotalPrice
        }).ToList();

        return Ok(ApiResponse<List<CalendarSlotDto>>.Ok(slots));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<List<BookingDto>>>> GetMyBookings()
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var bookings = await _context.Bookings
            .Include(b => b.Court)
            .Where(b => b.MemberId == memberId)
            .OrderByDescending(b => b.StartTime)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                CourtId = b.CourtId,
                CourtName = b.Court!.Name,
                MemberId = b.MemberId,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                IsRecurring = b.IsRecurring,
                RecurrenceRule = b.RecurrenceRule
            })
            .ToListAsync();

        return Ok(ApiResponse<List<BookingDto>>.Ok(bookings));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CreateBooking([FromBody] CreateBookingDto dto)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        var member = await _context.Members.FindAsync(memberId);

        if (member == null)
            return NotFound(ApiResponse<BookingDto>.Fail("Không tìm thấy thành viên"));

        var court = await _context.Courts.FindAsync(dto.CourtId);
        if (court == null)
            return NotFound(ApiResponse<BookingDto>.Fail("Không tìm thấy sân"));

        // Check for overlapping bookings
        var hasOverlap = await _context.Bookings
            .AnyAsync(b => b.CourtId == dto.CourtId &&
                          b.Status != BookingStatus.Cancelled &&
                          b.StartTime < dto.EndTime &&
                          b.EndTime > dto.StartTime);

        if (hasOverlap)
            return BadRequest(ApiResponse<BookingDto>.Fail("Khung giờ này đã có người đặt"));

        // Calculate price
        var hours = (decimal)(dto.EndTime - dto.StartTime).TotalHours;
        var totalPrice = hours * court.PricePerHour;

        // Check wallet balance
        if (member.WalletBalance < totalPrice)
            return BadRequest(ApiResponse<BookingDto>.Fail($"Số dư ví không đủ. Cần {totalPrice:N0}đ, hiện có {member.WalletBalance:N0}đ"));

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create wallet transaction
            var walletTransaction = new WalletTransaction
            {
                MemberId = memberId,
                Amount = -totalPrice,
                Type = TransactionType.Payment,
                Status = TransactionStatus.Completed,
                Description = $"Đặt sân {court.Name} - {dto.StartTime:dd/MM/yyyy HH:mm}",
                CreatedDate = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(walletTransaction);
            await _context.SaveChangesAsync();

            // Create booking
            var booking = new Booking
            {
                CourtId = dto.CourtId,
                MemberId = memberId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TotalPrice = totalPrice,
                TransactionId = walletTransaction.Id,
                Status = BookingStatus.Confirmed,
                CreatedDate = DateTime.UtcNow
            };
            _context.Bookings.Add(booking);

            // Deduct from wallet
            member.WalletBalance -= totalPrice;
            member.TotalSpent += totalPrice;

            // Update tier if needed
            UpdateMemberTier(member);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notify via SignalR
            await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
            {
                courtId = booking.CourtId,
                startTime = booking.StartTime,
                endTime = booking.EndTime,
                status = "booked"
            });

            var result = new BookingDto
            {
                Id = booking.Id,
                CourtId = booking.CourtId,
                CourtName = court.Name,
                MemberId = booking.MemberId,
                MemberName = member.FullName,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status.ToString()
            };

            return Ok(ApiResponse<BookingDto>.Ok(result, "Đặt sân thành công"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<BookingDto>.Fail($"Lỗi: {ex.Message}"));
        }
    }

    [HttpPost("recurring")]
    public async Task<ActionResult<ApiResponse<List<BookingDto>>>> CreateRecurringBooking([FromBody] CreateRecurringBookingDto dto)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        var member = await _context.Members.FindAsync(memberId);

        if (member == null)
            return NotFound(ApiResponse<List<BookingDto>>.Fail("Không tìm thấy thành viên"));

        // Check VIP tier
        if (member.Tier < MemberTier.Gold)
            return BadRequest(ApiResponse<List<BookingDto>>.Fail("Chỉ thành viên VIP (Gold, Diamond) mới được đặt lịch định kỳ"));

        var court = await _context.Courts.FindAsync(dto.CourtId);
        if (court == null)
            return NotFound(ApiResponse<List<BookingDto>>.Fail("Không tìm thấy sân"));

        // Parse recurrence rule (VD: "Weekly;Tue,Thu")
        var ruleParts = dto.RecurrenceRule.Split(';');
        if (ruleParts.Length != 2)
            return BadRequest(ApiResponse<List<BookingDto>>.Fail("Quy tắc lặp không hợp lệ"));

        var daysOfWeek = ruleParts[1].Split(',')
            .Select(d => Enum.Parse<DayOfWeek>(d.Trim(), true))
            .ToList();

        var bookingDates = new List<DateTime>();
        var currentDate = dto.StartDate;

        while (currentDate <= dto.EndDate)
        {
            if (daysOfWeek.Contains(currentDate.DayOfWeek))
            {
                bookingDates.Add(currentDate);
            }
            currentDate = currentDate.AddDays(1);
        }

        // Calculate total price
        var hours = (decimal)(dto.EndTime - dto.StartTime).TotalHours;
        var pricePerBooking = hours * court.PricePerHour;
        var totalPrice = pricePerBooking * bookingDates.Count;

        if (member.WalletBalance < totalPrice)
            return BadRequest(ApiResponse<List<BookingDto>>.Fail($"Số dư ví không đủ. Cần {totalPrice:N0}đ cho {bookingDates.Count} lịch đặt"));

        // Check for overlaps
        foreach (var date in bookingDates)
        {
            var startTime = date.Add(dto.StartTime);
            var endTime = date.Add(dto.EndTime);

            var hasOverlap = await _context.Bookings
                .AnyAsync(b => b.CourtId == dto.CourtId &&
                              b.Status != BookingStatus.Cancelled &&
                              b.StartTime < endTime &&
                              b.EndTime > startTime);

            if (hasOverlap)
                return BadRequest(ApiResponse<List<BookingDto>>.Fail($"Khung giờ ngày {date:dd/MM/yyyy} đã có người đặt"));
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var bookings = new List<Booking>();

            // Create parent booking
            var parentBooking = new Booking
            {
                CourtId = dto.CourtId,
                MemberId = memberId,
                StartTime = bookingDates.First().Add(dto.StartTime),
                EndTime = bookingDates.First().Add(dto.EndTime),
                TotalPrice = totalPrice,
                IsRecurring = true,
                RecurrenceRule = dto.RecurrenceRule,
                Status = BookingStatus.Confirmed,
                CreatedDate = DateTime.UtcNow
            };
            _context.Bookings.Add(parentBooking);
            await _context.SaveChangesAsync();

            bookings.Add(parentBooking);

            // Create child bookings
            foreach (var date in bookingDates.Skip(1))
            {
                var childBooking = new Booking
                {
                    CourtId = dto.CourtId,
                    MemberId = memberId,
                    StartTime = date.Add(dto.StartTime),
                    EndTime = date.Add(dto.EndTime),
                    TotalPrice = pricePerBooking,
                    ParentBookingId = parentBooking.Id,
                    Status = BookingStatus.Confirmed,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Bookings.Add(childBooking);
                bookings.Add(childBooking);
            }

            // Create wallet transaction
            var walletTransaction = new WalletTransaction
            {
                MemberId = memberId,
                Amount = -totalPrice,
                Type = TransactionType.Payment,
                Status = TransactionStatus.Completed,
                Description = $"Đặt sân định kỳ {court.Name} - {bookingDates.Count} lịch",
                RelatedId = parentBooking.Id.ToString(),
                CreatedDate = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(walletTransaction);

            // Deduct from wallet
            member.WalletBalance -= totalPrice;
            member.TotalSpent += totalPrice;
            UpdateMemberTier(member);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = bookings.Select(b => new BookingDto
            {
                Id = b.Id,
                CourtId = b.CourtId,
                CourtName = court.Name,
                MemberId = b.MemberId,
                MemberName = member.FullName,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                IsRecurring = b.IsRecurring,
                RecurrenceRule = b.RecurrenceRule
            }).ToList();

            return Ok(ApiResponse<List<BookingDto>>.Ok(result, $"Đã tạo {bookings.Count} lịch đặt sân định kỳ"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<List<BookingDto>>.Fail($"Lỗi: {ex.Message}"));
        }
    }

    [HttpPost("cancel/{id}")]
    public async Task<ActionResult<ApiResponse<BookingDto>>> CancelBooking(int id)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");

        var booking = await _context.Bookings
            .Include(b => b.Court)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound(ApiResponse<BookingDto>.Fail("Không tìm thấy lịch đặt"));

        if (booking.MemberId != memberId)
            return Forbid();

        if (booking.Status == BookingStatus.Cancelled)
            return BadRequest(ApiResponse<BookingDto>.Fail("Lịch đặt đã được hủy trước đó"));

        if (booking.Status == BookingStatus.Completed)
            return BadRequest(ApiResponse<BookingDto>.Fail("Không thể hủy lịch đã hoàn thành"));

        // Calculate refund (100% if > 24h before, 50% if 12-24h, 0% if < 12h)
        var hoursUntilStart = (booking.StartTime - DateTime.UtcNow).TotalHours;
        decimal refundPercentage = hoursUntilStart switch
        {
            > 24 => 1.0m,
            > 12 => 0.5m,
            _ => 0m
        };

        var refundAmount = booking.TotalPrice * refundPercentage;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            booking.Status = BookingStatus.Cancelled;

            if (refundAmount > 0 && booking.Member != null)
            {
                // Create refund transaction
                var refundTransaction = new WalletTransaction
                {
                    MemberId = memberId,
                    Amount = refundAmount,
                    Type = TransactionType.Refund,
                    Status = TransactionStatus.Completed,
                    Description = $"Hoàn tiền hủy sân {booking.Court?.Name} ({refundPercentage * 100}%)",
                    RelatedId = booking.Id.ToString(),
                    CreatedDate = DateTime.UtcNow
                };
                _context.WalletTransactions.Add(refundTransaction);

                booking.Member.WalletBalance += refundAmount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Notify via SignalR
            await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
            {
                courtId = booking.CourtId,
                startTime = booking.StartTime,
                endTime = booking.EndTime,
                status = "available"
            });

            var result = new BookingDto
            {
                Id = booking.Id,
                CourtId = booking.CourtId,
                CourtName = booking.Court?.Name ?? "",
                MemberId = booking.MemberId,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status.ToString()
            };

            var message = refundAmount > 0
                ? $"Đã hủy lịch đặt và hoàn {refundAmount:N0}đ ({refundPercentage * 100}%)"
                : "Đã hủy lịch đặt (không hoàn tiền do hủy sát giờ)";

            return Ok(ApiResponse<BookingDto>.Ok(result, message));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<BookingDto>.Fail($"Lỗi: {ex.Message}"));
        }
    }

    private void UpdateMemberTier(Member member)
    {
        member.Tier = member.TotalSpent switch
        {
            >= 10000000 => MemberTier.Diamond,
            >= 5000000 => MemberTier.Gold,
            >= 2000000 => MemberTier.Silver,
            _ => MemberTier.Standard
        };
    }
}
