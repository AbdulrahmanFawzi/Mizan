import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_client.dart';

enum CategoryKind { regular, other, savings }

class Category {
  const Category({required this.id, required this.name, required this.kind});

  factory Category.fromJson(Map<String, dynamic> json) => Category(
        id: json['id'] as String,
        name: json['name'] as String,
        kind: CategoryKind.values.byName((json['kind'] as String).toLowerCase()),
      );

  final String id;
  final String name;
  final CategoryKind kind;

  // "Other" and "Savings" always exist: they can't be renamed or deleted (BR-B3).
  bool get isSystem => kind != CategoryKind.regular;
}

class CategoryRepository {
  CategoryRepository(this._dio);

  final Dio _dio;

  Future<List<Category>> list() async {
    final response = await apiCall(() => _dio.get<List<dynamic>>('/api/categories'));
    return response.data!.cast<Map<String, dynamic>>().map(Category.fromJson).toList();
  }

  // Creating a category also gives it a limit in the current budget (BR-B10).
  Future<void> create({required String name, required double monthlyLimit}) => apiCall(
        () => _dio.post<void>('/api/categories', data: {'name': name, 'monthlyLimit': monthlyLimit}),
      );

  Future<void> rename(String id, String name) =>
      apiCall(() => _dio.put<void>('/api/categories/$id', data: {'name': name}));

  // The API moves the category's expenses to "Other" (BR-B4).
  Future<void> delete(String id) => apiCall(() => _dio.delete<void>('/api/categories/$id'));
}

final categoryRepositoryProvider = Provider<CategoryRepository>(
  (ref) => CategoryRepository(ref.watch(dioProvider)),
);

final categoriesProvider = FutureProvider.autoDispose<List<Category>>(
  (ref) => ref.watch(categoryRepositoryProvider).list(),
);
