import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../config/theme.dart';
import '../../providers/notification_provider.dart';

class NotificationScreen extends StatefulWidget {
  const NotificationScreen({super.key});

  @override
  State<NotificationScreen> createState() => _NotificationScreenState();
}

class _NotificationScreenState extends State<NotificationScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationProvider>().fetchNotifications();
    });
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<NotificationProvider>();
    final dateFormat = DateFormat('dd/MM HH:mm');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Thông báo'),
        actions: [
          if (provider.unreadCount > 0)
            TextButton(
              onPressed: () => provider.markAllAsRead(),
              child: const Text('Đọc tất cả', style: TextStyle(color: Colors.white)),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () => provider.fetchNotifications(),
        child: provider.isLoading && provider.notifications.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : provider.notifications.isEmpty
                ? const Center(child: Text('Không có thông báo'))
                : ListView.builder(
                    itemCount: provider.notifications.length,
                    itemBuilder: (context, index) {
                      final n = provider.notifications[index];
                      return Dismissible(
                        key: Key(n.id.toString()),
                        direction: DismissDirection.endToStart,
                        background: Container(
                          color: Colors.red,
                          alignment: Alignment.centerRight,
                          padding: const EdgeInsets.only(right: 16),
                          child: const Icon(Icons.delete, color: Colors.white),
                        ),
                        onDismissed: (_) => provider.deleteNotification(n.id),
                        child: Card(
                          margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                          color: n.isRead ? null : AppTheme.primaryColor.withValues(alpha: 0.05),
                          child: ListTile(
                            leading: CircleAvatar(
                              backgroundColor: n.isSuccess ? AppTheme.successColor
                                  : n.isWarning ? AppTheme.warningColor : Colors.blue,
                              child: Icon(n.isSuccess ? Icons.check : n.isWarning ? Icons.warning : Icons.info,
                                color: Colors.white, size: 20),
                            ),
                            title: Text(n.message, maxLines: 2, overflow: TextOverflow.ellipsis),
                            subtitle: Text(dateFormat.format(n.createdDate), style: AppTheme.captionStyle),
                            trailing: !n.isRead ? Container(
                              width: 8, height: 8,
                              decoration: const BoxDecoration(color: AppTheme.primaryColor, shape: BoxShape.circle),
                            ) : null,
                            onTap: () => provider.markAsRead(n.id),
                          ),
                        ),
                      );
                    },
                  ),
      ),
    );
  }
}
