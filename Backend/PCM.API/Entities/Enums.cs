namespace PCM.API.Entities;

// Member Tier
public enum MemberTier
{
    Standard = 0,
    Silver = 1,
    Gold = 2,
    Diamond = 3
}

// Wallet Transaction Type
public enum TransactionType
{
    Deposit = 0,      // Nạp tiền
    Withdraw = 1,     // Rút tiền
    Payment = 2,      // Thanh toán
    Refund = 3,       // Hoàn tiền
    Reward = 4        // Thưởng giải
}

// Transaction Status
public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Rejected = 2,
    Failed = 3
}

// Booking Status
public enum BookingStatus
{
    PendingPayment = 0,   // Chờ thanh toán
    Confirmed = 1,        // Đã đặt
    Cancelled = 2,        // Đã hủy
    Completed = 3,        // Đã hoàn thành
    Holding = 4           // Đang giữ chỗ
}

// Tournament Format
public enum TournamentFormat
{
    RoundRobin = 0,       // Vòng tròn
    Knockout = 1,         // Loại trực tiếp
    Hybrid = 2            // Kết hợp
}

// Tournament Status
public enum TournamentStatus
{
    Open = 0,
    Registering = 1,
    DrawCompleted = 2,    // Đã bốc thăm
    Ongoing = 3,
    Finished = 4
}

// Match Status
public enum MatchStatus
{
    Scheduled = 0,
    InProgress = 1,
    Finished = 2
}

// Match Winning Side
public enum WinningSide
{
    None = 0,
    Team1 = 1,
    Team2 = 2
}

// Notification Type
public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2
}

// Category Type (Thu/Chi)
public enum CategoryType
{
    Income = 0,    // Thu
    Expense = 1    // Chi
}
