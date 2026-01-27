class ApiConfig {
  // Base URL cho API
  // Sử dụng 10.0.2.2 cho Android Emulator (thay vì localhost)
  // Sử dụng localhost cho iOS Simulator
  static const String baseUrl = 'http://localhost:5027/api';
  
  // SignalR Hub URL
  static const String hubUrl = 'http://localhost:5027/hubs/pcm';
  
  // Timeout settings
  static const int connectTimeout = 30000; // 30 seconds
  static const int receiveTimeout = 30000;
  
  // API Endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String me = '/auth/me';
  
  static const String members = '/members';
  static const String profile = '/members/profile';
  
  static const String walletBalance = '/wallet/balance';
  static const String walletDeposit = '/wallet/deposit';
  static const String walletTransactions = '/wallet/transactions';
  static const String walletApprove = '/wallet/approve';
  
  static const String courts = '/courts';
  static const String bookings = '/bookings';
  static const String bookingsCalendar = '/bookings/calendar';
  static const String bookingsRecurring = '/bookings/recurring';
  
  static const String tournaments = '/tournaments';
  static const String matches = '/matches';
  
  static const String notifications = '/notifications';
  static const String notificationsUnreadCount = '/notifications/unread-count';
  
  static const String dashboard = '/dashboard';
  static const String adminDashboard = '/dashboard/admin';
  
  static const String news = '/news';
}
