import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../config/theme.dart';
import '../../providers/tournament_provider.dart';
import '../../providers/auth_provider.dart';

class TournamentDetailScreen extends StatefulWidget {
  final int tournamentId;

  const TournamentDetailScreen({super.key, required this.tournamentId});

  @override
  State<TournamentDetailScreen> createState() => _TournamentDetailScreenState();
}

class _TournamentDetailScreenState extends State<TournamentDetailScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TournamentProvider>().fetchTournament(widget.tournamentId);
    });
  }

  @override
  void dispose() {
    context.read<TournamentProvider>().leaveTournamentGroup(widget.tournamentId);
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _joinTournament() async {
    final user = context.read<AuthProvider>().user;
    final tournament = context.read<TournamentProvider>().selectedTournament;

    if (tournament == null) return;

    if ((user?.walletBalance ?? 0) < tournament.entryFee) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Số dư ví không đủ để đăng ký'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xác nhận đăng ký'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Giải đấu: ${tournament.name}'),
            Text('Phí tham gia: ${NumberFormat.currency(locale: 'vi_VN', symbol: 'đ').format(tournament.entryFee)}'),
            const SizedBox(height: 8),
            const Text(
              'Phí tham gia sẽ được trừ từ ví của bạn.',
              style: TextStyle(color: Colors.grey, fontSize: 12),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Đăng ký'),
          ),
        ],
      ),
    );

    if (confirmed == true && mounted) {
      final provider = context.read<TournamentProvider>();
      final success = await provider.joinTournament(widget.tournamentId);

      if (success && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đăng ký thành công!'),
            backgroundColor: AppTheme.successColor,
          ),
        );
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
    final provider = context.watch<TournamentProvider>();
    final tournament = provider.selectedTournament;
    final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: 'đ');
    final dateFormat = DateFormat('dd/MM/yyyy');

    if (provider.isLoading && tournament == null) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (tournament == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Chi tiết giải đấu')),
        body: const Center(child: Text('Không tìm thấy giải đấu')),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: Text(tournament.name),
      ),
      body: Column(
        children: [
          // Tournament Info Card
          Container(
            padding: const EdgeInsets.all(16),
            color: AppTheme.primaryColor.withValues(alpha: 0.1),
            child: Column(
              children: [
                Row(
                  children: [
                    Expanded(
                      child: _InfoChip(
                        icon: Icons.calendar_today,
                        label: '${dateFormat.format(tournament.startDate)} - ${dateFormat.format(tournament.endDate)}',
                      ),
                    ),
                    Expanded(
                      child: _InfoChip(
                        icon: Icons.people,
                        label: '${tournament.participantCount} người',
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: _InfoChip(
                        icon: Icons.attach_money,
                        label: 'Phí: ${currencyFormat.format(tournament.entryFee)}',
                      ),
                    ),
                    Expanded(
                      child: _InfoChip(
                        icon: Icons.emoji_events,
                        label: 'Giải: ${currencyFormat.format(tournament.prizePool)}',
                        color: AppTheme.accentColor,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),

          // Tabs
          TabBar(
            controller: _tabController,
            labelColor: AppTheme.primaryColor,
            tabs: const [
              Tab(text: 'Thông tin'),
              Tab(text: 'Người tham gia'),
              Tab(text: 'Lịch đấu'),
            ],
          ),

          // Tab Content
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                // Info Tab
                SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Mô tả', style: AppTheme.subheadingStyle),
                      const SizedBox(height: 8),
                      Text(tournament.description ?? 'Không có mô tả'),
                      const SizedBox(height: 16),
                      const Text('Thể thức', style: AppTheme.subheadingStyle),
                      const SizedBox(height: 8),
                      Text(tournament.format),
                    ],
                  ),
                ),

                // Participants Tab
                tournament.participants?.isEmpty ?? true
                    ? const Center(child: Text('Chưa có người tham gia'))
                    : ListView.builder(
                        padding: const EdgeInsets.all(8),
                        itemCount: tournament.participants!.length,
                        itemBuilder: (context, index) {
                          final participant = tournament.participants![index];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: AppTheme.primaryColor,
                              child: Text(
                                participant.memberName.substring(0, 1).toUpperCase(),
                                style: const TextStyle(color: Colors.white),
                              ),
                            ),
                            title: Text(participant.memberName),
                            subtitle: participant.teamName != null
                                ? Text(participant.teamName!)
                                : null,
                            trailing: participant.seed != null
                                ? Chip(
                                    label: Text('Seed ${participant.seed}'),
                                    backgroundColor: AppTheme.goldTier,
                                  )
                                : null,
                          );
                        },
                      ),

                // Matches Tab
                tournament.matches?.isEmpty ?? true
                    ? const Center(child: Text('Chưa có lịch đấu'))
                    : ListView.builder(
                        padding: const EdgeInsets.all(8),
                        itemCount: tournament.matches!.length,
                        itemBuilder: (context, index) {
                          final match = tournament.matches![index];
                          return Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            child: ListTile(
                              title: Text('${match.team1Name} vs ${match.team2Name}'),
                              subtitle: Text(
                                '${match.roundName ?? ''} • ${dateFormat.format(match.date)}',
                              ),
                              trailing: match.isFinished
                                  ? Text(
                                      '${match.score1} - ${match.score2}',
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                        fontSize: 16,
                                      ),
                                    )
                                  : const Icon(Icons.schedule),
                            ),
                          );
                        },
                      ),
              ],
            ),
          ),
        ],
      ),
      bottomNavigationBar: tournament.isRegistering && !tournament.isJoined
          ? SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: ElevatedButton(
                  onPressed: provider.isLoading ? null : _joinTournament,
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                  child: provider.isLoading
                      ? const SizedBox(
                          width: 24,
                          height: 24,
                          child: CircularProgressIndicator(color: Colors.white),
                        )
                      : Text(
                          'ĐĂNG KÝ THAM GIA (${currencyFormat.format(tournament.entryFee)})',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                ),
              ),
            )
          : null,
    );
  }
}

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color? color;

  const _InfoChip({
    required this.icon,
    required this.label,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 16, color: color ?? AppTheme.primaryColor),
        const SizedBox(width: 4),
        Expanded(
          child: Text(
            label,
            style: TextStyle(fontSize: 12, color: color ?? Colors.black87),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}
