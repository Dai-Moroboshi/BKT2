using System.ComponentModel.DataAnnotations;
using PCM.API.Entities;

namespace PCM.API.DTOs;

// ============ WALLET DTOs ============

public class DepositRequestDto
{
    [Required]
    [Range(10000, 100000000)]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Base64 hoặc URL ảnh chứng minh chuyển khoản
    public string? ProofImageUrl { get; set; }

    public string PaymentMethod { get; set; } = "BankTransfer"; // Default
}

public class WalletTransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RelatedId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class WalletSummaryDto
{
    public decimal Balance { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalReward { get; set; }
    public int PendingTransactions { get; set; }
}

public class ApproveTransactionDto
{
    public bool IsApproved { get; set; }
    public string? Note { get; set; }
}

// For Admin Dashboard
public class FinancialReportDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public List<MonthlyReportDto> MonthlyData { get; set; } = new();
}

public class MonthlyReportDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}
