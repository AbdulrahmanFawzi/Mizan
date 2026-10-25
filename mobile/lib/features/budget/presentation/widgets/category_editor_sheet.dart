import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../core/theme/mizan_colors.dart';
import '../../../../core/widgets/amount_field.dart';
import '../../../../core/widgets/ui.dart';
import '../../../../core/widgets/validators.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/auth_scaffold.dart' show FormErrorText;
import '../../../categories/data/category.dart';
import '../../../categories/presentation/category_label.dart';
import '../../../categories/presentation/category_visual.dart';
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
    final colors = context.mizan;
    final theme = Theme.of(context);
    final existing = widget.existing;
    final kind = existing?.kind ?? CategoryKind.regular;
    final isSavings = kind == CategoryKind.savings;

    return Padding(
      // Keep the form above the keyboard.
      padding: EdgeInsets.fromLTRB(20, 0, 20, 20 + MediaQuery.viewInsetsOf(context).bottom),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(children: [
              // The icon follows the name as it is typed, so the user sees how the category will look.
              ValueListenableBuilder(
                valueListenable: _name,
                builder: (_, value, _) {
                  final visual = categoryVisual(value.text, kind);
                  return IconTile(icon: visual.icon, color: visual.color, size: 48);
                },
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(_isEdit ? l10n.editCategory : l10n.addCategory, style: theme.textTheme.titleLarge),
                  if (_isSystem)
                    Text(categoryLabel(existing!.category, l10n), style: TextStyle(color: colors.textMuted)),
                ]),
              ),
            ]),
            const SizedBox(height: 20),
            if (_isSystem)
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(color: colors.fieldFill, borderRadius: BorderRadius.circular(14)),
                child: Row(children: [
                  Icon(Icons.lock_outline_rounded, size: 18, color: colors.textMuted),
                  const SizedBox(width: 8),
                  Expanded(child: Text(l10n.systemCategoryNote, style: TextStyle(color: colors.textMuted))),
                ]),
              )
            else
              TextFormField(
                key: const Key('categoryNameField'),
                controller: _name,
                autofocus: !_isEdit,
                decoration: InputDecoration(labelText: l10n.categoryName),
                maxLength: 50,
                validator: (v) => Validators.required(v, l10n),
              ),
            const SizedBox(height: 12),
            AmountField(
              key: const Key('categoryLimitField'),
              controller: _limit,
              compact: true,
              label: isSavings ? l10n.savingsTarget : l10n.monthlyLimit,
              helper: isSavings ? null : l10n.monthlyLimitHint,
              validator: (v) => Validators.nonNegativeAmount(v, l10n),
            ),
            const SizedBox(height: 20),
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
                icon: const Icon(Icons.delete_outline_rounded),
                label: Text(l10n.delete),
                style: TextButton.styleFrom(foregroundColor: colors.danger, minimumSize: const Size.fromHeight(48)),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
