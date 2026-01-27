import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../config/theme.dart';
import '../../providers/tournament_provider.dart';

class TournamentListScreen extends StatefulWidget {
  const TournamentListScreen({super.key});

  @override
  State<TournamentListScreen> createState() => _TournamentListScreenState();
}

class _TournamentListScreenState extends State<TournamentListScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TournamentProvider>().fetchTournaments();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<TournamentProvider>();
    final tournaments = provider.tournaments;

    final openTournaments = tournaments
        .where((t) => t.status == 'Registering' || t.status == 'Open')
        .toList()
      ..sort((a, b) => a.startDate.compareTo(b.startDate));
    
    final ongoingTournaments = tournaments.where((t) => 
      t.status == 'Ongoing' || t.status == 'DrawCompleted').toList();
    final finishedTournaments = tournaments.where((t) => 
      t.status == 'Finished').toList();

    return Column(
      children: [
        TabBar(
          controller: _tabController,
          labelColor: AppTheme.primaryColor,
          isScrollable: true,
          tabAlignment: TabAlignment.start,
          tabs: [
            Tab(text: 'Đang mở (${openTournaments.length})'),
            Tab(text: 'Hôm nay (${ongoingTournaments.length})'), // Renamed from "Đang diễn ra"
            Tab(text: 'Đã kết thúc (${finishedTournaments.length})'),
          ],
        ),
        Expanded(
          child: TabBarView(
            controller: _tabController,
            children: [
              _TournamentList(
                tournaments: openTournaments,
                isLoading: provider.isLoading,
                emptyMessage: 'Không có giải đấu nào đang mở đăng ký',
              ),
              _TournamentList(
                tournaments: ongoingTournaments,
                isLoading: provider.isLoading,
                emptyMessage: 'Không có giải đấu nào đang diễn ra',
              ),
              _TournamentList(
                tournaments: finishedTournaments,
                isLoading: provider.isLoading,
                emptyMessage: 'Chưa có giải đấu nào kết thúc',
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _TournamentList extends StatelessWidget {
  final List tournaments;
  final bool isLoading;
  final String emptyMessage;

  const _TournamentList({
    required this.tournaments,
    required this.isLoading,
    required this.emptyMessage,
  });

  @override
  Widget build(BuildContext context) {
    if (isLoading && tournaments.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (tournaments.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.emoji_events_outlined,
              size: 64,
              color: Colors.grey,
            ),
            const SizedBox(height: 16),
            Text(
              emptyMessage,
              style: AppTheme.captionStyle,
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => context.read<TournamentProvider>().fetchTournaments(),
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        itemCount: tournaments.length,
        itemBuilder: (context, index) {
          final tournament = tournaments[index];
          return _TournamentCard(tournament: tournament);
        },
      ),
    );
  }
}

class _TournamentCard extends StatelessWidget {
  final dynamic tournament;

  const _TournamentCard({required this.tournament});

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Registering':
      case 'Open':
        return AppTheme.successColor;
      case 'Ongoing':
      case 'DrawCompleted':
        return AppTheme.accentColor;
      case 'Finished':
        return Colors.grey;
      default:
        return Colors.blue;
    }
  }

  String _getStatusText(String status) {
    switch (status) {
      case 'Registering':
        return 'Đang đăng ký';
      case 'Open':
        return 'Mở đăng ký';
      case 'Ongoing':
        return 'Đang diễn ra';
      case 'DrawCompleted':
        return 'Đã bốc thăm';
      case 'Finished':
        return 'Đã kết thúc';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: 'đ');
    final dateFormat = DateFormat('dd/MM/yyyy');

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: InkWell(
        onTap: () => context.go('/tournaments/${tournament.id}'),
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      tournament.name,
                      style: AppTheme.subheadingStyle,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color: _getStatusColor(tournament.status),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      _getStatusText(tournament.status),
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 12,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  const Icon(Icons.calendar_today, size: 16, color: Colors.grey),
                  const SizedBox(width: 4),
                  Text(
                    '${dateFormat.format(tournament.startDate)} - ${dateFormat.format(tournament.endDate)}',
                    style: AppTheme.captionStyle,
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: Row(
                      children: [
                        const Icon(Icons.people, size: 16, color: Colors.grey),
                        const SizedBox(width: 4),
                        Text(
                          '${tournament.participantCount} người',
                          style: AppTheme.captionStyle,
                        ),
                      ],
                    ),
                  ),
                  Expanded(
                    child: Row(
                      children: [
                        const Icon(Icons.sports_tennis, size: 16, color: Colors.grey),
                        const SizedBox(width: 4),
                        Text(
                          tournament.format,
                          style: AppTheme.captionStyle,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const Divider(height: 24),
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Phí tham gia', style: AppTheme.captionStyle),
                        Text(
                          currencyFormat.format(tournament.entryFee),
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            color: AppTheme.primaryColor,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('Tổng giải thưởng', style: AppTheme.captionStyle),
                        Text(
                          currencyFormat.format(tournament.prizePool),
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            color: AppTheme.accentColor,
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (tournament.isJoined)
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 6,
                      ),
                      decoration: BoxDecoration(
                        color: AppTheme.primaryColor.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: AppTheme.primaryColor),
                      ),
                      child: const Text(
                        'Đã đăng ký',
                        style: TextStyle(
                          color: AppTheme.primaryColor,
                          fontSize: 12,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
