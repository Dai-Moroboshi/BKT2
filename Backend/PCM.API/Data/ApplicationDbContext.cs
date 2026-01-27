using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PCM.API.Entities;

namespace PCM.API.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets with prefix 272
    public DbSet<Member> Members { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<TransactionCategory> TransactionCategories { get; set; }
    public DbSet<Court> Courts { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Tournament> Tournaments { get; set; }
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Member - User relationship
        builder.Entity<Member>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // WalletTransaction - Member relationship
        builder.Entity<WalletTransaction>()
            .HasOne(w => w.Member)
            .WithMany(m => m.WalletTransactions)
            .HasForeignKey(w => w.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Booking - Court relationship
        builder.Entity<Booking>()
            .HasOne(b => b.Court)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking - Member relationship
        builder.Entity<Booking>()
            .HasOne(b => b.Member)
            .WithMany(m => m.Bookings)
            .HasForeignKey(b => b.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Booking - Parent Booking (self-referencing)
        builder.Entity<Booking>()
            .HasOne(b => b.ParentBooking)
            .WithMany(b => b.ChildBookings)
            .HasForeignKey(b => b.ParentBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // TournamentParticipant relationships
        builder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(tp => tp.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Member)
            .WithMany(m => m.TournamentParticipations)
            .HasForeignKey(tp => tp.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Match relationships
        builder.Entity<Match>()
            .HasOne(m => m.Tournament)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Match>()
            .HasOne(m => m.Team1_Player1)
            .WithMany()
            .HasForeignKey(m => m.Team1_Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.Team1_Player2)
            .WithMany()
            .HasForeignKey(m => m.Team1_Player2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.Team2_Player1)
            .WithMany()
            .HasForeignKey(m => m.Team2_Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Match>()
            .HasOne(m => m.Team2_Player2)
            .WithMany()
            .HasForeignKey(m => m.Team2_Player2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification - Member relationship
        builder.Entity<Notification>()
            .HasOne(n => n.Receiver)
            .WithMany(m => m.Notifications)
            .HasForeignKey(n => n.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.Entity<Member>()
            .HasIndex(m => m.UserId)
            .IsUnique();

        builder.Entity<Booking>()
            .HasIndex(b => new { b.CourtId, b.StartTime, b.EndTime });

        builder.Entity<WalletTransaction>()
            .HasIndex(w => w.MemberId);

        builder.Entity<Notification>()
            .HasIndex(n => new { n.ReceiverId, n.IsRead });
    }
}
