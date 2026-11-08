// Starter categories for the guided setup, as a share of the monthly income.
// Each name contains a word the bank categorization recognizes (CategoryResolver), so imported
// transactions land in them automatically.
class CategorySuggestion {
  const CategorySuggestion(this.arabic, this.english, this.share);

  final String arabic;
  final String english;
  final double share;

  String name(String languageCode) => languageCode == 'ar' ? arabic : english;

  // Rounded to 50 riyals, so the numbers look like a plan, not a calculation.
  double limitFor(double income) => (income * share / 50).round() * 50;
}

const categorySuggestions = [
  CategorySuggestion('السكن والفواتير', 'Rent & bills', 0.25),
  CategorySuggestion('البقالة', 'Groceries', 0.12),
  CategorySuggestion('المطاعم والقهوة', 'Restaurants & coffee', 0.08),
  CategorySuggestion('المواصلات والبنزين', 'Transport & fuel', 0.08),
  CategorySuggestion('التسوق', 'Shopping', 0.07),
  CategorySuggestion('الترفيه', 'Entertainment', 0.05),
  CategorySuggestion('الصحة', 'Health', 0.03),
];

const savingsShare = 0.10;
