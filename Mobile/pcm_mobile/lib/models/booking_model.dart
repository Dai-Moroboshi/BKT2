class CourtModel {
  final int id;
  final String name;
  final bool isActive;
  final String? description;
  final double pricePerHour;

  CourtModel({
    required this.id,
    required this.name,
    required this.isActive,
    this.description,
    required this.pricePerHour,
  });

  factory CourtModel.fromJson(Map<String, dynamic> json) {
    return CourtModel(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      isActive: json['isActive'] ?? true,
      description: json['description'],
      pricePerHour: (json['pricePerHour'] ?? 0).toDouble(),
    );
  }
}

class BookingModel {
  final int id;
  final int courtId;
  final String courtName;
  final int memberId;
  final String? memberName;
  final DateTime startTime;
  final DateTime endTime;
  final double totalPrice;
  final String status;
  final bool isRecurring;
  final String? recurrenceRule;

  BookingModel({
    required this.id,
    required this.courtId,
    required this.courtName,
    required this.memberId,
    this.memberName,
    required this.startTime,
    required this.endTime,
    required this.totalPrice,
    required this.status,
    this.isRecurring = false,
    this.recurrenceRule,
  });

  factory BookingModel.fromJson(Map<String, dynamic> json) {
    return BookingModel(
      id: json['id'] ?? 0,
      courtId: json['courtId'] ?? 0,
      courtName: json['courtName'] ?? '',
      memberId: json['memberId'] ?? 0,
      memberName: json['memberName'],
      startTime: json['startTime'] != null
          ? DateTime.parse(json['startTime'])
          : DateTime.now(),
      endTime: json['endTime'] != null
          ? DateTime.parse(json['endTime'])
          : DateTime.now(),
      totalPrice: (json['totalPrice'] ?? 0).toDouble(),
      status: json['status'] ?? '',
      isRecurring: json['isRecurring'] ?? false,
      recurrenceRule: json['recurrenceRule'],
    );
  }

  Duration get duration => endTime.difference(startTime);
  bool get isConfirmed => status == 'Confirmed';
  bool get isCancelled => status == 'Cancelled';
}

class CalendarSlotModel {
  final int? bookingId;
  final int courtId;
  final String courtName;
  final DateTime startTime;
  final DateTime endTime;
  final String status;
  final String? bookedBy;
  final double price;

  CalendarSlotModel({
    this.bookingId,
    required this.courtId,
    required this.courtName,
    required this.startTime,
    required this.endTime,
    required this.status,
    this.bookedBy,
    required this.price,
  });

  factory CalendarSlotModel.fromJson(Map<String, dynamic> json) {
    return CalendarSlotModel(
      bookingId: json['bookingId'],
      courtId: json['courtId'] ?? 0,
      courtName: json['courtName'] ?? '',
      startTime: json['startTime'] != null
          ? DateTime.parse(json['startTime'])
          : DateTime.now(),
      endTime: json['endTime'] != null
          ? DateTime.parse(json['endTime'])
          : DateTime.now(),
      status: json['status'] ?? 'available',
      bookedBy: json['bookedBy'],
      price: (json['price'] ?? 0).toDouble(),
    );
  }

  bool get isAvailable => status == 'available';
  bool get isMine => status == 'mine';
  bool get isBooked => status == 'booked';
  bool get isHolding => status == 'holding';
}
