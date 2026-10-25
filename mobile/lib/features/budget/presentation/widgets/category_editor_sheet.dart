import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/widgets/validators.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/auth_scaffold.dart' show FormErrorText;
import '../../../categories/data/category.dart';
import '../../../categories/presentation/category_label.dart';
import '../../application/budget_actions.dart';
import '../../data/budget.dart';

// Opens the editor as a bottom sheet. `existing == null` means "add a new category".
Future<void> showCategoryEditor(BuildContext context, {CategoryBudget? existing}) => showModalBottomSheet<void>(
      context: context,
      // Each tab has its own navigator. Without this, the sheet opens inside the tab,
      // underneath the tab bar and the "Add expense" button.
      useRootNavigator: true,
      isScrollControlled: true, // lets the sheet move up above the keyboard
      showDragHandle: true,
      builder: (_) => CategoryEditorSheet(existing: existing),
    );

class CategoryEditorSheet extends ConsumerStatefulWidget {
  const CategoryEditorSheet({super.key, this.existing});

  final CategoryBudget? existing;

  @override
  ConsumerState<CategoryEditorSheet> createState() => _CategoryEditorSheetState();
}

class _CategoryEditorSheetState extends ConsumerState<CategoryEditorSheet> {
  final _formKey = GlobalKey<FormState>();
  late final _name = TextEditingController(text: widget.existing?.name ?? '');
  late final _limit = TextEditingController(text: _format(widget.existing?.limit));
  bool _busy = false;
  String? _error;

  bool get _isEdit => widget.existing != null;
  bool get _isSystem => widget.existing?.category.isSystem ?? false;

  static String _format(double? value) {
    if (value == null) return '';
    return value == value.roundToDouble() ? value.toStringAsFixed(0) : value.toString();
  }

  @override
  void dispose() {
    _name.dispose();
    _limit.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final actions = ref.read(budgetActionsProvider);
    final limit = double.parse(_limit.text.trim());
    final name = _name.text.trim();

    await _run(() async {
      final existing = widget.existing;
      if (existing == null) {
        await actions.createCategory(name: name, limit: limit);
      } else {
        await actions.updateCategory(existing.category, name: name, limit: limit, oldLimit: existing.limit);
      }
      messenger.showSnackBar(SnackBar(content: Text(l10n.categorySaved)));
    });
  }

  Future<void> _delete() async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final existing = widget.existing!;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.deleteCategoryQuestion(existing.name)),
        content: Text(l10n.deleteCategoryExplain),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: Text(l10n.cancel)),
          TextButton(
            key: const Key('confirmDeleteCategory'),
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.delete),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    await _run(() async {
      await ref.read(budgetActionsProvider).deleteCategory(existing.categoryId);
      messenger.showSnackBar(SnackBar(content: Text(l10n.categoryDeleted)));
    });
  }

  Future<void> _run(Future<void> Function() action) async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await action();
      if (mounted) Navigator.pop(context);
    } catch (error) {
      if (mounted) setState(() => _error = errorMessage(error, AppLocalizations.of(context)));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final existing = widget.existing;
    final isSavings = existing?.kind == CategoryKind.savings;

    return Padding(
      // Keep the form above the keyboard.
      padding: EdgeInsets.fromLTRB(16, 0, 16, 16 + MediaQuery.viewInsetsOf(context).bottom),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              _isEdit ? l10n.editCategory : l10n.addCategory,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 16),
            if (_isSystem) ...[
              Text(categoryLabel(existing!.category, l10n), style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 4),
              Text(l10n.systemCategoryNote, style: Theme.of(context).textTheme.bodySmall),
            ] else
              TextFormField(
                key: const Key('categoryNameField'),
                controller: _name,
                autofocus: !_isEdit,
                decoration: InputDecoration(labelText: l10n.categoryName),
                maxLength: 50,
                validator: (v) => Validators.required(v, l10n),
              ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('categoryLimitField'),
              controller: _limit,
              decoration: InputDecoration(
                labelText: isSavings ? l10n.savingsTarget : l10n.monthlyLimit,
                helperText: isSavings ? null : l10n.monthlyLimitHint,
              ),
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              validator: (v) => Validators.nonNegativeAmount(v, l10n),
            ),
            const SizedBox(height: 16),
            FormErrorText(_error),
            FilledButton(
              key: const Key('saveCategory'),
              onPressed: _busy ? null : _save,
              child: Text(l10n.save),
            ),
            if (_isEdit && !_isSystem) ...[
              const SizedBox(height: 8),
              TextButton.icon(
                key: const Key('deleteCategory'),
                onPressed: _busy ? null : _delete,
                icon: const Icon(Icons.delete_outline),
                label: Text(l10n.delete),
                style: TextButton.styleFrom(foregroundColor: Theme.of(context).colorScheme.error),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
