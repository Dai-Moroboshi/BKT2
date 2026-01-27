using Microsoft.AspNetCore.Identity;
using PCM.API.Entities;

namespace PCM.API.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager, context);
        await SeedCourtsAsync(context);
        await SeedTransactionCategoriesAsync(context);
        await SeedTournamentsAsync(context);
        await SeedNewsAsync(context);
        await SeedBookingsAsync(context);
        await SeedNotificationsAsync(context);

        await context.SaveChangesAsync();
        Console.WriteLine("Database seeded successfully!");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "Treasurer", "Referee", "Member" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedUsersAsync(UserManager<IdentityUser> userManager, ApplicationDbContext context)
    {
        if (context.Members.Any()) return;

        var random = new Random(42);

        // Admin
        await CreateUserAndMemberAsync(userManager, context,
            "admin@pcm.com", "Admin@123", "Admin",
            "Nguyễn Văn Admin", 5.0, MemberTier.Diamond, 15000000);

        // Treasurer
        await CreateUserAndMemberAsync(userManager, context,
            "treasurer@pcm.com", "Treasurer@123", "Treasurer",
            "Trần Thị Thủ Quỹ", 4.5, MemberTier.Gold, 8500000);

        // Referee
        await CreateUserAndMemberAsync(userManager, context,
            "referee@pcm.com", "Referee@123", "Referee",
            "Lê Văn Trọng Tài", 4.2, MemberTier.Silver, 5200000);

        // 20 Regular Members with Vietnamese names
        var members = new[]
        {
            ("Nguyễn Văn An", 4.8, MemberTier.Diamond, 12500000m),
            ("Trần Thị Bình", 4.5, MemberTier.Gold, 8700000m),
            ("Phạm Đức Cường", 4.2, MemberTier.Gold, 6500000m),
            ("Hoàng Minh Dũng", 3.9, MemberTier.Silver, 4800000m),
            ("Lê Thanh Hùng", 3.7, MemberTier.Silver, 4200000m),
            ("Vũ Quốc Khoa", 3.5, MemberTier.Silver, 3800000m),
            ("Đặng Anh Long", 4.0, MemberTier.Gold, 5500000m),
            ("Bùi Văn Nam", 3.3, MemberTier.Standard, 2800000m),
            ("Nguyễn Hoàng Phong", 3.6, MemberTier.Silver, 4100000m),
            ("Trần Quốc Quang", 3.4, MemberTier.Standard, 3200000m),
            ("Phạm Đức Sơn", 3.8, MemberTier.Silver, 4500000m),
            ("Hoàng Thanh Tâm", 3.2, MemberTier.Standard, 2500000m),
            ("Lê Minh Tuấn", 4.1, MemberTier.Gold, 5800000m),
            ("Vũ Anh Việt", 3.5, MemberTier.Silver, 3900000m),
            ("Đặng Hoàng Xuân", 3.0, MemberTier.Standard, 2200000m),
            ("Nguyễn Thị Yến", 3.8, MemberTier.Silver, 4600000m),
            ("Trần Thị Hà", 3.4, MemberTier.Standard, 3100000m),
            ("Phạm Thị Lan", 4.3, MemberTier.Gold, 6200000m),
            ("Hoàng Thị Mai", 3.6, MemberTier.Silver, 4000000m),
            ("Lê Thị Thảo", 3.1, MemberTier.Standard, 2400000m)
        };

        for (int i = 0; i < members.Length; i++)
        {
            var (name, rank, tier, balance) = members[i];
            await CreateUserAndMemberAsync(userManager, context,
                $"member{i + 1}@pcm.com", "Member@123", "Member",
                name, rank, tier, balance);
        }
    }

    private static async Task<Member> CreateUserAndMemberAsync(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context,
        string email, string password, string role,
        string fullName, double rankLevel, MemberTier tier, decimal walletBalance)
    {
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);

            var random = new Random();
            var member = new Member
            {
                FullName = fullName,
                UserId = user.Id,
                RankLevel = rankLevel,
                Tier = tier,
                WalletBalance = walletBalance,
                TotalSpent = walletBalance * 0.4m,
                JoinDate = DateTime.UtcNow.AddDays(-random.Next(30, 365))
            };

            context.Members.Add(member);
            await context.SaveChangesAsync();

            // Initial deposit
            context.WalletTransactions.Add(new WalletTransaction
            {
                MemberId = member.Id,
                Amount = walletBalance + (walletBalance * 0.4m),
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = "Nạp tiền lần đầu",
                CreatedDate = member.JoinDate
            });

            // Some spending transactions
            for (int i = 0; i < 3; i++)
            {
                context.WalletTransactions.Add(new WalletTransaction
                {
                    MemberId = member.Id,
                    Amount = -(50000 + random.Next(150000)),
                    Type = TransactionType.Payment,
                    Status = TransactionStatus.Completed,
                    Description = $"Thanh toán đặt sân #{random.Next(1000, 9999)}",
                    CreatedDate = member.JoinDate.AddDays(random.Next(1, 100))
                });
            }

            return member;
        }
        throw new Exception($"Failed to create user {email}");
    }

    private static async Task SeedCourtsAsync(ApplicationDbContext context)
    {
        if (context.Courts.Any()) return;

        context.Courts.AddRange(new List<Court>
        {
            new() { Name = "Sân 1 - Indoor", Description = "Sân trong nhà, có điều hòa, mặt sân nhựa tổng hợp", PricePerHour = 180000, IsActive = true },
            new() { Name = "Sân 2 - Indoor", Description = "Sân trong nhà, có điều hòa, mặt sân nhựa tổng hợp", PricePerHour = 180000, IsActive = true },
            new() { Name = "Sân 3 - Mái che", Description = "Sân có mái che, thông thoáng, mặt sân xi măng", PricePerHour = 120000, IsActive = true },
            new() { Name = "Sân 4 - Mái che", Description = "Sân có mái che, thông thoáng, mặt sân xi măng", PricePerHour = 120000, IsActive = true },
            new() { Name = "Sân 5 - Ngoài trời", Description = "Sân ngoài trời, có đèn chiếu sáng", PricePerHour = 80000, IsActive = true },
            new() { Name = "Sân VIP", Description = "Sân VIP, điều hòa, phòng chờ riêng, nước uống miễn phí", PricePerHour = 300000, IsActive = true }
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedTransactionCategoriesAsync(ApplicationDbContext context)
    {
        if (context.TransactionCategories.Any()) return;

        context.TransactionCategories.AddRange(new List<TransactionCategory>
        {
            new() { Name = "Phí đặt sân", Type = CategoryType.Income },
            new() { Name = "Phí tham gia giải đấu", Type = CategoryType.Income },
            new() { Name = "Nạp tiền thành viên", Type = CategoryType.Income },
            new() { Name = "Phí thành viên hàng tháng", Type = CategoryType.Income },
            new() { Name = "Tiền thưởng giải đấu", Type = CategoryType.Expense },
            new() { Name = "Hoàn tiền hủy sân", Type = CategoryType.Expense },
            new() { Name = "Chi phí bảo trì sân", Type = CategoryType.Expense },
            new() { Name = "Chi phí tổ chức giải", Type = CategoryType.Expense },
            new() { Name = "Mua vợt, bóng", Type = CategoryType.Expense }
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedTournamentsAsync(ApplicationDbContext context)
    {
        if (context.Tournaments.Any()) return;

        var now = DateTime.UtcNow.Date;
        var tournaments = new List<Tournament>();
        var random = new Random(42);

        // 1. Manual Highlight Tournaments (High Quality)
        tournaments.Add(new Tournament
        {
            Name = "Winter Championship 2026",
            StartDate = now.AddDays(14),
            EndDate = now.AddDays(30),
            Format = TournamentFormat.Hybrid,
            EntryFee = 350000,
            PrizePool = 20000000,
            Status = TournamentStatus.Registering,
            Description = "Giải vô địch mùa đông - Giải đấu lớn nhất năm với tổng giải thưởng 20 triệu đồng. Đăng ký ngay!",
            Settings = "{\"maxTeams\": 32, \"groups\": 8, \"advancePerGroup\": 2}"
        });

        // 2. Procedural Generation for 100+ Tournaments
        var locations = new[] { "Thôn 1", "Thôn 4", "Tổ 5", "Phường Yên Thế", "Xã Biển Hồ", "Huyện Chư Păh", "Pleiku", "Gia Lai", "Ia Grai", "Đak Đoa" };
        var types = new[] { "Mở Rộng", "Giao Hữu", "Tranh Cúp", "Vô Địch", "Thanh Niên", "Lão Tướng", "Mùa Xuân", "Mùa Hè" };
        var sponsors = new[] { "Bia Sài Gòn", "Phở Khô", "Cà Phê Ngon", "Vợt Pro", "Sport Center" };

        for (int i = 0; i < 110; i++)
        {
            var loc = locations[random.Next(locations.Length)];
            var type = types[random.Next(types.Length)];
            var sponsor = random.NextDouble() > 0.7 ? $" - Tài trợ bởi {sponsors[random.Next(sponsors.Length)]}" : "";
            
            // Random days offset (Past 2 years to Future 6 months)
            var daysOffset = random.Next(-700, 180);
            var startDate = now.AddDays(daysOffset);
            var duration = random.Next(2, 10);
            
            TournamentStatus status;
            if (daysOffset < -duration) status = TournamentStatus.Finished;
            else if (daysOffset <= 0 && daysOffset >= -duration) status = TournamentStatus.Ongoing;
            else if (daysOffset < 14) status = TournamentStatus.Registering;
            else status = TournamentStatus.Open;

            tournaments.Add(new Tournament
            {
                Name = $"Giải Pickleball {loc} {type} {startDate.Year}{sponsor}",
                StartDate = startDate,
                EndDate = startDate.AddDays(duration),
                Format = (TournamentFormat)random.Next(0, 3),
                EntryFee = (random.Next(1, 10) * 50000), // 50k to 500k
                PrizePool = (random.Next(1, 20) * 1000000), // 1M to 20M
                Status = status,
                Description = $"Giải đấu phong trào tổ chức tại {loc}. Quy tụ các tay vợt xuất sắc trong khu vực.",
                Settings = $"{{\"maxTeams\": {Math.Pow(2, random.Next(3, 7))}}}" // 8, 16, 32, 64
            });
        }

        context.Tournaments.AddRange(tournaments);
        await context.SaveChangesAsync();

        // 3. Add Participants & Matches
        var members = context.Members.ToList();
        if (!members.Any()) return;

        foreach (var tournament in tournaments)
        {
            // Almost all tournaments should have some participants
            if (tournament.Status == TournamentStatus.Open && random.NextDouble() > 0.8) continue;

            int maxParticipants = 32;
            int participantCount = random.Next(8, maxParticipants); // Ensure at least 8 for playoffs
            var tournamentParticipants = new List<TournamentParticipant>();

            for (int k = 0; k < participantCount; k++)
            {
                var member = members[random.Next(members.Count)];
                
                if (context.TournamentParticipants.Local.Any(p => p.TournamentId == tournament.Id && p.MemberId == member.Id) || 
                    tournamentParticipants.Any(p => p.MemberId == member.Id)) continue;
                
                var p = new TournamentParticipant
                {
                    TournamentId = tournament.Id,
                    MemberId = member.Id,
                    TeamName = random.NextDouble() > 0.5 ? member.FullName : $"Team {k + 1}",
                    PaymentStatus = random.NextDouble() > 0.1 || tournament.Status == TournamentStatus.Finished,
                    JoinedDate = tournament.StartDate.AddDays(-random.Next(5, 20)),
                    Seed = random.NextDouble() > 0.9 ? random.Next(1, 4) : null
                };
                tournamentParticipants.Add(p);
                context.TournamentParticipants.Add(p);
            }

            // Generate Matches for Finished/Ongoing tournaments
            if (tournament.Status == TournamentStatus.Finished || tournament.Status == TournamentStatus.Ongoing)
            {
                 // Create fake matches
                 if (tournamentParticipants.Count >= 4)
                 {
                     var rounds = new[] { "Vòng Loại", "Tứ Kết", "Bán Kết", "Chung Kết" };
                     int matchCount = random.Next(5, 15);
                     
                     for(int m = 0; m < matchCount; m++)
                     {
                         var p1 = tournamentParticipants[random.Next(tournamentParticipants.Count)];
                         var p2 = tournamentParticipants[random.Next(tournamentParticipants.Count)];
                         if (p1 == p2) continue;

                         var matchDate = tournament.StartDate.AddDays(random.Next(0, (tournament.EndDate - tournament.StartDate).Days));
                         var hour = random.Next(7, 20); // 7 AM to 8 PM
                         
                         var isFinished = tournament.Status == TournamentStatus.Finished || (tournament.Status == TournamentStatus.Ongoing && matchDate < now);
                         
                         context.Matches.Add(new Match
                         {
                             TournamentId = tournament.Id,
                             RoundName = rounds[random.Next(rounds.Length)],
                             Date = matchDate,
                             StartTime = TimeSpan.FromHours(hour),
                             Team1_Player1Id = p1.MemberId,
                             Team2_Player1Id = p2.MemberId,
                             Status = isFinished ? MatchStatus.Finished : MatchStatus.Scheduled,
                             Score1 = isFinished ? random.Next(0, 3) : 0,
                             Score2 = isFinished ? random.Next(0, 3) : 0,
                             WinningSide = isFinished ? (WinningSide)random.Next(1, 3) : WinningSide.None
                         });
                     }
                 }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedNewsAsync(ApplicationDbContext context)
    {
        if (context.News.Any()) return;

        var now = DateTime.UtcNow;
        context.News.AddRange(new List<News>
        {
            new()
            {
                Title = "🎉 Chào mừng đến CLB Vợt Thủ Phố Núi!",
                Content = "Chào mừng tất cả thành viên đến với CLB Pickleball Vợt Thủ Phố Núi! Đây là nơi giao lưu, rèn luyện sức khỏe và kết nối cộng đồng yêu thích Pickleball tại Pleiku. Hãy cùng nhau tạo nên những trận đấu đáng nhớ!",
                IsPinned = true,
                CreatedDate = now.AddDays(-90)
            },
            new()
            {
                Title = "🏆 Winter Championship 2026 - Mở đăng ký!",
                Content = "Giải vô địch mùa đông 2026 đã chính thức mở đăng ký!\n\n📅 Thời gian: 14/02 - 28/02/2026\n💰 Phí tham gia: 350,000đ\n🏆 Tổng giải thưởng: 15,000,000đ\n\nĐây là giải đấu lớn nhất năm với sự tham gia của 32 đội. Hãy nhanh tay đăng ký để không bỏ lỡ cơ hội giành giải thưởng lớn!",
                IsPinned = true,
                CreatedDate = now.AddDays(-3)
            },
            new()
            {
                Title = "📢 Thông báo: Nâng cấp sân 1 và sân 2",
                Content = "CLB sẽ tiến hành nâng cấp mặt sân 1 và sân 2 với vật liệu mới, chất lượng cao hơn. Thời gian dự kiến hoàn thành: 1 tuần. Trong thời gian này, các bạn vui lòng đặt các sân còn lại. Xin lỗi vì sự bất tiện này!",
                IsPinned = false,
                CreatedDate = now.AddDays(-7)
            },
            new()
            {
                Title = "🎖️ Kết quả Giải Mở Rộng Mùa Hè 2026",
                Content = "Xin chúc mừng các vận động viên đã hoàn thành xuất sắc Giải Mở Rộng Mùa Hè 2026!\n\n🥇 Vô địch: Team An-Bình\n🥈 Á quân: Team Cường-Dũng\n🥉 Hạng 3: Team Hùng-Khoa\n\nCảm ơn tất cả các đội đã tham gia và tạo nên những trận đấu kịch tính!",
                IsPinned = false,
                CreatedDate = now.AddDays(-45)
            },
            new()
            {
                Title = "💡 Tips: Cách chọn vợt Pickleball phù hợp",
                Content = "Bạn mới chơi Pickleball và đang phân vân chọn vợt? Đây là một số gợi ý:\n\n1. Trọng lượng: 200-250g cho người mới\n2. Kích thước mặt vợt: Oversized (rộng hơn, dễ đánh trúng)\n3. Chất liệu: Composite hoặc Graphite\n4. Grip: Chọn size phù hợp với tay\n\nNếu cần tư vấn thêm, hãy liên hệ với Admin nhé!",
                IsPinned = false,
                CreatedDate = now.AddDays(-20)
            }
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedBookingsAsync(ApplicationDbContext context)
    {
        if (context.Bookings.Any()) return;

        var members = context.Members.ToList();
        var courts = context.Courts.ToList();
        var random = new Random(42);
        var now = DateTime.UtcNow.Date;

        // Past bookings (completed)
        for (int day = -14; day < 0; day++)
        {
            var date = now.AddDays(day);
            foreach (var court in courts.Take(4))
            {
                for (int hour = 6; hour < 21; hour += 2)
                {
                    if (random.NextDouble() > 0.6) // 40% fill rate
                    {
                        var member = members[random.Next(members.Count)];
                        context.Bookings.Add(new Booking
                        {
                            CourtId = court.Id,
                            MemberId = member.Id,
                            StartTime = date.AddHours(hour),
                            EndTime = date.AddHours(hour + 1.5),
                            TotalPrice = (int)(court.PricePerHour * 1.5m),
                            Status = BookingStatus.Completed,
                            CreatedDate = date.AddDays(-random.Next(1, 7))
                        });
                    }
                }
            }
        }

        // Today and future bookings
        for (int day = 0; day <= 7; day++)
        {
            var date = now.AddDays(day);
            foreach (var court in courts)
            {
                for (int hour = 6; hour < 21; hour += 2)
                {
                    if (random.NextDouble() > 0.5) // 50% fill rate
                    {
                        var member = members[random.Next(members.Count)];
                        var status = day == 0 && hour < DateTime.Now.Hour 
                            ? BookingStatus.Completed 
                            : BookingStatus.Confirmed;
                            
                        context.Bookings.Add(new Booking
                        {
                            CourtId = court.Id,
                            MemberId = member.Id,
                            StartTime = date.AddHours(hour),
                            EndTime = date.AddHours(hour + 1.5),
                            TotalPrice = (int)(court.PricePerHour * 1.5m),
                            Status = status,
                            CreatedDate = date.AddDays(-random.Next(0, 3))
                        });
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedNotificationsAsync(ApplicationDbContext context)
    {
        if (context.Notifications.Any()) return;

        var members = context.Members.Take(10).ToList();
        var now = DateTime.UtcNow;

        foreach (var member in members)
        {
            context.Notifications.AddRange(new List<Notification>
            {
                new()
                {
                    ReceiverId = member.Id,
                    Message = "🎉 Chào mừng bạn đến với CLB Vợt Thủ Phố Núi! Hãy bắt đầu bằng việc nạp tiền và đặt sân nhé.",
                    Type = NotificationType.Info,
                    IsRead = true,
                    CreatedDate = member.JoinDate
                },
                new()
                {
                    ReceiverId = member.Id,
                    Message = "🏆 Winter Championship 2026 đã mở đăng ký! Tổng giải thưởng 15 triệu đồng. Đăng ký ngay!",
                    Type = NotificationType.Info,
                    IsRead = false,
                    CreatedDate = now.AddDays(-3)
                },
                new()
                {
                    ReceiverId = member.Id,
                    Message = $"✅ Đặt sân thành công! Sân 1 - Indoor, {now.AddDays(2):dd/MM} lúc 17:00",
                    Type = NotificationType.Success,
                    IsRead = false,
                    CreatedDate = now.AddHours(-5)
                }
            });
        }
        await context.SaveChangesAsync();
    }
}
