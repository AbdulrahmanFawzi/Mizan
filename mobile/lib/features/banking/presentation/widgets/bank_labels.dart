import 'package:flutter/material.dart';

import '../../../../l10n/app_localizations.dart';
import '../../data/banking.dart';

String accountKindLabel(AccountKind kind, AppLocalizations l10n) => switch (kind) {
      AccountKind.current => l10n.accountCurrent,
      AccountKind.savings => l10n.accountSavings,
      AccountKind.creditCard => l10n.accountCreditCard,
      AccountKind.chargeCard => l10n.accountChargeCard,
      AccountKind.prepaid => l10n.accountPrepaid,
      AccountKind.eMoney => l10n.accountEMoney,
      AccountKind.other => l10n.accountOther,
    };

IconData accountKindIcon(AccountKind kind) => switch (kind) {
      AccountKind.current => Icons.account_balance_rounded,
      AccountKind.savings => Icons.savings_rounded,
      AccountKind.creditCard || AccountKind.chargeCard => Icons.credit_card_rounded,
      AccountKind.prepaid => Icons.card_giftcard_rounded,
      AccountKind.eMoney => Icons.account_balance_wallet_rounded,
      AccountKind.other => Icons.account_balance_outlined,
    };
