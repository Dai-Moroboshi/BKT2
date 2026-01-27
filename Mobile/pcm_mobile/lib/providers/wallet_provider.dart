import 'package:flutter/foundation.dart';
import '../config/api_config.dart';
import '../models/wallet_transaction_model.dart';
import '../services/api_client.dart';
import '../services/signalr_service.dart';

class WalletProvider extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();
  final SignalRService _signalRService = SignalRService();

  double _balance = 0;
  List<WalletTransactionModel> _transactions = [];
  List<WalletTransactionModel> _pendingTransactions = [];
  bool _isLoading = false;
  String? _error;

  double get balance => _balance;
  List<WalletTransactionModel> get transactions => _transactions;
  List<WalletTransactionModel> get pendingTransactions => _pendingTransactions;
  bool get isLoading => _isLoading;
  String? get error => _error;

  WalletProvider() {
    _setupSignalR();
  }

  void _setupSignalR() {
    _signalRService.onWalletUpdate = (newBalance) {
      _balance = newBalance;
      notifyListeners();
    };
  }

  Future<void> fetchBalance() async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get(ApiConfig.walletBalance);
      if (response.data['success'] == true) {
        _balance = (response.data['data'] ?? 0).toDouble();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchTransactions({int page = 1, int pageSize = 20}) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get(
        ApiConfig.walletTransactions,
        queryParameters: {
          'page': page,
          'pageSize': pageSize,
        },
      );

      if (response.data['success'] == true) {
        _transactions = (response.data['data'] as List)
            .map((t) => WalletTransactionModel.fromJson(t))
            .toList();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchPendingTransactions() async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiClient.get('${ApiConfig.walletBalance}/pending');
      if (response.data['success'] == true) {
        _pendingTransactions = (response.data['data'] as List)
            .map((t) => WalletTransactionModel.fromJson(t))
            .toList();
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<WalletTransactionModel?> deposit(double amount, String? description, String paymentMethod) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await _apiClient.post(
        ApiConfig.walletDeposit,
        data: {
          'amount': amount,
          'description': description,
          'paymentMethod': paymentMethod,
        },
      );

      if (response.data['success'] == true) {
        final transaction = WalletTransactionModel.fromJson(response.data['data']);
        _transactions.insert(0, transaction);
        
        // Optimistic update or fetch
        await fetchBalance();
        
        notifyListeners();
        return transaction;
      } else {
        _error = response.data['message'] ?? 'Nạp tiền thất bại';
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
    return null;
  }

  Future<bool> approveTransaction(int transactionId, bool approve) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await _apiClient.put(
        '${ApiConfig.walletApprove}/$transactionId',
        data: {
          'approve': approve,
        },
      );

      if (response.data['success'] == true) {
        await fetchPendingTransactions();
        return true;
      } else {
        _error = response.data['message'] ?? 'Thao tác thất bại';
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
