class UserModel {
  final int memberId;
  final String email;
  final String fullName;
  final String role;
  final double walletBalance;
  final String tier;
  final double rankLevel;
  final String? avatarUrl;
  final DateTime? joinDate;
  final double? totalSpent;
  final int? unreadNotifications;

  UserModel({
    required this.memberId,
    required this.email,
    required this.fullName,
    required this.role,
    required this.walletBalance,
    required this.tier,
    required this.rankLevel,
    this.avatarUrl,
    this.joinDate,
    this.totalSpent,
    this.unreadNotifications,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      memberId: json['memberId'] ?? 0,
      email: json['email'] ?? '',
      fullName: json['fullName'] ?? '',
      role: json['role'] ?? 'Member',
      walletBalance: (json['walletBalance'] ?? 0).toDouble(),
      tier: json['tier'] ?? 'Standard',
      rankLevel: (json['rankLevel'] ?? 0).toDouble(),
      avatarUrl: json['avatarUrl'],
      joinDate: json['joinDate'] != null ? DateTime.parse(json['joinDate']) : null,
      totalSpent: json['totalSpent']?.toDouble(),
      unreadNotifications: json['unreadNotifications'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'memberId': memberId,
      'email': email,
      'fullName': fullName,
      'role': role,
      'walletBalance': walletBalance,
      'tier': tier,
      'rankLevel': rankLevel,
      'avatarUrl': avatarUrl,
      'joinDate': joinDate?.toIso8601String(),
      'totalSpent': totalSpent,
      'unreadNotifications': unreadNotifications,
    };
  }

  bool get isAdmin => role == 'Admin';
  bool get isTreasurer => role == 'Treasurer' || role == 'Admin';
  bool get isReferee => role == 'Referee' || role == 'Admin';
  bool get isVip => tier == 'Gold' || tier == 'Diamond';
}

class AuthResponse {
  final String token;
  final UserModel user;

  AuthResponse({
    required this.token,
    required this.user,
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      token: json['token'] ?? '',
      user: UserModel.fromJson(json),
    );
  }
}
