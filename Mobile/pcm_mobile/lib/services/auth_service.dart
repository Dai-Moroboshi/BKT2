import 'dart:convert';
import '../config/api_config.dart';
import '../models/user_model.dart';
import 'api_client.dart';

class AuthService {
  final ApiClient _apiClient = ApiClient();
  UserModel? _cachedUser;

  Future<AuthResponse?> login(String email, String password) async {
    try {
      final response = await _apiClient.post(
        ApiConfig.login,
        data: {
          'email': email,
          'password': password,
        },
      );

      if (response.statusCode == 200) {
        final apiResponse = response.data;
        if (apiResponse['success'] == true && apiResponse['data'] != null) {
          final authResponse = AuthResponse.fromJson(apiResponse['data']);
          
          // Save token
          await _apiClient.setToken(authResponse.token);
          
          // Cache user data
          _cachedUser = authResponse.user;
          
          return authResponse;
        }
      }
      return null;
    } catch (e) {
      rethrow;
    }
  }

  Future<AuthResponse?> register(String email, String password, String fullName) async {
    try {
      final response = await _apiClient.post(
        ApiConfig.register,
        data: {
          'email': email,
          'password': password,
          'fullName': fullName,
        },
      );

      if (response.statusCode == 200) {
        final apiResponse = response.data;
        if (apiResponse['success'] == true && apiResponse['data'] != null) {
          final authResponse = AuthResponse.fromJson(apiResponse['data']);
          
          await _apiClient.setToken(authResponse.token);
          _cachedUser = authResponse.user;
          
          return authResponse;
        }
      }
      return null;
    } catch (e) {
      rethrow;
    }
  }

  Future<UserModel?> getCurrentUser() async {
    try {
      final response = await _apiClient.get(ApiConfig.me);

      if (response.statusCode == 200) {
        final apiResponse = response.data;
        if (apiResponse['success'] == true && apiResponse['data'] != null) {
          final user = UserModel.fromJson(apiResponse['data']);
          _cachedUser = user;
          return user;
        }
      }
      return null;
    } catch (e) {
      rethrow;
    }
  }

  Future<UserModel?> getCachedUser() async {
    return _cachedUser;
  }

  Future<bool> isLoggedIn() async {
    final token = await _apiClient.getToken();
    return token != null && token.isNotEmpty;
  }

  Future<void> logout() async {
    await _apiClient.clearToken();
    _cachedUser = null;
  }
}
