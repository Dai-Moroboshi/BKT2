import 'package:flutter/foundation.dart';
import '../config/api_config.dart';
import '../models/tournament_model.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

class TournamentProvider extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();
  final SignalRService _signalRService = SignalRService();

  List<TournamentModel> _tournaments = [];
  TournamentModel? _selectedTournament;
  List<MatchModel> _myMatches = [];
  bool _isLoading = false;
  String? _error;

  List<TournamentModel> get tournaments => _tournaments;
  TournamentModel? get selectedTournament => _selectedTournament;
  List<MatchModel> get myMatches => _myMatches;
  bool get isLoading => _isLoading;
  String? get error => _error;

  TournamentProvider() {
    _setupSignalR();
  }

  void _setupSignalR() {
    _signalRService.onMatchScoreUpdate = (data) {
      final matchId = data['matchId'] as int?;
      if (matchId != null && _selectedTournament != null) {
        fetchTournament(_selectedTournament!.id);
      }
    };
  }

  Future<void> fetchTournaments({String? status}) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get(
        ApiConfig.tournaments,
        queryParameters: status != null ? {'status': status} : null,
      );

      if (response.data['success'] == true) {
        _tournaments = (response.data['data'] as List)
            .map((t) => TournamentModel.fromJson(t))
            .toList();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchTournament(int id) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get('${ApiConfig.tournaments}/$id');
      if (response.data['success'] == true) {
        _selectedTournament = TournamentModel.fromJson(response.data['data']);
        await _signalRService.joinTournamentGroup(id);
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> joinTournament(int tournamentId, {String? teamName}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await _apiClient.post(
        '${ApiConfig.tournaments}/$tournamentId/join',
        data: {
          'teamName': teamName,
        },
      );

      if (response.data['success'] == true) {
        await fetchTournament(tournamentId);
        return true;
      } else {
        _error = response.data['message'] ?? 'Đăng ký thất bại';
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return false;
  }

  Future<void> fetchMyMatches() async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get('${ApiConfig.matches}/my');
      if (response.data['success'] == true) {
        _myMatches = (response.data['data'] as List)
            .map((m) => MatchModel.fromJson(m))
            .toList();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void leaveTournamentGroup(int id) {
    _signalRService.leaveTournamentGroup(id);
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
