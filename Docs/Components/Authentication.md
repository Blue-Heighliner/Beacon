# Authentication

Covers `AuthService`, `InputValidator`, and `User`: registration, sign-in, and account management, with the signed-in user held in memory only.

## Credentials

A password is stored as a base64 PBKDF2 hash and per-user salt; a badge as the uppercase hex SHA-256 of its barcode. An account needs at least one of the two. Password comparison uses a fixed-time comparison. Badge input is trimmed for lookups and for self-service and admin changes, while registration hashes the barcode exactly as scanned, so the scanner's trailing newline handling must stay consistent across both paths.

## Uniqueness

Username (case-insensitive), badge hash, and DOD ID hash are each unique. `AuthService` checks before writing to give a friendly message, and the database constraints remain the final guard against races.

## Built-in admin

`EnsureAdminAccount` creates `admin` with a well-known default password and DOD ID `0000000000` only when no account named `admin` exists. Admin accounts cannot be deleted from the UI, and an admin cannot delete themselves.

## Authorization

Admin-only operations check `CurrentUser.IsAdmin` inside `AuthService` rather than trusting the UI, so a view model cannot bypass them. When an operation changes the signed-in user's own record, `CurrentUser` is replaced with the updated copy.

## Input screening

`InputValidator` rejects text containing statement separators, comment markers, quote-plus-keyword sequences, and tautologies. Patterns are deliberately narrow so serials such as `SN-SELECT-01` pass. It runs before any validation message that would echo input.
