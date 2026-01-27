using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCM.API.Entities;

[Table("272_WalletTransactions")]
public class WalletTransaction
{
    [Key]
    public int Id { get; set; }

    // FK to Member
    public int MemberId { get; set; }

    [ForeignKey("MemberId")]
    public virtual Member? Member { get; set; }

    // Số tiền (+ cho nạp/thưởng, - cho thanh toán/rút)
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    // Loại giao dịch
    public TransactionType Type { get; set; }

    // Trạng thái
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    // ID liên quan (Booking hoặc Tournament)
    [MaxLength(50)]
    public string? RelatedId { get; set; }

    // Mô tả
    [MaxLength(500)]
    public string? Description { get; set; }

    // Ảnh chứng minh (cho nạp tiền)
    [MaxLength(500)]
    public string? ProofImageUrl { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
