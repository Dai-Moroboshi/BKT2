class MemberModel {
  final int id;
  final String fullName;
  final DateTime joinDate;
  final double rankLevel;
  final bool isActive;
  final String tier;
  final String? avatarUrl;
  final String? email;
  final double? totalSpent;
  final int? totalMatches;
  final int? wins;
  final int? losses;

  MemberModel({
    required this.id,
    required this.fullName,
    required this.joinDate,
    required this.rankLevel,
    required this.isActive,
    required this.tier,
    this.avatarUrl,
    this.email,
    this.totalSpent,
    this.totalMatches,
    this.wins,
    this.losses,
  });

  factory MemberModel.fromJson(Map<String, dynamic> json) {
    return MemberModel(
      id: json['id'] ?? 0,
      fullName: json['fullName'] ?? '',
      joinDate: json['joinDate'] != null 
          ? DateTime.parse(json['joinDate']) 
          : DateTime.now(),
      rankLevel: (json['rankLevel'] ?? 0).toDouble(),
      isActive: json['isActive'] ?? true,
      tier: json['tier'] ?? 'Standard',
      avatarUrl: json['avatarUrl'],
      email: json['email'],
      totalSpent: json['totalSpent']?.toDouble(),
      totalMatches: json['totalMatches'],
      wins: json['wins'],
      losses: json['losses'],
    );
  }

  double get winRate {
    if (totalMatches == null || totalMatches == 0) return 0;
    return (wins ?? 0) / totalMatches! * 100;
  }
}
