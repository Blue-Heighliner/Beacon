# Architecture

This document explains the high-level design decisions behind the app, *why* it's built the way it is, not the class-by-class mechanics of *how*.

## One self-contained executable, fully offline

The app is distributed as a single self-contained, single-file executable per platform, and makes no network calls. Target machines are often disconnected, so it cannot rely on an installed runtime, an installer, or a server. The trade-off is a large binary (roughly 50 MB) and a per-machine database: two machines never share inventory or accounts. Trimming is left off because `ViewLocator` resolves views by reflection.

## Local SQLite in the user's data folder

Data lives in a SQLite file under the OS local application data folder rather than beside the executable. The executable is often run from synced or removable folders, and cloud sync on a live SQLite file risks lock errors.

## Hashes for secrets, encryption for display values

Passwords are hashed with PBKDF2-SHA256 (100,000 iterations, random per-user salt) and badge barcodes with SHA-256, since neither is ever shown again. The DOD ID is displayed and edited, so it cannot be a one-way hash: it is stored encrypted, next to a separate deterministic hash column used for exact-match lookup and the uniqueness constraint, so plaintext never reaches a WHERE clause. All SQL is parameterized; `InputValidator` screens typed text for injection markers only as a second layer.

## Platform-specific encryption of the DOD ID

DPAPI binds ciphertext to the Windows user with no key to manage, so it is used on Windows. DPAPI does not exist elsewhere, so other platforms use AES-GCM with a random key kept in a user-only file next to the database. The alternative of failing on Linux would have made the Linux build unusable; the accepted trade-off is that the Linux key file is only as protected as the user's file permissions.

## Hand-composed dependencies

Services and repositories are wired by hand in one composition root instead of a DI container. The graph is small and static, and a container would add a runtime dependency and a license notice for no benefit. Interfaces still exist for every service so tests can mock them.

## Immutable models

`User` and `InventoryItem` are records changed through `with` copies. This removes a class of bugs where a grid row, the signed-in user, and a repository result all alias one mutable object and drift apart.
