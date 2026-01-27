import 'package:flutter/foundation.dart';
import '../config/api_config.dart';
import '../models/notification_model.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

class NotificationProvider extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();
  final SignalRService _signalRService = SignalRService();

  List<NotificationModel> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  String? _error;

  List<NotificationModel> get notifications => _notifications;
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  String? get error => _error;

  NotificationProvider() {
    _setupSignalR();
  }

  void _setupSignalR() {
    _signalRService.onNotification = (data) {
      _unreadCount++;
      // Add to beginning of list if we have it loaded
      final notification = NotificationModel(
        id: 0,
        message: data['message'] ?? '',
        type: data['type'] ?? 'Info',
        isRead: false,
        createdDate: DateTime.now(),
      );
      _notifications.insert(0, notification);
      notifyListeners();
    };
  }

  Future<void> fetchNotifications({bool unreadOnly = false}) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get(
        ApiConfig.notifications,
        queryParameters: {
          if (unreadOnly) 'unreadOnly': true,
        },
      );

      if (response.data['success'] == true) {
        _notifications = (response.data['data'] as List)
            .map((n) => NotificationModel.fromJson(n))
            .toList();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchUnreadCount() async {
    try {
      final response = await _apiClient.get(ApiConfig.notificationsUnreadCount);
      if (response.data['success'] == true) {
        _unreadCount = response.data['data'] ?? 0;
        notifyListeners();
      }
    } catch (e) {
      _error = e.toString();
    }
  }

  Future<bool> markAsRead(int id) async {
    try {
      final response = await _apiClient.put('${ApiConfig.notifications}/$id/read');
      if (response.data['success'] == true) {
        final index = _notifications.indexWhere((n) => n.id == id);
        if (index != -1) {
          // Update local state
          _notifications[index] = NotificationModel(
            id: _notifications[index].id,
            message: _notifications[index].message,
            type: _notifications[index].type,
            linkUrl: _notifications[index].linkUrl,
            isRead: true,
            createdDate: _notifications[index].createdDate,
          );
          _unreadCount = (_unreadCount - 1).clamp(0, _unreadCount);
          notifyListeners();
        }
        return true;
      }
    } catch (e) {
      _error = e.toString();
    }
    return false;
  }

  Future<bool> markAllAsRead() async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.put('${ApiConfig.notifications}/read-all');
      if (response.data['success'] == true) {
        _unreadCount = 0;
        await fetchNotifications();
        return true;
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return false;
  }

  Future<bool> deleteNotification(int id) async {
    try {
      final response = await _apiClient.delete('${ApiConfig.notifications}/$id');
      if (response.data['success'] == true) {
        _notifications.removeWhere((n) => n.id == id);
        notifyListeners();
        return true;
      }
    } catch (e) {
      _error = e.toString();
    }
    return false;
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
