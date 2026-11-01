# Mizan — Production Roadmap

Decided on 2026-10-25, after MVP 0:

| Decision | Choice |
|---|---|
| Launch scope | **Launch with Open Banking** (bank import), not manual-only |
| Legal entity | **Start a company** (needed for Lean and for a company App Store account) |
| Platforms | **iPhone and Android** |
| Running cost | **Low: ~100–250 SAR / month** |

## Two tracks in parallel

The company and the Lean contract take months. Instead of waiting, we build everything against our own
mock bank that follows the KSA Open Banking data shape. When Lean access arrives, we add one class
(`LeanBankingProvider`) and the rest of the system stays the same.

```
Business track (owner: you)          Build track (owner: us)
───────────────────────────          ───────────────────────
Company + commercial registration    Phase 1  Open Banking domain + mock bank
Business bank account                Phase 2  Production features
D-U-N-S number                       Phase 3  Infrastructure
Legal consult (PDPL, SAMA)           Phase 4  Security + legal documents
Contract with Lean (or Tarabut)  ──► Phase 5  Lean integration (needs the contract)
Apple + Google company accounts  ──► Phase 6  Beta, then launch
```

## Build track

**Status (2026-10-25):** Phase 1 done, and most of Phase 5 too: the Lean integration works end to end against Lean's sandbox
(link screen in the app, import, classification, review). What's left of Phase 5 needs the company and Lean's production
access. Details: `docs/domain-discovery/mvp1/` (design + "فروقات التنفيذ" in 05).

### Phase 1 — Open Banking with a mock bank
- Domain Discovery: use cases, rules, entities (bank connection, account, consent, sync).
- `IBankingTransactionProvider` interface + `MockBankProvider`.
- Import bank transactions, detect duplicates, and decide how bank and manual expenses live together.
- Automatic categorization (rules first; smarter later).
- Consent expiry and reconnection.
- Flutter: "link your bank" flow, accounts screen, sync status.

### Phase 2 — Production features
- Forgot password + email verification (needs an email provider).
- Server-side translation of insights and errors (Accept-Language).
- Data export (PDPL right of access).
- Push notifications (Firebase Cloud Messaging for iOS and Android).
- First-run onboarding.
- Android: run, test, and fix on an emulator.

### Phase 3 — Infrastructure (low budget)
- One server + PostgreSQL, preferably hosted in Saudi Arabia (PDPL). Cost comparison before choosing.
- Domain + HTTPS.
- Automatic daily backups, and a tested restore.
- CI/CD: tests on every push, then automatic deploy.
- Error tracking and uptime monitoring (free tiers).
- Staging environment, if the budget allows.

### Phase 4 — Security and legal documents
- Secrets out of the code, security headers, rate limits review.
- Privacy policy and terms of use (reviewed by a lawyer).
- Consent screens that follow SAMA's Open Banking customer experience rules.
- Penetration test before launch.

### Phase 5 — Lean integration (after the contract)
- `LeanBankingProvider`, Lean LinkSDK in Flutter, webhooks.
- Test against Lean's SAMA MockBank, then real banks.

### Phase 6 — Beta and launch
- TestFlight (iOS) and Play internal testing (Android) with a small group.
- Fix, then publish to both stores.

## Open questions
- Lean's pricing per connected user: ask during the sales call (it affects the monthly budget).
- Whether a manual-entry budgeting app alone falls under any SAMA rule: confirm in the legal consult.
- Lean or Tarabut: compare both once the company exists.
