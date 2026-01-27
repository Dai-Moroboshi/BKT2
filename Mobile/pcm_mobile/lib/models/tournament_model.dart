class TournamentModel {
  final int id;
  final String name;
  final DateTime startDate;
  final DateTime endDate;
  final String format;
  final double entryFee;
  final double prizePool;
  final String status;
  final String? description;
  final String? imageUrl;
  final int participantCount;
  final bool isJoined;
  final String? settings;
  final List<ParticipantModel>? participants;
  final List<MatchModel>? matches;

  TournamentModel({
    required this.id,
    required this.name,
    required this.startDate,
    required this.endDate,
    required this.format,
    required this.entryFee,
    required this.prizePool,
    required this.status,
    this.description,
    this.imageUrl,
    this.participantCount = 0,
    this.isJoined = false,
    this.settings,
    this.participants,
    this.matches,
  });

  factory TournamentModel.fromJson(Map<String, dynamic> json) {
    return TournamentModel(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      startDate: json['startDate'] != null
          ? DateTime.parse(json['startDate'])
          : DateTime.now(),
      endDate: json['endDate'] != null
          ? DateTime.parse(json['endDate'])
          : DateTime.now(),
      format: json['format'] ?? 'Knockout',
      entryFee: (json['entryFee'] ?? 0).toDouble(),
      prizePool: (json['prizePool'] ?? 0).toDouble(),
      status: json['status'] ?? 'Open',
      description: json['description'],
      imageUrl: json['imageUrl'],
      participantCount: json['participantCount'] ?? 0,
      isJoined: json['isJoined'] ?? false,
      settings: json['settings'],
      participants: json['participants'] != null
          ? (json['participants'] as List)
              .map((p) => ParticipantModel.fromJson(p))
              .toList()
          : null,
      matches: json['matches'] != null
          ? (json['matches'] as List)
              .map((m) => MatchModel.fromJson(m))
              .toList()
          : null,
    );
  }

  bool get isRegistering => status == 'Registering' || status == 'Open';
  bool get isOngoing => status == 'Ongoing';
  bool get isFinished => status == 'Finished';
}

class ParticipantModel {
  final int id;
  final int memberId;
  final String memberName;
  final String? teamName;
  final bool paymentStatus;
  final int? seed;
  final String? groupName;
  final double memberRank;

  ParticipantModel({
    required this.id,
    required this.memberId,
    required this.memberName,
    this.teamName,
    this.paymentStatus = false,
    this.seed,
    this.groupName,
    this.memberRank = 0,
  });

  factory ParticipantModel.fromJson(Map<String, dynamic> json) {
    return ParticipantModel(
      id: json['id'] ?? 0,
      memberId: json['memberId'] ?? 0,
      memberName: json['memberName'] ?? '',
      teamName: json['teamName'],
      paymentStatus: json['paymentStatus'] ?? false,
      seed: json['seed'],
      groupName: json['groupName'],
      memberRank: (json['memberRank'] ?? 0).toDouble(),
    );
  }
}

class MatchModel {
  final int id;
  final int? tournamentId;
  final String? tournamentName;
  final String? roundName;
  final DateTime date;
  final Duration startTime;
  final int? team1Player1Id;
  final String? team1Player1Name;
  final int? team1Player2Id;
  final String? team1Player2Name;
  final int? team2Player1Id;
  final String? team2Player1Name;
  final int? team2Player2Id;
  final String? team2Player2Name;
  final int score1;
  final int score2;
  final String? details;
  final String winningSide;
  final bool isRanked;
  final String status;
  final int? courtId;
  final String? courtName;

  MatchModel({
    required this.id,
    this.tournamentId,
    this.tournamentName,
    this.roundName,
    required this.date,
    required this.startTime,
    this.team1Player1Id,
    this.team1Player1Name,
    this.team1Player2Id,
    this.team1Player2Name,
    this.team2Player1Id,
    this.team2Player1Name,
    this.team2Player2Id,
    this.team2Player2Name,
    this.score1 = 0,
    this.score2 = 0,
    this.details,
    this.winningSide = 'None',
    this.isRanked = true,
    this.status = 'Scheduled',
    this.courtId,
    this.courtName,
  });

  factory MatchModel.fromJson(Map<String, dynamic> json) {
    return MatchModel(
      id: json['id'] ?? 0,
      tournamentId: json['tournamentId'],
      tournamentName: json['tournamentName'],
      roundName: json['roundName'],
      date: json['date'] != null ? DateTime.parse(json['date']) : DateTime.now(),
      startTime: _parseTimeSpan(json['startTime']),
      team1Player1Id: json['team1_Player1Id'],
      team1Player1Name: json['team1_Player1Name'],
      team1Player2Id: json['team1_Player2Id'],
      team1Player2Name: json['team1_Player2Name'],
      team2Player1Id: json['team2_Player1Id'],
      team2Player1Name: json['team2_Player1Name'],
      team2Player2Id: json['team2_Player2Id'],
      team2Player2Name: json['team2_Player2Name'],
      score1: json['score1'] ?? 0,
      score2: json['score2'] ?? 0,
      details: json['details'],
      winningSide: json['winningSide'] ?? 'None',
      isRanked: json['isRanked'] ?? true,
      status: json['status'] ?? 'Scheduled',
      courtId: json['courtId'],
      courtName: json['courtName'],
    );
  }

  static Duration _parseTimeSpan(dynamic value) {
    if (value == null) return Duration.zero;
    if (value is String) {
      final parts = value.split(':');
      if (parts.length >= 2) {
        return Duration(
          hours: int.tryParse(parts[0]) ?? 0,
          minutes: int.tryParse(parts[1]) ?? 0,
        );
      }
    }
    return Duration.zero;
  }

  String get team1Name {
    if (team1Player2Name != null) {
      return '${team1Player1Name ?? "TBD"} / ${team1Player2Name}';
    }
    return team1Player1Name ?? 'TBD';
  }

  String get team2Name {
    if (team2Player2Name != null) {
      return '${team2Player1Name ?? "TBD"} / ${team2Player2Name}';
    }
    return team2Player1Name ?? 'TBD';
  }

  bool get isScheduled => status == 'Scheduled';
  bool get isInProgress => status == 'InProgress';
  bool get isFinished => status == 'Finished';
}
