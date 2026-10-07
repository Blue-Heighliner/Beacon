# Storage

Covers `Database`, `DodIdCipher`, `UserRepository`, and `ItemRepository`: the SQLite file, its schema upgrades, and DOD ID protection.

## Connections

Every repository call opens its own connection from `IDatabase.Open` and disposes it. Pooling is disabled so the file handle is released as soon as the connection is, which lets the file be deleted or backed up while the app runs and keeps integration tests from holding temp files open.

## Schema and migration

`Initialize` creates missing tables, then upgrades older databases in place: columns added since the first build (`DodId`, `IsAdmin`, `DodIdHash`) are added with `ALTER TABLE`, and a legacy unique index on the plaintext `DodId` is dropped in favor of a partial unique index on `DodIdHash`. Every step is idempotent, so it runs on every start.

## DOD ID legacy rows

Early builds stored the DOD ID as ten plaintext digits. Initialization finds such rows by shape (exactly ten ASCII digits), rewrites them to the encrypted value plus hash, and leaves already-encrypted rows alone, so re-running is safe.

## Timestamps

`CreatedAt` is stored as an ISO-8601 round-trip string in UTC. Because that format sorts chronologically as text, date filters and `ORDER BY` operate on the string directly. Filter dates entered as local calendar days are converted to a UTC inclusive lower and exclusive upper bound by the view model.

## DOD ID cipher

On Windows the value is DPAPI-protected for the current user with a fixed entropy string that scopes the blob to this purpose. Elsewhere it is AES-GCM, stored as base64 of nonce, tag, and ciphertext, with the key generated on first use and written with user-only permissions.
