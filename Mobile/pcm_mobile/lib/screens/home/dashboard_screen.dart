import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../config/theme.dart';
import '../../providers/auth_provider.dart';
import '../../providers/dashboard_provider.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<DashboardProvider>().fetchDashboard();
    });
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final dashboardProvider = context.watch<DashboardProvider>();
    final dashboard = dashboardProvider.dashboard;
    final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: 'đ');

    return RefreshIndicator(
      onRefresh: () async {
        await context.read<DashboardProvider>().fetchDashboard();
        await context.read<AuthProvider>().refreshUser();
      },
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Custom Header
            Padding(
              padding: const EdgeInsets.only(bottom: 20),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                   Row(
                     children: [
                       Container(
                         padding: const EdgeInsets.all(8),
                         decoration: BoxDecoration(
                           color: AppTheme.primaryColor.withValues(alpha: 0.1),
                           shape: BoxShape.circle,
                         ),
                         child: const Icon(Icons.sports_tennis, color: AppTheme.primaryColor, size: 24),
                       ),
                       const SizedBox(width: 12),
                       Column(
                         crossAxisAlignment: CrossAxisAlignment.start,
                         children: [
                           Text('PCM Club', style: TextStyle(color: Colors.grey.shade600, fontSize: 12, fontWeight: FontWeight.bold)),
                           Text('Xin chào, ${user?.fullName.split(' ').last ?? 'User'}!', 
                             style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)
                           ),
                         ],
                       ),
                     ],
                   ),
                   IconButton(
                     onPressed: () => context.go('/notifications'),
                     icon: const Icon(Icons.notifications_outlined),
                   ),
                ],
              ),
            ),

            // Welcome Card (Simplified)
            Card(
              elevation: 0,
              color: AppTheme.primaryColor,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(24),
              ),
              child: Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(24),
                  gradient: LinearGradient(
                    colors: [
                      AppTheme.primaryColor,
                      AppTheme.primaryColor.withValues(alpha: 0.8),
                    ],
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                     // Wallet Balance
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Số dư ví',
                                style: TextStyle(
                                  color: Colors.white70,
                                  fontSize: 14,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                currencyFormat.format(user?.walletBalance ?? 0),
                                style: const TextStyle(
                                  fontSize: 24,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.white,
                                ),
                              ),
                            ],
                          ),
                          ElevatedButton(
                            onPressed: () => context.go('/wallet/deposit'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: Colors.white,
                              foregroundColor: AppTheme.primaryColor,
                            ),
                            child: const Text('Nạp tiền'),
                          ),
                        ],
                      ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // Quick Stats
            Row(
              children: [
                Expanded(
                  child: _StatCard(
                    icon: Icons.calendar_today,
                    title: 'Lịch đặt sân',
                    value: '${dashboard?.upcomingBookings ?? 2}', 
                    color: AppTheme.primaryColor, 
                    backgroundColor: Theme.of(context).brightness == Brightness.dark 
                        ? Theme.of(context).cardColor 
                        : AppTheme.secondaryColor,
                    onTap: () => context.go('/calendar'),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: _StatCard(
                    icon: Icons.sports_tennis,
                    title: 'Trận đấu',
                    value: '${dashboard?.upcomingMatches ?? 5}',
                    color: AppTheme.warningColor,
                    backgroundColor: Theme.of(context).brightness == Brightness.dark 
                        ? Theme.of(context).cardColor 
                        : const Color(0xFFFFF7ED),
                    onTap: () => context.go('/tournaments'),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            
            // PROMO BANNER
            SizedBox(
              height: 140,
              child: PageView(
                controller: PageController(viewportFraction: 0.9),
                padEnds: false,
                children: [
                  _PromoCard(
                     color: Colors.orange, 
                     title: 'Khuyến mãi hè', 
                     subtitle: 'Giảm 20% phí sân sáng',
                     icon: Icons.wb_sunny,
                  ),
                  _PromoCard(
                     color: Colors.blue, 
                     title: 'Giải đấu mới', 
                     subtitle: 'Đăng ký Winter Championship ngay',
                     icon: Icons.emoji_events,
                  ),
                  _PromoCard(
                     color: Colors.purple, 
                     title: 'Thành viên VIP', 
                     subtitle: 'Nâng hạng để nhận ưu đãi',
                     icon: Icons.diamond,
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Quick Actions
            const Text(
              'Thao tác nhanh',
              style: AppTheme.subheadingStyle,
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: _ActionButton(
                    icon: Icons.add_circle_outline,
                    label: 'Đặt sân',
                    onTap: () => context.go('/calendar'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _ActionButton(
                    icon: Icons.emoji_events_outlined,
                    label: 'Giải đấu',
                    onTap: () => context.go('/tournaments'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _ActionButton(
                    icon: Icons.people_outline,
                    label: 'Thành viên',
                    onTap: () {},
                  ),
                ),
                const SizedBox(width: 12),
                 Expanded(
                  child: _ActionButton(
                    icon: Icons.shopping_bag_outlined,
                    label: 'Cửa hàng',
                    onTap: () {},
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            
            // ONGOING EVENTS (FAKE)
            const Text(
              'Đang diễn ra tại CLB',
              style: AppTheme.subheadingStyle,
            ),
            const SizedBox(height: 12),
            ListView(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              children: [
                 Card(
                    child: ListTile(
                      leading: const CircleAvatar(backgroundColor: Colors.redAccent, child: Icon(Icons.sports_tennis, color: Colors.white, size: 20)),
                      title: const Text('Chung kết: Team A vs Team B', style: TextStyle(fontWeight: FontWeight.bold)),
                      subtitle: const Text('Sân 1 - Indoor • Đang thi đấu (Set 3)'),
                      trailing: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(color: Colors.green, borderRadius: BorderRadius.circular(12)),
                        child: const Text('LIVE', style: TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold)),
                      ),
                    ),
                 ),
                 Card(
                    child: ListTile(
                      leading: const CircleAvatar(backgroundColor: Colors.blueAccent, child: Icon(Icons.group, color: Colors.white, size: 20)),
                      title: const Text('Giao lưu CLB Vợt Thủ', style: TextStyle(fontWeight: FontWeight.bold)),
                      subtitle: const Text('Sân 3, 4 - Mái che • 12 thành viên'),
                      trailing: const Text('10:00 - 12:00'),
                    ),
                 ),
              ],
            ),
            const SizedBox(height: 24),

            // Pinned News
            if (dashboard?.pinnedNews.isNotEmpty ?? true) ...[ 
              const Text(
                'Tin tức nổi bật',
                style: AppTheme.subheadingStyle,
              ),
              const SizedBox(height: 12),
              ...(dashboard?.pinnedNews.isNotEmpty == true ? dashboard!.pinnedNews : [
                  // Mock news if empty
                  (title: 'Thông báo bảo trì sân 2', content: 'Sân 2 sẽ bảo trì vào ngày 28/01. Vui lòng đặt sân khác.'),
                  (title: 'Kết quả giải đấu tháng 1', content: 'Chúc mừng Team X đã giành chiến thắng thuyết phục.'),
              ] as dynamic).map((news) => Card( 
                    margin: const EdgeInsets.only(bottom: 12),
                    child: ListTile(
                      leading: Container(
                        width: 48,
                        height: 48,
                        decoration: BoxDecoration(
                          color: AppTheme.primaryColor.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Icon(
                          Icons.article_outlined,
                          color: AppTheme.primaryColor,
                        ),
                      ),
                      title: Text(
                        news is Map ? news['title'] : news.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                      subtitle: Text(
                        news is Map ? news['content'] : news.content,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                      trailing: const Icon(Icons.chevron_right),
                    ),
                  )),
            ],
          ],
        ),
      ),
    );
  }
}

class _PromoCard extends StatelessWidget {
  final Color color;
  final String title;
  final String subtitle;
  final IconData icon;

  const _PromoCard({required this.color, required this.title, required this.subtitle, required this.icon});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(right: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(color: color.withOpacity(0.3), blurRadius: 8, offset: const Offset(0, 4)),
        ],
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(title, style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                Text(subtitle, style: const TextStyle(color: Colors.white70, fontSize: 12)),
              ],
            ),
          ),
          Icon(icon, color: Colors.white.withOpacity(0.8), size: 48),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final String value;
  final Color color;
  final Color backgroundColor;
  final VoidCallback? onTap;

  const _StatCard({
    required this.icon,
    required this.title,
    required this.value,
    required this.color,
    this.backgroundColor = Colors.white,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    
    return Card(
      color: backgroundColor,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: isDark ? Colors.white10 : color.withOpacity(0.1), width: 1),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: isDark ? Colors.white10 : Colors.white,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: color, size: 24),
              ),
              const SizedBox(height: 16),
              Text(
                value,
                style: TextStyle(
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                  color: color,
                  letterSpacing: -1,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                title,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Theme.of(context).textTheme.bodyMedium?.color?.withOpacity(0.7),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ActionButton extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback? onTap;

  const _ActionButton({
    required this.icon,
    required this.label,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card( 
      elevation: 0,
      shape: RoundedRectangleBorder(
         borderRadius: BorderRadius.circular(12),
         side: BorderSide(color: Theme.of(context).dividerColor.withOpacity(0.2)),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 16),
          child: Column(
            children: [
              Container(
                 padding: const EdgeInsets.all(10),
                 decoration: BoxDecoration(
                    color: AppTheme.primaryColor.withOpacity(0.1),
                    shape: BoxShape.circle,
                 ),
                 child: Icon(icon, color: AppTheme.primaryColor, size: 24),
              ),
              const SizedBox(height: 8),
              Text(
                label,
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: Theme.of(context).textTheme.bodyLarge?.color,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
