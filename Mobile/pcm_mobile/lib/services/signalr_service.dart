import 'package:signalr_netcore/signalr_client.dart';
import '../config/api_config.dart';
import 'api_client.dart';

class SignalRService {
  static final SignalRService _instance = SignalRService._internal();
  factory SignalRService() => _instance;

  HubConnection? _hubConnection;
  bool _isConnected = false;
  final ApiClient _apiClient = ApiClient();

  // Callbacks
  Function(Map<String, dynamic>)? onNotification;
  Function(Map<String, dynamic>)? onCalendarUpdate;
  Function(Map<String, dynamic>)? onMatchScoreUpdate;
  Function(double)? onWalletUpdate;

  SignalRService._internal();

  bool get isConnected => _isConnected;

  Future<void> connect() async {
    if (_isConnected) return;

    final token = await _apiClient.getToken();
    if (token == null) return;

    _hubConnection = HubConnectionBuilder()
        .withUrl(
          '${ApiConfig.hubUrl}?access_token=$token',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
          ),
        )
        .withAutomaticReconnect()
        .build();

    // Register handlers
    _hubConnection!.on('ReceiveNotification', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        onNotification?.call(data);
      }
    });

    _hubConnection!.on('UpdateCalendar', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        onCalendarUpdate?.call(data);
      }
    });

    _hubConnection!.on('UpdateMatchScore', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        onMatchScoreUpdate?.call(data);
      }
    });

    _hubConnection!.on('WalletUpdated', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final balance = (arguments[0] as num).toDouble();
        onWalletUpdate?.call(balance);
      }
    });

    try {
      await _hubConnection!.start();
      _isConnected = true;
      print('SignalR connected');
    } catch (e) {
      print('SignalR connection error: $e');
      _isConnected = false;
    }
  }

  Future<void> disconnect() async {
    if (_hubConnection != null && _isConnected) {
      await _hubConnection!.stop();
      _isConnected = false;
    }
  }

  Future<void> joinMatchGroup(int matchId) async {
    if (_hubConnection != null && _isConnected) {
      await _hubConnection!.invoke('JoinMatchGroup', args: [matchId]);
    }
  }

  Future<void> leaveMatchGroup(int matchId) async {
    if (_hubConnection != null && _isConnected) {
      await _hubConnection!.invoke('LeaveMatchGroup', args: [matchId]);
    }
  }

  Future<void> joinTournamentGroup(int tournamentId) async {
    if (_hubConnection != null && _isConnected) {
      await _hubConnection!.invoke('JoinTournamentGroup', args: [tournamentId]);
    }
  }

  Future<void> leaveTournamentGroup(int tournamentId) async {
    if (_hubConnection != null && _isConnected) {
      await _hubConnection!.invoke('LeaveTournamentGroup', args: [tournamentId]);
    }
  }

  Future<void> subscribeToCourtCalendar(int courtId) async {
    if (_hubConnection != null && _isConnected) {
      await _hubConnection!.invoke('SubscribeToCourtCalendar', args: [courtId]);
    }
  }

  Future<void> unsubscribeFromCourtCalendar(int courtId) async {
    if (_hubConnection != null && _isConnected) {
      await _hubConnection!.invoke('UnsubscribeFromCourtCalendar', args: [courtId]);
    }
  }
}
