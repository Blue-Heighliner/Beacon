# Artemis Inventory

A desktop application for tracking technical equipment: barcode-driven serial number
entry, filtering, user accounts with badge or password sign-in, and Excel export.

Built with .NET 9 and Avalonia UI. Data is stored in a local SQLite database. The
application makes **no network calls of any kind** and is designed to run on
disconnected machines.

## Deploying to a target machine

Deployment is a single file copy — there is no installer and nothing to configure.

1. Build `Artemis.exe` (see below), or download it from the latest GitLab pipeline
   under **Build → Pipelines → build:windows-exe → Download artifacts**.
2. Copy `Artemis.exe` to the target machine, anywhere the user can write to
   (Desktop, `C:\Tools\Artemis\`, a shared drive, a USB stick).
3. Double-click it.

**Target machine requirements:** 64-bit Windows 10 (1607 or later) or Windows 11.
Nothing else. The .NET runtime and every native dependency are embedded in the exe,
so no .NET installation, no admin rights, and no internet connection are required.

### First run

The application creates its database and a built-in administrator account the first
time it starts:

| Username | Password |
| --- | --- |
| `admin` | `TestAdmin123` |

**Change this password immediately** via *My Account → Change Password*. The default
is hardcoded for initial setup and is not a secret.

### Where data is stored

```
%LOCALAPPDATA%\Artemis\inventory.db
```

This is per-Windows-user and per-machine. It is created automatically on first run
and reused thereafter; the schema upgrades itself in place when the app is updated.

> **Important deployment consideration:** because the database is local, each machine
> holds its own separate inventory. Two people running the exe on two computers will
> **not** see each other's items or share user accounts. If you need a single shared
> inventory across a team, that requires moving the database to a shared location or
> a server — it is not something this build does today.

To back up or transfer data, copy `inventory.db`. To reset an installation
completely, delete it — the app rebuilds it (and the default admin) on next launch.

## Building the executable

Building requires internet access on the **build** machine only (to download NuGet
packages). The resulting exe is what runs offline.

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download)

```powershell
git clone https://gitlab.com/bowtiesrcool679/artemis.git
cd artemis
.\publish.ps1
```

Output: `publish\win-x64\Artemis.exe` (~49 MB, single file).

To write the exe straight to another folder:

```powershell
.\publish.ps1 -OutputDirectory "D:\Deploy"
```

Or invoke the publish profile directly, without the script:

```powershell
dotnet publish -p:PublishProfile=SelfContained
```

### How the packaging works

`Properties/PublishProfiles/SelfContained.pubxml` controls the output:

- `SelfContained` — embeds the .NET 9 runtime, so the target needs no .NET install.
- `PublishSingleFile` + `IncludeNativeLibrariesForSelfExtract` — folds every managed
  assembly and native library (Avalonia, Skia, SQLite) into one exe.
- `EnableCompressionInSingleFile` — roughly halves the file size.
- `PublishTrimmed` is deliberately **off**. Trimming would strip types that
  `ViewLocator` resolves by reflection at runtime, breaking navigation between views.

## Development

```powershell
dotnet run
```

Project layout:

| Folder | Contents |
| --- | --- |
| `Models/` | `User`, `InventoryItem` |
| `Data/` | SQLite connection, schema creation/migration, repositories |
| `Services/` | Authentication, Excel export, input validation, theming |
| `ViewModels/` | MVVM view models (CommunityToolkit.Mvvm) |
| `Views/` | Avalonia XAML views |

Security notes: passwords are hashed with PBKDF2-SHA256 (100,000 iterations, random
per-user salt); badge barcodes are stored as SHA-256 hashes. All SQL uses
parameterized queries, with `Services/InputValidator.cs` screening user input for
injection patterns as a second layer.

## Distribution notes

`Artemis.exe` is unsigned, so Windows SmartScreen may show a
"Windows protected your PC" prompt on first launch (click *More info → Run anyway*),
and some endpoint protection suites flag unsigned single-file executables. For
fleet-wide rollout, sign the exe with your organization's code-signing certificate
or have it allow-listed by whoever manages endpoint policy.
