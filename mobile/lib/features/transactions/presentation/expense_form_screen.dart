import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/format/formatters.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/widgets/validators.dart';
import '../../../l10n/app_localizations.dart';
import '../../auth/presentation/auth_scaffold.dart' show FormErrorText;
import '../../categories/data/category.dart';
import '../../categories/presentation/category_label.dart';
import '../application/transactions_controller.dart';
import '../data/transaction.dart';

// One screen for both "add" (existing == null) and "edit".
class ExpenseFormScreen extends ConsumerStatefulWidget {
  const ExpenseFormScreen({super.key, this.existing});

  final Transaction? existing;

  @override
  ConsumerState<ExpenseFormScreen> createState() => _ExpenseFormScreenState();
}

class _ExpenseFormScreenState extends ConsumerState<ExpenseFormScreen> {
  final _formKey = GlobalKey<FormState>();
  late final _amount = TextEditingController(text: _initialAmount());
  late final _merchant = TextEditingController(text: widget.existing?.merchant ?? '');
  late DateTime _date = widget.existing?.date ?? DateUtils.dateOnly(DateTime.now());
  String? _categoryId;
  bool _saving = false;
  String? _error;

  bool get _isEdit => widget.existing != null;

  @override
  void initState() {
    super.initState();
    _categoryId = widget.existing?.categoryId;
  }

  @override
  void dispose() {
    _amount.dispose();
    _merchant.dispose();
    super.dispose();
  }

  // 50.0 shows as "50", 12.5 as "12.5".
  String _initialAmount() {
    final amount = widget.existing?.amount;
    if (amount == null) return '';
    return amount == amount.roundToDouble() ? amount.toStringAsFixed(0) : amount.toString();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(2020),
      lastDate: DateUtils.dateOnly(DateTime.now()), // BR-T3: no future dates.
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final input = ExpenseInput(
      amount: double.parse(_amount.text.trim()),
      categoryId: _categoryId!,
      date: _date,
      merchant: _merchant.text,
    );

    await _run(() async {
      final actions = ref.read(expenseActionsProvider);
      _isEdit ? await actions.update(widget.existing!.id, input) : await actions.add(input);
      messenger.showSnackBar(SnackBar(content: Text(l10n.expenseSaved)));
    });
  }

  Future<void> _delete() async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.deleteExpenseQuestion),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: Text(l10n.cancel)),
          TextButton(
            key: const Key('confirmDelete'),
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.delete),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    await _run(() async {
      await ref.read(expenseActionsProvider).delete(widget.existing!.id);
      messenger.showSnackBar(SnackBar(content: Text(l10n.expenseDeleted)));
    });
  }

  // Shared loading / error handling for save and delete. Closes the screen on success.
  Future<void> _run(Future<void> Function() action) async {
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      await action();
      if (mounted) context.pop();
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, AppLocalizations.of(context)));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final categories = ref.watch(categoriesProvider);
    final locked = widget.existing?.isGoalContribution ?? false;

    return Scaffold(
      appBar: AppBar(
        title: Text(_isEdit ? l10n.editExpense : l10n.addExpense),
        actions: [
          if (_isEdit)
            IconButton(
              key: const Key('deleteExpense'),
              tooltip: l10n.delete,
              onPressed: _saving ? null : _delete,
              icon: const Icon(Icons.delete_outline),
            ),
        ],
      ),
      body: switch (categories) {
        // While a refresh is running, Riverpod keeps the OLD list as AsyncData (with isLoading = true).
        // The form picks its default category from this list, so it waits for the fresh one.
        AsyncData(value: final list, isLoading: false) => _buildForm(context, l10n, list, locked),
        AsyncError(:final error) => Center(child: Text(errorMessage(error, l10n))),
        _ => const Center(child: CircularProgressIndicator()),
      },
    );
  }

  Widget _buildForm(BuildContext context, AppLocalizations l10n, List<Category> categories, bool locked) {
    // Default for a new expense: the first category the user created, otherwise "Other".
    _categoryId ??= (categories.where((c) => c.kind == CategoryKind.regular).firstOrNull ??
            categories.firstWhere((c) => c.kind == CategoryKind.other))
        .id;

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextFormField(
            key: const Key('amountField'),
            controller: _amount,
            autofocus: !_isEdit,
            decoration: InputDecoration(labelText: l10n.amount),
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            validator: (v) => Validators.positiveAmount(v, l10n),
          ),
          const SizedBox(height: 16),
          DropdownButtonFormField<String>(
            initialValue: _categoryId,
            decoration: InputDecoration(labelText: l10n.category, helperText: locked ? l10n.goalCategoryLocked : null),
            items: [
              for (final category in categories)
                DropdownMenuItem(value: category.id, child: Text(categoryLabel(category, l10n))),
            ],
            onChanged: locked ? null : (id) => setState(() => _categoryId = id),
          ),
          const SizedBox(height: 16),
          InkWell(
            onTap: _pickDate,
            child: InputDecorator(
              decoration: InputDecoration(labelText: l10n.date, suffixIcon: const Icon(Icons.calendar_today_outlined)),
              child: Text(context.longDate(_date)),
            ),
          ),
          const SizedBox(height: 16),
          TextFormField(
            key: const Key('merchantField'),
            controller: _merchant,
            decoration: InputDecoration(labelText: l10n.merchantOptional),
            maxLength: 100,
          ),
          const SizedBox(height: 8),
          FormErrorText(_error),
          FilledButton(
            key: const Key('saveExpense'),
            onPressed: _saving ? null : _save,
            child: _saving
                ? const SizedBox.square(dimension: 22, child: CircularProgressIndicator(strokeWidth: 2))
                : Text(l10n.save),
          ),
        ],
      ),
    );
  }
}
