import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:table_calendar/table_calendar.dart';
import 'package:intl/intl.dart';
import '../../config/theme.dart';
import '../../models/booking_model.dart';
import '../../providers/booking_provider.dart';
import '../../providers/auth_provider.dart';

class CalendarScreen extends StatefulWidget {
  const CalendarScreen({super.key});

  @override
  State<CalendarScreen> createState() => _CalendarScreenState();
}

class _CalendarScreenState extends State<CalendarScreen> {
  DateTime _selectedDay = DateTime.now();
  DateTime _focusedDay = DateTime.now();
  CalendarFormat _calendarFormat = CalendarFormat.week;
  int? _selectedCourtId;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<BookingProvider>();
      provider.fetchCourts();
      provider.fetchCalendar(_selectedDay);
    });
  }

  void _onDaySelected(DateTime selectedDay, DateTime focusedDay) {
    setState(() {
      _selectedDay = selectedDay;
      _focusedDay = focusedDay;
    });
    context.read<BookingProvider>().fetchCalendar(
      selectedDay,
      courtId: _selectedCourtId,
    );
  }

  Future<void> _showBookingDialog(CalendarSlotModel slot) async {
    final user = context.read<AuthProvider>().user;
    
    if (!slot.isAvailable) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(slot.isMine ? 'Đây là lịch của bạn' : 'Slot này đã được đặt'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xác nhận đặt sân'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Sân: ${slot.courtName}'),
            Text('Thời gian: ${DateFormat('HH:mm').format(slot.startTime)} - ${DateFormat('HH:mm').format(slot.endTime)}'),
            Text('Ngày: ${DateFormat('dd/MM/yyyy').format(slot.startTime)}'),
            const SizedBox(height: 8),
            Text(
              'Giá: ${NumberFormat.currency(locale: 'vi_VN', symbol: 'đ').format(slot.price)}',
              style: const TextStyle(fontWeight: FontWeight.bold, color: AppTheme.primaryColor),
            ),
            const SizedBox(height: 8),
            Text(
              'Số dư ví: ${NumberFormat.currency(locale: 'vi_VN', symbol: 'đ').format(user?.walletBalance ?? 0)}',
              style: TextStyle(
                color: (user?.walletBalance ?? 0) >= slot.price ? Colors.green : Colors.red,
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            onPressed: (user?.walletBalance ?? 0) >= slot.price
                ? () => Navigator.pop(context, true)
                : null,
            child: const Text('Đặt sân'),
          ),
        ],
      ),
    );

    if (confirmed == true && mounted) {
      final provider = context.read<BookingProvider>();
      final booking = await provider.createBooking(
        courtId: slot.courtId,
        startTime: slot.startTime,
        endTime: slot.endTime,
      );

      if (booking != null && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đặt sân thành công!'),
            backgroundColor: AppTheme.successColor,
          ),
        );
        // Refresh user data to update wallet balance
        context.read<AuthProvider>().refreshUser();
      } else if (provider.error != null && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(provider.error!),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final bookingProvider = context.watch<BookingProvider>();
    final courts = bookingProvider.courts;
    final slots = bookingProvider.slots;

    // Group slots by court
    final slotsByCourt = <int, List<CalendarSlotModel>>{};
    for (final slot in slots) {
      slotsByCourt.putIfAbsent(slot.courtId, () => []).add(slot);
    }

    return Column(
      children: [
        // Calendar
        TableCalendar(
          firstDay: DateTime.now().subtract(const Duration(days: 7)),
          lastDay: DateTime.now().add(const Duration(days: 60)),
          focusedDay: _focusedDay,
          selectedDayPredicate: (day) => isSameDay(_selectedDay, day),
          calendarFormat: _calendarFormat,
          onDaySelected: _onDaySelected,
          onFormatChanged: (format) {
            setState(() {
              _calendarFormat = format;
            });
          },
          calendarStyle: CalendarStyle(
            todayDecoration: BoxDecoration(
              color: AppTheme.primaryColor.withValues(alpha: 0.5),
              shape: BoxShape.circle,
            ),
            selectedDecoration: const BoxDecoration(
              color: AppTheme.primaryColor,
              shape: BoxShape.circle,
            ),
          ),
          headerStyle: const HeaderStyle(
            formatButtonVisible: true,
            titleCentered: true,
          ),
        ),
        const Divider(height: 1),

        // Court Filter
        if (courts.isNotEmpty)
          Container(
            height: 50,
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: ListView.builder(
              scrollDirection: Axis.horizontal,
              itemCount: courts.length + 1,
              itemBuilder: (context, index) {
                if (index == 0) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 8),
                    child: ChoiceChip(
                      label: const Text('Tất cả'),
                      selected: _selectedCourtId == null,
                      onSelected: (selected) {
                        setState(() {
                          _selectedCourtId = null;
                        });
                        context.read<BookingProvider>().fetchCalendar(_selectedDay);
                      },
                    ),
                  );
                }
                final court = courts[index - 1];
                return Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 8),
                  child: ChoiceChip(
                    label: Text(court.name),
                    selected: _selectedCourtId == court.id,
                    onSelected: (selected) {
                      setState(() {
                        _selectedCourtId = selected ? court.id : null;
                      });
                      context.read<BookingProvider>().fetchCalendar(
                        _selectedDay,
                        courtId: _selectedCourtId,
                      );
                    },
                  ),
                );
              },
            ),
          ),
        const Divider(height: 1),

        // Slots Grid
        Expanded(
          child: bookingProvider.isLoading
              ? const Center(child: CircularProgressIndicator())
              : slots.isEmpty
                  ? const Center(
                      child: Text('Không có slot nào trong ngày này'),
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.all(8),
                      itemCount: slotsByCourt.length,
                      itemBuilder: (context, index) {
                        final courtId = slotsByCourt.keys.elementAt(index);
                        final courtSlots = slotsByCourt[courtId]!;
                        final courtName = courtSlots.first.courtName;

                        return Card(
                          margin: const EdgeInsets.only(bottom: 12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Padding(
                                padding: const EdgeInsets.all(12),
                                child: Text(
                                  courtName,
                                  style: AppTheme.subheadingStyle,
                                ),
                              ),
                              const Divider(height: 1),
                              GridView.builder(
                                shrinkWrap: true,
                                physics: const NeverScrollableScrollPhysics(),
                                padding: const EdgeInsets.all(8),
                                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                                  crossAxisCount: 4,
                                  childAspectRatio: 1.5,
                                  crossAxisSpacing: 8,
                                  mainAxisSpacing: 8,
                                ),
                                itemCount: courtSlots.length,
                                itemBuilder: (context, slotIndex) {
                                  final slot = courtSlots[slotIndex];
                                  return _SlotTile(
                                    slot: slot,
                                    onTap: () => _showBookingDialog(slot),
                                  );
                                },
                              ),
                            ],
                          ),
                        );
                      },
                    ),
        ),
      ],
    );
  }
}

class _SlotTile extends StatelessWidget {
  final CalendarSlotModel slot;
  final VoidCallback onTap;

  const _SlotTile({
    required this.slot,
    required this.onTap,
  });

  Color get _backgroundColor {
    if (slot.isMine) return AppTheme.primaryColor;
    if (slot.isBooked) return Colors.grey;
    if (slot.isHolding) return Colors.orange;
    return Colors.green;
  }

  @override
  Widget build(BuildContext context) {
    return Material(
      color: _backgroundColor,
      borderRadius: BorderRadius.circular(8),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(8),
        child: Center(
          child: Text(
            DateFormat('HH:mm').format(slot.startTime),
            style: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w500,
              fontSize: 12,
            ),
          ),
        ),
      ),
    );
  }
}
