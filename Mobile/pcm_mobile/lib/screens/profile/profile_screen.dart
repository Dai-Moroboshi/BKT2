import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import 'package:image_picker/image_picker.dart';
import '../../config/theme.dart';
import '../../providers/auth_provider.dart';
import '../../providers/theme_provider.dart';

import 'package:flutter/foundation.dart'; // For kIsWeb
import 'dart:io';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  XFile? _pickedFile;
  String? _selectedSampleAvatar;

  final List<String> _sampleAvatars = [
    'https://api.dicebear.com/9.x/adventurer/png?seed=Felix',
    'https://api.dicebear.com/9.x/adventurer/png?seed=Aneka',
    'https://api.dicebear.com/9.x/adventurer/png?seed=Snuggles',
    'https://api.dicebear.com/9.x/adventurer/png?seed=Mittens',
    'https://api.dicebear.com/9.x/adventurer/png?seed=Daisy',
    'https://api.dicebear.com/9.x/adventurer/png?seed=Scooter',
    'https://api.dicebear.com/9.x/micah/png?seed=George',
    'https://api.dicebear.com/9.x/micah/png?seed=Willow',
  ];

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final themeProvider = context.watch<ThemeProvider>(); // Force rebuild on locale change

    ImageProvider? getAvatarImage() {
      if (_pickedFile != null) {
        if (kIsWeb) {
          return NetworkImage(_pickedFile!.path);
        } else {
          return FileImage(File(_pickedFile!.path));
        }
      }
      if (_selectedSampleAvatar != null) {
        return NetworkImage(_selectedSampleAvatar!);
      }
      if (user?.avatarUrl != null) {
        return NetworkImage(user!.avatarUrl!);
      }
      return null;
    }

    return Scaffold(
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      body: SingleChildScrollView(
        child: Column(
          children: [
            // Header Section
            Container(
              padding: const EdgeInsets.fromLTRB(20, 60, 20, 30),
              decoration: BoxDecoration(
                color: Theme.of(context).cardColor,
                borderRadius: const BorderRadius.vertical(bottom: Radius.circular(30)),
                boxShadow: const [
                  BoxShadow(color: Colors.black12, blurRadius: 10, offset: Offset(0, 5)),
                ],
              ),
              child: Column(
                children: [
                  Stack(
                    alignment: Alignment.bottomRight,
                    children: [
                      Container(
                        padding: const EdgeInsets.all(4),
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          border: Border.all(color: AppTheme.primaryColor.withValues(alpha: 0.2), width: 2),
                        ),
                        child: CircleAvatar(
                          radius: 50,
                          backgroundColor: AppTheme.getTierColor(user?.tier ?? 'Standard'),
                          backgroundImage: getAvatarImage(),
                          child: (getAvatarImage() == null)
                            ? Text(
                                user?.fullName.substring(0, 1).toUpperCase() ?? 'U',
                                style: const TextStyle(fontSize: 40, color: Colors.white, fontWeight: FontWeight.bold),
                              )
                            : null,
                        ),
                      ),
                      InkWell(
                        onTap: () => _showAvatarOptions(context),
                        child: Container(
                          padding: const EdgeInsets.all(6),
                          decoration: const BoxDecoration(
                            color: AppTheme.primaryColor,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.edit, color: Colors.white, size: 16),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Text(user?.fullName ?? 'User', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold, fontSize: 24)),
                  const SizedBox(height: 4),
                  Text(user?.email ?? '', style: Theme.of(context).textTheme.bodyMedium),
                  const SizedBox(height: 16),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                    decoration: BoxDecoration(
                      color: AppTheme.getTierColor(user?.tier ?? 'Standard').withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: AppTheme.getTierColor(user?.tier ?? 'Standard'), width: 1),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.stars, size: 16, color: AppTheme.getTierColor(user?.tier ?? 'Standard')),
                        const SizedBox(width: 8),
                        Text(
                          '${user?.tier ?? 'Standard'} • DUPR ${user?.rankLevel.toStringAsFixed(2) ?? '0.00'}',
                          style: TextStyle(
                            color: AppTheme.getTierColor(user?.tier ?? 'Standard'),
                            fontWeight: FontWeight.bold,
                            fontSize: 14,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            
            const SizedBox(height: 24),

            // Menu Options
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: Column(
                children: [
                   _ProfileSection(
                     title: 'Tài khoản',
                     children: [
                       _ProfileMenuItem(
                         icon: Icons.person_outline, 
                         label: 'Thông tin cá nhân', 
                         onTap: () => _showUserInfo(context, user),
                       ),
                       _ProfileMenuItem(
                         icon: Icons.history, 
                         label: 'Lịch sử đặt sân', 
                         onTap: () => _showBookingHistory(context),
                       ),
                       _ProfileMenuItem(
                         icon: Icons.emoji_events_outlined, 
                         label: 'Trận đấu của tôi', 
                         onTap: () => _showMyMatches(context),
                       ),
                     ]
                   ),
                   const SizedBox(height: 24),
                   _ProfileSection(
                     title: 'Cài đặt & Khác',
                     children: [
                        _ProfileMenuItem(
                         icon: Icons.settings_outlined, 
                         label: 'Cài đặt ứng dụng', 
                         onTap: () => _showSettings(context),
                       ),
                       _ProfileMenuItem(
                         icon: Icons.help_outline, 
                         label: 'Trợ giúp & Hỗ trợ', 
                         onTap: () {},
                       ),
                       const SizedBox(height: 8),
                       _LogoutButton(),
                     ]
                   ),
                ],
              ),
            ),
            const SizedBox(height: 40),
          ],
        ),
      ),
    );
  }

  void _showAvatarOptions(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: Theme.of(context).cardColor,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Đổi ảnh đại diện', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 20),
            ListTile(
              leading: Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(color: Colors.blue.withValues(alpha: 0.1), shape: BoxShape.circle),
                child: const Icon(Icons.photo_library, color: Colors.blue),
              ),
              title: const Text('Chọn từ thư viện'),
              onTap: () async {
                Navigator.pop(context);
                final ImagePicker picker = ImagePicker();
                final XFile? image = await picker.pickImage(source: ImageSource.gallery);
                if (image != null) {
                  setState(() {
                    _pickedFile = image;
                    _selectedSampleAvatar = null;
                  });
                }
              },
            ),
            ListTile(
              leading: Container(
                 padding: const EdgeInsets.all(8),
                 decoration: BoxDecoration(color: Colors.purple.withValues(alpha: 0.1), shape: BoxShape.circle),
                 child: const Icon(Icons.face, color: Colors.purple),
              ),
              title: const Text('Chọn avatar có sẵn'),
              onTap: () {
                Navigator.pop(context);
                _showSampleAvatars(context);
              },
            ),
          ],
        ),
      ),
    );
  }

  void _showSampleAvatars(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      isScrollControlled: true,
      builder: (context) => DraggableScrollableSheet(
        initialChildSize: 0.6,
        minChildSize: 0.4,
        maxChildSize: 0.8,
        expand: false,
        builder: (_, controller) => Container(
          padding: const EdgeInsets.all(24),
           decoration: BoxDecoration(
            color: Theme.of(context).cardColor,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
          ),
          child: Column(
            children: [
              Text('Chọn Avatar', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
              const SizedBox(height: 20),
              Expanded(
                child: GridView.builder(
                  controller: controller,
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 3,
                    crossAxisSpacing: 16,
                    mainAxisSpacing: 16,
                  ),
                  itemCount: _sampleAvatars.length,
                  itemBuilder: (context, index) {
                    final url = _sampleAvatars[index];
                    return InkWell(
                      onTap: () {
                        setState(() {
                          _selectedSampleAvatar = url;
                          _pickedFile = null;
                        });
                        Navigator.pop(context);
                      },
                      child: Container(
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          border: Border.all(
                            color: _selectedSampleAvatar == url ? AppTheme.primaryColor : Colors.transparent,
                            width: 3,
                          ),
                        ),
                        child: CircleAvatar(
                          backgroundImage: NetworkImage(url),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showUserInfo(BuildContext context, dynamic user) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => Container(
        height: MediaQuery.of(context).size.height * 0.6,
        decoration: BoxDecoration(
          color: Theme.of(context).cardColor,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
        ),
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(child: Container(width: 40, height: 4, decoration: BoxDecoration(color: Colors.grey[300], borderRadius: BorderRadius.circular(2)))),
            const SizedBox(height: 24),
            Text('Thông tin cá nhân', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 24),
            _InfoRow(label: 'Họ và tên', value: user?.fullName ?? ''),
            _InfoRow(label: 'Email', value: user?.email ?? ''),
            _InfoRow(label: 'Số điện thoại', value: '0988 123 456'),
            _InfoRow(label: 'Tham gia từ', value: '20/01/2026'),
            _InfoRow(label: 'Hạng thành viên', value: user?.tier ?? 'Standard'),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Chỉnh sửa'),
              ),
            )
          ],
        ),
      ),
    );
  }

  void _showBookingHistory(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => DraggableScrollableSheet(
        initialChildSize: 0.7,
        maxChildSize: 0.9,
        builder: (_, controller) => Container(
          decoration: BoxDecoration(
            color: Theme.of(context).cardColor,
            borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
          ),
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
               Center(child: Container(width: 40, height: 4, decoration: BoxDecoration(color: Colors.grey[300], borderRadius: BorderRadius.circular(2)))),
               const SizedBox(height: 20),
               Text('Lịch sử đặt sân', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
               const SizedBox(height: 16),
               Expanded(
                 child: ListView.builder(
                   controller: controller,
                   itemCount: 5,
                   itemBuilder: (context, index) {
                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                          side: BorderSide(color: Colors.grey.withOpacity(0.2)),
                        ),
                        child: ListTile(
                          leading: Container(
                            padding: const EdgeInsets.all(8),
                            decoration: BoxDecoration(
                              color: Colors.blue.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: const Icon(Icons.calendar_today, color: Colors.blue),
                          ),
                          title: Text('Sân ${index + 1} - Pickleball Pleiku'),
                          subtitle: Text(DateFormat('dd/MM/yyyy • HH:mm').format(DateTime.now().subtract(Duration(days: index * 2)))),
                          trailing: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: Colors.green.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Text('Đã xong', style: TextStyle(color: Colors.green.shade700, fontSize: 12)),
                          ),
                        ),
                      );
                   },
                 ),
               ),
            ],
          ),
        ),
      ),
    );
  }

  void _showMyMatches(BuildContext context) {
     showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => Container(
        height: MediaQuery.of(context).size.height * 0.6,
        decoration: BoxDecoration(
          color: Theme.of(context).cardColor,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
        ),
         padding: const EdgeInsets.all(24),
         child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
               Center(child: Container(width: 40, height: 4, decoration: BoxDecoration(color: Colors.grey[300], borderRadius: BorderRadius.circular(2)))),
               const SizedBox(height: 20),
               Text('Trận đấu gần đây', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
               const SizedBox(height: 16),
               Expanded(
                 child: ListView(
                   children: [
                      _MatchItem(result: 'Thắng', score: '11 - 9', opponent: 'Nguyễn Văn A', date: 'Hôm qua'),
                      _MatchItem(result: 'Thua', score: '8 - 11', opponent: 'Team X', date: '25/01'),
                      _MatchItem(result: 'Thắng', score: '11 - 5', opponent: 'CLB Vợt Mới', date: '20/01'),
                   ]
                 )
               )
            ]
         )
      ),
     );
  }

  void _showSettings(BuildContext context) {
     final themeProvider = context.read<ThemeProvider>();
     bool notificationsEnabled = true;
     String selectedLanguage = 'vi'; // Default to Vietnamese

     showDialog(
       context: context,
       builder: (context) => StatefulBuilder(
         builder: (context, setState) {
           return AlertDialog(
             title: const Text('Cài đặt'),
             content: Column(
               mainAxisSize: MainAxisSize.min,
               children: [
                 SwitchListTile(
                    title: const Text('Thông báo'),
                    value: notificationsEnabled,
                    onChanged: (v){
                      setState(() {
                        notificationsEnabled = v;
                      });
                    },
                    activeColor: AppTheme.primaryColor
                 ),
                 SwitchListTile(
                   title: const Text('Giao diện tối'),
                   value: context.watch<ThemeProvider>().isDarkMode,
                   onChanged: (v) {
                     themeProvider.toggleTheme(v);
                   },
                   activeColor: AppTheme.primaryColor
                 ),
                  ListTile(
                    contentPadding: const EdgeInsets.symmetric(horizontal: 16),
                    title: const Text('Ngôn ngữ'),
                    trailing: DropdownButton<String>(
                      value: context.watch<ThemeProvider>().locale.languageCode,
                      icon: const Icon(Icons.arrow_drop_down),
                      elevation: 16,
                      style: const TextStyle(color: Colors.black, fontSize: 16),
                      underline: Container(
                        height: 2,
                        color: AppTheme.primaryColor,
                      ),
                      onChanged: (String? value) {
                        if (value != null) {
                          themeProvider.setLanguage(value);
                          // We don't need setState here because the provider will notify listeners
                          // and the dialog (wrapped in StatefulBuilder) might not need to rebuild
                          // strictly for this, but the parent App will.
                          // However, to update the dropdown *value* locally if needed:
                          setState(() {});
                        }
                      },
                      items: [
                        DropdownMenuItem<String>(
                          value: 'vi',
                          child: Text('Tiếng Việt', style: TextStyle(color: Theme.of(context).textTheme.bodyLarge?.color)),
                        ),
                        DropdownMenuItem<String>(
                          value: 'en',
                          child: Text('Tiếng Anh', style: TextStyle(color: Theme.of(context).textTheme.bodyLarge?.color)),
                        ),
                        DropdownMenuItem<String>(
                          value: 'zh',
                          child: Text('Tiếng Trung', style: TextStyle(color: Theme.of(context).textTheme.bodyLarge?.color)),
                        ),
                      ],
                    ),
                 ),
               ],
             ),
             actions: [
               TextButton(onPressed: () => Navigator.pop(context), child: const Text('Đóng'))
             ],
           );
         }
       ),
     );
  }
}

