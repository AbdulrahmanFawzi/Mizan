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
