import 'package:flutter/foundation.dart';
import '../config/api_config.dart';
import '../models/booking_model.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

class BookingProvider extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();
  final SignalRService _signalRService = SignalRService();

  List<CourtModel> _courts = [];
  List<CalendarSlotModel> _slots = [];
  List<BookingModel> _myBookings = [];
  bool _isLoading = false;
  String? _error;

  List<CourtModel> get courts => _courts;
  List<CalendarSlotModel> get slots => _slots;
  List<BookingModel> get myBookings => _myBookings;
  bool get isLoading => _isLoading;
  String? get error => _error;

  BookingProvider() {
    _setupSignalR();
  }

  void _setupSignalR() {
    _signalRService.onCalendarUpdate = (data) {
      // Refresh calendar when update received
      if (_slots.isNotEmpty) {
        final date = _slots.first.startTime;
        fetchCalendar(date);
      }
    };
  }

  Future<void> fetchCourts() async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get(ApiConfig.courts);
      if (response.data['success'] == true) {
        _courts = (response.data['data'] as List)
            .map((c) => CourtModel.fromJson(c))
            .toList();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchCalendar(DateTime date, {int? courtId}) async {
    _isLoading = true;
    notifyListeners();

    try {
      // 1. Fetch real bookings from API
      List<CalendarSlotModel> apiBookings = [];
      final response = await _apiClient.get(
        ApiConfig.bookingsCalendar,
        queryParameters: {
          'date': date.toIso8601String().split('T')[0],
          if (courtId != null) 'courtId': courtId,
        },
      );

      if (response.data['success'] == true) {
        apiBookings = (response.data['data'] as List)
            .map((s) => CalendarSlotModel.fromJson(s))
            .toList();
      }

      // 2. Generate Full Grid (05:00 - 22:00)
      List<CalendarSlotModel> fullSlots = [];
      
      // Determine target courts
      List<CourtModel> targetCourts = [];
      if (courtId != null) {
        targetCourts = _courts.where((c) => c.id == courtId).toList();
      } else {
        targetCourts = _courts;
      }

      // If no courts loaded yet, we can't generate slots properly 
      // ensuring fetchCourts is called before fetchCalendar in UI
      if (targetCourts.isEmpty && _courts.isEmpty) { 
         // Fallback if courts not loaded or empty
         _slots = apiBookings; 
      } else {
         for (var court in targetCourts) {
            // Hours: 05:00 to 22:00
            for (int hour = 5; hour < 22; hour++) {
               final slotStart = DateTime(date.year, date.month, date.day, hour);
               final slotEnd = slotStart.add(const Duration(hours: 1));

               // Check if any booking overlaps with this slot
               final existingBooking = apiBookings.firstWhere(
                 (b) => b.courtId == court.id && 
                        b.startTime.toLocal().isBefore(slotEnd) && 
                        b.endTime.toLocal().isAfter(slotStart),
                 orElse: () => CalendarSlotModel(
                   bookingId: 0, 
                   courtId: court.id, 
                   courtName: court.name,
                   startTime: slotStart, 
                   endTime: slotEnd, 
                   status: 'available', 
                   price: court.pricePerHour,
                   bookedBy: null
                 ),
               );

               // Add to list, avoiding duplicates for multi-hour bookings
               if (existingBooking.status != 'available') {
                  // If it's a real booking, only add if not already added
                  if (!fullSlots.any((s) => s.bookingId == existingBooking.bookingId)) {
                     fullSlots.add(existingBooking);
                  }
               } else {
                  // Add available slot
                  fullSlots.add(existingBooking);
               }
            }
         }
         _slots = fullSlots;
      }

    } catch (e) {
      _error = e.toString();
      // On error, clear slots or fake them? Let's clear for now
      _slots = []; 
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchMyBookings() async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get('${ApiConfig.bookings}/my');
      if (response.data['success'] == true) {
        _myBookings = (response.data['data'] as List)
            .map((b) => BookingModel.fromJson(b))
            .toList();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<BookingModel?> createBooking({
    required int courtId,
    required DateTime startTime,
    required DateTime endTime,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await _apiClient.post(
        ApiConfig.bookings,
        data: {
          'courtId': courtId,
          'startTime': startTime.toIso8601String(),
          'endTime': endTime.toIso8601String(),
        },
      );

      if (response.data['success'] == true) {
        final booking = BookingModel.fromJson(response.data['data']);
        _myBookings.insert(0, booking);
        await fetchCalendar(startTime, courtId: courtId);
        notifyListeners();
        return booking;
      } else {
        _error = response.data['message'] ?? 'Đặt sân thất bại';
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return null;
  }

  Future<List<BookingModel>?> createRecurringBooking({
    required int courtId,
    required DateTime startTime,
    required DateTime endTime,
    required String recurrenceRule,
    required int occurrences,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await _apiClient.post(
        ApiConfig.bookingsRecurring,
        data: {
          'courtId': courtId,
          'startTime': startTime.toIso8601String(),
          'endTime': endTime.toIso8601String(),
          'recurrenceRule': recurrenceRule,
          'occurrences': occurrences,
        },
      );

      if (response.data['success'] == true) {
        final bookings = (response.data['data'] as List)
            .map((b) => BookingModel.fromJson(b))
            .toList();
        await fetchMyBookings();
        notifyListeners();
        return bookings;
      } else {
        _error = response.data['message'] ?? 'Đặt sân định kỳ thất bại';
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return null;
  }

  Future<bool> cancelBooking(int bookingId) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await _apiClient.post('${ApiConfig.bookings}/cancel/$bookingId');

      if (response.data['success'] == true) {
        final index = _myBookings.indexWhere((b) => b.id == bookingId);
        if (index != -1) {
          _myBookings.removeAt(index);
        }
        notifyListeners();
        return true;
      } else {
        _error = response.data['message'] ?? 'Hủy đặt sân thất bại';
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return false;
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