class _ProfileSection extends StatelessWidget {
  final String title;
  final List<Widget> children;

  const _ProfileSection({required this.title, required this.children});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 8, bottom: 12),
          child: Text(title, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.grey[600], letterSpacing: 0.5)), // Larger, cleaner title
        ),
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).cardColor,
            borderRadius: BorderRadius.circular(20), // Rounder corners
            boxShadow: [
              BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10, offset:const Offset(0, 4)), // Softer shadow
            ],
          ),
          child: Column(children: children),
        ),
      ],
    );
  }
}

class _ProfileMenuItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final Color? color;

  const _ProfileMenuItem({required this.icon, required this.label, required this.onTap, this.color});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 18), // Larger touch target
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: (color ?? AppTheme.primaryColor).withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: color ?? AppTheme.primaryColor, size: 22),
            ),
            const SizedBox(width: 18),
            Expanded(child: Text(label, style: TextStyle(fontWeight: FontWeight.w600, fontSize: 16, color: color ?? Theme.of(context).textTheme.bodyLarge?.color))), // 16px font
            Icon(Icons.chevron_right, size: 24, color: Colors.grey.shade400),
          ],
        ),
      ),
    );
  }
}

class _LogoutButton extends StatelessWidget {
  final String text;
  final String confirmTitle;
  final String confirmMsg;
  final String cancel;

