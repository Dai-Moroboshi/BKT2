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
public class WalletController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<PcmHub> _hubContext;

    public WalletController(ApplicationDbContext context, IHubContext<PcmHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<WalletSummaryDto>>> GetBalance()
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        var member = await _context.Members.FindAsync(memberId);

        if (member == null)
            return NotFound(ApiResponse<WalletSummaryDto>.Fail("Không tìm thấy thành viên"));

        var transactions = await _context.WalletTransactions
            .Where(t => t.MemberId == memberId && t.Status == TransactionStatus.Completed)
            .ToListAsync();

        var summary = new WalletSummaryDto
        {
            Balance = member.WalletBalance,
            TotalDeposit = transactions.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Amount),
            TotalSpent = Math.Abs(transactions.Where(t => t.Type == TransactionType.Payment).Sum(t => t.Amount)),
            TotalReward = transactions.Where(t => t.Type == TransactionType.Reward).Sum(t => t.Amount),
            PendingTransactions = await _context.WalletTransactions
                .CountAsync(t => t.MemberId == memberId && t.Status == TransactionStatus.Pending)
        };

        return Ok(ApiResponse<WalletSummaryDto>.Ok(summary));
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<ApiResponse<WalletTransactionDto>>> Deposit([FromBody] DepositRequestDto dto)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        var member = await _context.Members.FindAsync(memberId);

        if (member == null)
            return NotFound(ApiResponse<WalletTransactionDto>.Fail("Không tìm thấy thành viên"));

        // AUTO-APPROVE LOGIC
        member.WalletBalance += dto.Amount;

        var methodMap = new Dictionary<string, string>
        {
            { "Cash", "Tiền mặt" },
            { "BankTransfer", "Chuyển khoản" },
            { "USDT", "USDT" },
            { "EWallet", "Ví điện tử" }
        };
        var methodText = methodMap.ContainsKey(dto.PaymentMethod) ? methodMap[dto.PaymentMethod] : dto.PaymentMethod;

        var transaction = new WalletTransaction
        {
            MemberId = memberId,
            Amount = dto.Amount,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed, // Instant approval
            Description = dto.Description ?? $"Nạp tiền qua {methodText}",
            ProofImageUrl = dto.ProofImageUrl,
            CreatedDate = DateTime.UtcNow
        };

        _context.WalletTransactions.Add(transaction);
        
        // Notification
        var notification = new Notification
        {
            ReceiverId = memberId,
            Message = $"Nạp tiền thành công: {dto.Amount:N0}đ qua {methodText}",
            Type = NotificationType.Success,
            CreatedDate = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        // SignalR Update
        await _hubContext.Clients.User(member.UserId)
            .SendAsync("WalletUpdated", member.WalletBalance);

        await _hubContext.Clients.User(member.UserId)
            .SendAsync("ReceiveNotification", new
            {
                message = notification.Message,
                type = "Success",
                balance = member.WalletBalance
            });

        var result = new WalletTransactionDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            Description = transaction.Description,
            CreatedDate = transaction.CreatedDate
        };

        return Ok(ApiResponse<WalletTransactionDto>.Ok(result, "Nạp tiền thành công! Số dư đã được cập nhật."));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<WalletTransactionDto>>>> GetTransactions(
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var memberId = int.Parse(User.FindFirstValue("MemberId") ?? "0");
        
        var query = _context.WalletTransactions
            .Where(t => t.MemberId == memberId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, out var typeEnum))
        {
            query = query.Where(t => t.Type == typeEnum);
        }

        var totalCount = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(t => t.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                RelatedId = t.RelatedId,
                Description = t.Description,
                CreatedDate = t.CreatedDate
            })
            .ToListAsync();

        var result = new PaginatedResult<WalletTransactionDto>
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<WalletTransactionDto>>.Ok(result));
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Treasurer")]
    public async Task<ActionResult<ApiResponse<List<WalletTransactionDto>>>> GetPendingTransactions()
    {
        var transactions = await _context.WalletTransactions
            .Include(t => t.Member)
            .Where(t => t.Status == TransactionStatus.Pending)
            .OrderBy(t => t.CreatedDate)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                RelatedId = t.Member != null ? t.Member.FullName : null,
                Description = t.Description,
                CreatedDate = t.CreatedDate
            })
            .ToListAsync();

        return Ok(ApiResponse<List<WalletTransactionDto>>.Ok(transactions));
    }

    [HttpPut("approve/{transactionId}")]
    [Authorize(Roles = "Admin,Treasurer")]
    public async Task<ActionResult<ApiResponse<WalletTransactionDto>>> ApproveTransaction(
        int transactionId,
        [FromBody] ApproveTransactionDto dto)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var transaction = await _context.WalletTransactions
                .Include(t => t.Member)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                return NotFound(ApiResponse<WalletTransactionDto>.Fail("Không tìm thấy giao dịch"));

            if (transaction.Status != TransactionStatus.Pending)
                return BadRequest(ApiResponse<WalletTransactionDto>.Fail("Giao dịch này đã được xử lý"));

            if (dto.IsApproved)
            {
                transaction.Status = TransactionStatus.Completed;
                
                // Cộng tiền vào ví
                if (transaction.Member != null)
                {
                    transaction.Member.WalletBalance += transaction.Amount;
                }

                // Tạo notification
                var notification = new Notification
                {
                    ReceiverId = transaction.MemberId,
                    Message = $"Nạp tiền thành công: {transaction.Amount:N0}đ",
                    Type = NotificationType.Success,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                // Gửi SignalR notification
                await _hubContext.Clients.User(transaction.Member?.UserId ?? "")
                    .SendAsync("ReceiveNotification", new
                    {
                        message = notification.Message,
                        type = "Success",
                        balance = transaction.Member?.WalletBalance
                    });
            }
            else
            {
                transaction.Status = TransactionStatus.Rejected;
                transaction.Description += $" | Từ chối: {dto.Note}";

                var notification = new Notification
                {
                    ReceiverId = transaction.MemberId,
                    Message = $"Yêu cầu nạp tiền bị từ chối: {dto.Note}",
                    Type = NotificationType.Warning,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            var result = new WalletTransactionDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                CreatedDate = transaction.CreatedDate
            };

            return Ok(ApiResponse<WalletTransactionDto>.Ok(result,
                dto.IsApproved ? "Đã duyệt nạp tiền thành công" : "Đã từ chối yêu cầu nạp tiền"));
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return StatusCode(500, ApiResponse<WalletTransactionDto>.Fail($"Lỗi: {ex.Message}"));
        }
    }

    [HttpGet("report")]
    [Authorize(Roles = "Admin,Treasurer")]
    public async Task<ActionResult<ApiResponse<FinancialReportDto>>> GetFinancialReport()
    {
        var transactions = await _context.WalletTransactions
            .Where(t => t.Status == TransactionStatus.Completed)
            .ToListAsync();

        var income = transactions
            .Where(t => t.Type == TransactionType.Deposit || t.Type == TransactionType.Payment)
            .Sum(t => Math.Abs(t.Amount));

        var expense = transactions
            .Where(t => t.Type == TransactionType.Refund || t.Type == TransactionType.Reward)
            .Sum(t => Math.Abs(t.Amount));

        // Monthly data for last 6 months
        var monthlyData = transactions
            .Where(t => t.CreatedDate >= DateTime.UtcNow.AddMonths(-6))
            .GroupBy(t => new { t.CreatedDate.Year, t.CreatedDate.Month })
            .Select(g => new MonthlyReportDto
            {
                Month = $"{g.Key.Month}/{g.Key.Year}",
                Income = g.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Amount),
                Expense = Math.Abs(g.Where(t => t.Type == TransactionType.Refund || t.Type == TransactionType.Reward)
                    .Sum(t => t.Amount))
            })
            .OrderBy(m => m.Month)
            .ToList();

        var report = new FinancialReportDto
        {
            TotalIncome = income,
            TotalExpense = expense,
            Balance = income - expense,
            MonthlyData = monthlyData
        };

        return Ok(ApiResponse<FinancialReportDto>.Ok(report));
    }
}
