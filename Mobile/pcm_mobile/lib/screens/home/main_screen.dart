import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../../config/theme.dart';
import '../../providers/auth_provider.dart';
import '../../providers/notification_provider.dart';

class MainScreen extends StatefulWidget {
  final Widget child;

  const MainScreen({super.key, required this.child});

  @override
  State<MainScreen> createState() => _MainScreenState();
}

class _MainScreenState extends State<MainScreen> {
  int _currentIndex = 0;

  final List<_NavItem> _navItems = [
    _NavItem(
      path: '/',
      icon: Icons.home_outlined,
      activeIcon: Icons.home,
      label: 'Trang chủ',
    ),
    _NavItem(
      path: '/calendar',
      icon: Icons.calendar_today_outlined,
      activeIcon: Icons.calendar_today,
      label: 'Đặt sân',
    ),
    _NavItem(
      path: '/tournaments',
      icon: Icons.emoji_events_outlined,
      activeIcon: Icons.emoji_events,
      label: 'Giải đấu',
    ),
    _NavItem(
      path: '/wallet',
      icon: Icons.account_balance_wallet_outlined,
      activeIcon: Icons.account_balance_wallet,
      label: 'Ví tiền',
    ),
    _NavItem(
      path: '/profile',
      icon: Icons.person_outlined,
      activeIcon: Icons.person,
      label: 'Cá nhân',
    ),
  ];

  @override
  void initState() {
    super.initState();
    // Fetch unread count on init
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationProvider>().fetchUnreadCount();
    });
  }

  void _onItemTapped(int index) {
    setState(() {
      _currentIndex = index;
    });
    context.go(_navItems[index].path);
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final unreadCount = context.watch<NotificationProvider>().unreadCount;

    return Scaffold(
      appBar: (_currentIndex == 0 || _currentIndex == 4) 
        ? null 
        : AppBar(
        title: Row(
          children: [
            if (_currentIndex == 2) const Icon(Icons.emoji_events, size: 28), // Specific icon for Tournament
            if (_currentIndex == 1) const Icon(Icons.calendar_today, size: 28),
            if (_currentIndex == 3) const Icon(Icons.account_balance_wallet, size: 28),
            const SizedBox(width: 8),
            Text(
              _navItems[_currentIndex].label,
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
          ],
        ),
        actions: [
          // Notifications
          Stack(
             alignment: Alignment.center,
             children: [
               IconButton(
                 icon: const Icon(Icons.notifications_outlined),
                 onPressed: () => context.go('/notifications'),
               ),
               if (unreadCount > 0)
                 Positioned(
                   right: 8,
                   top: 8,
                   child: Container(
                     padding: const EdgeInsets.all(4),
                     decoration: const BoxDecoration(
                       color: Colors.red,
                       shape: BoxShape.circle,
                     ),
                     child: Text(
                       unreadCount > 99 ? '99+' : unreadCount.toString(),
                       style: const TextStyle(
                         color: Colors.white,
                         fontSize: 10,
                         fontWeight: FontWeight.bold,
                       ),
                     ),
                   ),
                 ),
             ],
           ),
        ],
      ),
      body: widget.child,
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: _onItemTapped,
        destinations: _navItems
            .map((item) => NavigationDestination(
                  icon: Icon(item.icon),
                  selectedIcon: Icon(item.activeIcon),
                  label: item.label,
                ))
            .toList(),
      ),
    );
  }
}

class _NavItem {
  final String path;
  final IconData icon;
  final IconData activeIcon;
  final String label;

  _NavItem({
    required this.path,
    required this.icon,
    required this.activeIcon,
    required this.label,
  });
}