  const _LogoutButton({
    this.text = 'Đăng xuất',
    this.confirmTitle = 'Đăng xuất',
    this.confirmMsg = 'Bạn có chắc muốn đăng xuất?',
    this.cancel = 'Hủy',
  });

   @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () async {
              final confirmed = await showDialog<bool>(
                context: context,
                builder: (c) => AlertDialog(
                  title: Text(confirmTitle),
                  content: Text(confirmMsg),
                  actions: [
                    TextButton(onPressed: () => Navigator.pop(c, false), child: Text(cancel)),
                    ElevatedButton(onPressed: () => Navigator.pop(c, true), child: Text(text), style: ElevatedButton.styleFrom(backgroundColor: Colors.red)),
                  ],
                ),
              );
              if (confirmed == true && context.mounted) {
                await context.read<AuthProvider>().logout();
                context.go('/login');
              }
            },
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 8),
        padding: const EdgeInsets.symmetric(vertical: 18),
        width: double.infinity,
        decoration: BoxDecoration(
           color: Colors.red.withOpacity(0.08),
           borderRadius: BorderRadius.circular(20),
        ),
        child: Center(child: Text(text, style: const TextStyle(color: Colors.red, fontWeight: FontWeight.w700, fontSize: 16))),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  const _InfoRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 20),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey, fontSize: 15)),
          Text(value, style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: Theme.of(context).textTheme.bodyLarge?.color)),
        ],
      ),
    );
  }
}

class _MatchItem extends StatelessWidget {
  final String result;
  final String score;
  final String opponent;
  final String date;

  const _MatchItem({required this.result, required this.score, required this.opponent, required this.date});

  @override
  Widget build(BuildContext context) {
    final isWin = result == 'Thắng';
    return Card(
      elevation: 0,
       shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      color: isWin ? Colors.green.withOpacity(0.08) : Colors.red.withOpacity(0.08),
      margin: const EdgeInsets.only(bottom: 16),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                 color: isWin ? Colors.green : Colors.red,
                 borderRadius: BorderRadius.circular(8),
              ),
              child: Text(result, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13)),
            ),
            const SizedBox(width: 16),
            Column(
               crossAxisAlignment: CrossAxisAlignment.start,
               children: [
                  Text('vs $opponent', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
                  Text(score, style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 18)),
               ],
            ),
            const Spacer(),
            Text(date, style: const TextStyle(color: Colors.grey, fontSize: 13)),
          ],
        ),
      ),
    );
  }
}
