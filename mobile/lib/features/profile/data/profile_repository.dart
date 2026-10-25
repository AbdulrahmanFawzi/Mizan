import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';

class Profile {
  const Profile({
    required this.name,
    required this.email,
    required this.currency,
    required this.monthlyIncome,
    required this.periodStartDay,
  });

  factory Profile.fromJson(Map<String, dynamic> json) => Profile(
        name: json['name'] as String,
        email: json['email'] as String,
        currency: json['currency'] as String,
        monthlyIncome: (json['monthlyIncome']['amount'] as num).toDouble(),
        periodStartDay: json['periodStartDay'] as int,
      );

  final String name;
  final String email;
  final String currency;
  final double monthlyIncome;
  final int periodStartDay;
}

class ProfileRepository {
  ProfileRepository(this._dio);

  final Dio _dio;

  Future<Profile> getMe() async {
    final response = await apiCall(() => _dio.get<Map<String, dynamic>>('/api/me'));
    return Profile.fromJson(response.data!);
  }

  Future<Profile> update({required String name, required double monthlyIncome, required int periodStartDay}) async {
    final response = await apiCall(() => _dio.put<Map<String, dynamic>>('/api/me', data: {
          'name': name,
          'monthlyIncome': monthlyIncome,
          'periodStartDay': periodStartDay,
        }));
    return Profile.fromJson(response.data!);
  }

  // UC-06 / BR-U3: the API deletes the user and all their financial data.
  Future<void> deleteAccount() => apiCall(() => _dio.delete<void>('/api/me'));
}

final profileRepositoryProvider = Provider<ProfileRepository>(
  (ref) => ProfileRepository(ref.watch(dioProvider)),
);

final profileProvider = FutureProvider.autoDispose<Profile>(
  (ref) => ref.watch(profileRepositoryProvider).getMe(),
);
