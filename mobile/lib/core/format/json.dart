// Small helpers so every model reads the API's JSON the same way.
double moneyAmount(Object? money) => ((money as Map<String, dynamic>)['amount'] as num).toDouble();

DateTime parseDate(Object? value) => DateTime.parse(value as String);

List<T> listOf<T>(Object? value, T Function(Map<String, dynamic>) fromJson) =>
    (value as List<dynamic>).cast<Map<String, dynamic>>().map(fromJson).toList();

// The API expects dates as yyyy-MM-dd. Built by hand (not with the user's locale),
// so an Arabic phone can never send Arabic digits or a Hijri date.
String apiDate(DateTime value) =>
    '${value.year.toString().padLeft(4, '0')}-${value.month.toString().padLeft(2, '0')}-${value.day.toString().padLeft(2, '0')}';
