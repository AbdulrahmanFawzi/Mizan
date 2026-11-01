# Phase 2 — Production features: decisions

## Email (UC-05, UC-07)

| ID | Decision |
|---|---|
| P2-1 | Password reset and email verification use a **6-digit code** sent by email (no links, so no deep-link setup). |
| P2-2 | **BR-U4:** a code works for 15 minutes, once, and dies after 5 wrong tries. Asking again within a minute sends nothing. Only the latest code counts. Only a hash is stored. |
| P2-3 | **BR-U5:** the app works without a verified email, but **linking a bank needs one** (API answers 403). |
| P2-4 | "Forgot password" answers the same whether the email has an account or not. A reset verifies the email, ends every other session, and logs the user in. |
| P2-5 | Emails go through the outbox, so the request is fast and the code text isn't kept after sending. The email is in Arabic, then English. |
| P2-6 | The email provider is chosen in Phase 3 (needs the domain). Until then `LogEmailSender` writes the email to the server log. |

API: `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`, `POST /api/me/email/send-code`,
`POST /api/me/email/verify`; `GET /api/me` has `emailVerified`.

## Data export (UC-09, PDPL right of access)

| ID | Decision |
|---|---|
| P2-7 | Settings → "Export my data" downloads one JSON file (`GET /api/me/export`, named `halalati-data-<date>.json`) and opens the phone's share sheet. |
| P2-8 | It holds everything we keep: profile, categories, budgets, all transactions, goals, notifications, bank connections and accounts, merchant rules. IBANs stay masked. |
| P2-9 | JSON, not PDF or CSV: complete and machine-readable (portability). A readable report can come later if users ask. |

## Server text in the app's language

| ID | Decision |
|---|---|
| P2-10 | The app sends `Accept-Language` (`ar` or `en`, the same language it shows). Insights, notifications and error messages come back in it. Arabic is the default. |
| P2-11 | Only the text language changes on the server; number and date formatting stay invariant (no Arabic decimal separator, no Hijri dates by accident). |
| P2-12 | The code throws in English. `ErrorText` (API) holds the Arabic version of every user-facing message; a test fails if a new fixed message has no Arabic. Provider errors (bank, consent) show a friendly message, never technical details. |
| P2-13 | Emails stay bilingual (Arabic, then English): they're sent in the background, and the user's language isn't stored yet. |
