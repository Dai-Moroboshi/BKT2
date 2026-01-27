class NotificationModel {
  final int id;
  final String message;
  final String type;
  final String? linkUrl;
  final bool isRead;
  final DateTime createdDate;

  NotificationModel({
    required this.id,
    required this.message,
    required this.type,
    this.linkUrl,
    required this.isRead,
    required this.createdDate,
  });

  factory NotificationModel.fromJson(Map<String, dynamic> json) {
    return NotificationModel(
      id: json['id'] ?? 0,
      message: json['message'] ?? '',
      type: json['type'] ?? 'Info',
      linkUrl: json['linkUrl'],
      isRead: json['isRead'] ?? false,
      createdDate: json['createdDate'] != null
          ? DateTime.parse(json['createdDate'])
          : DateTime.now(),
    );
  }

  bool get isInfo => type == 'Info';
  bool get isSuccess => type == 'Success';
  bool get isWarning => type == 'Warning';
}

class NewsModel {
  final int id;
  final String title;
  final String content;
  final bool isPinned;
  final DateTime createdDate;
  final String? imageUrl;

  NewsModel({
    required this.id,
    required this.title,
    required this.content,
    required this.isPinned,
    required this.createdDate,
    this.imageUrl,
  });

  factory NewsModel.fromJson(Map<String, dynamic> json) {
    return NewsModel(
      id: json['id'] ?? 0,
      title: json['title'] ?? '',
      content: json['content'] ?? '',
      isPinned: json['isPinned'] ?? false,
      createdDate: json['createdDate'] != null
          ? DateTime.parse(json['createdDate'])
          : DateTime.now(),
      imageUrl: json['imageUrl'],
    );
  }
}

class DashboardModel {
  final double walletBalance;
  final double rankLevel;
  final int upcomingBookings;
  final int upcomingMatches;
  final int unreadNotifications;
  final List<dynamic> nextBookings;
  final List<dynamic> nextMatches;
  final List<NewsModel> pinnedNews;

  DashboardModel({
    required this.walletBalance,
    required this.rankLevel,
    required this.upcomingBookings,
    required this.upcomingMatches,
    required this.unreadNotifications,
    required this.nextBookings,
    required this.nextMatches,
    required this.pinnedNews,
  });

  factory DashboardModel.fromJson(Map<String, dynamic> json) {
    return DashboardModel(
      walletBalance: (json['walletBalance'] ?? 0).toDouble(),
      rankLevel: (json['rankLevel'] ?? 0).toDouble(),
      upcomingBookings: json['upcomingBookings'] ?? 0,
      upcomingMatches: json['upcomingMatches'] ?? 0,
      unreadNotifications: json['unreadNotifications'] ?? 0,
      nextBookings: json['nextBookings'] ?? [],
      nextMatches: json['nextMatches'] ?? [],
      pinnedNews: json['pinnedNews'] != null
          ? (json['pinnedNews'] as List)
              .map((n) => NewsModel.fromJson(n))
              .toList()
          : [],
    );
  }
}
