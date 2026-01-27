using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_Bookings")]
public class Booking
{
    [Key]
    public int Id { get; set; }

    // FK to Court
    public int CourtId { get; set; }

    [ForeignKey("CourtId")]
    public virtual Court? Court { get; set; }

    // FK to Member
    public int MemberId { get; set; }

    [ForeignKey("MemberId")]
    public virtual Member? Member { get; set; }

    // Thời gian đặt
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Tổng tiền thanh toán
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    // FK to WalletTransaction (giao dịch trừ tiền)
    public int? TransactionId { get; set; }

    [ForeignKey("TransactionId")]
    public virtual WalletTransaction? Transaction { get; set; }

    // Đặt lịch lặp
    public bool IsRecurring { get; set; } = false;

    // Quy tắc lặp (VD: "Weekly;Tue,Thu")
    [MaxLength(100)]
    public string? RecurrenceRule { get; set; }

    // FK to Parent Booking (nếu là booking con từ lịch lặp)
    public int? ParentBookingId { get; set; }

    [ForeignKey("ParentBookingId")]
    public virtual Booking? ParentBooking { get; set; }

    // Trạng thái
    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

    // Thời điểm giữ chỗ (để auto-cancel sau 5 phút)
    public DateTime? HoldStartTime { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<Booking> ChildBookings { get; set; } = new List<Booking>();
}
