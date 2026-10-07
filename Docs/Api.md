# API

The app has no public API: everything is internal to the single executable. This document covers the *design and flow* of its internal contracts, the interfaces the view models depend on, using those interfaces only. It does not restate member-level detail already covered in the source itself.

## Shape

`ServiceRegistration.AddBeacon` is the single place every service, repository, and view model is registered with the `Microsoft.Extensions.DependencyInjection` container, and `App.OnFrameworkInitializationCompleted` builds the provider and resolves `MainWindowViewModel` from it. Everything is consumed through its `I`-prefixed interface, including the things that touch the outside world: `IClock`, `IAppPaths`, `IKeyStore`, `IFilePicker`, `IUiTimerFactory`, `ICredentialHasher`. View models and `AuthService` are therefore unit-tested against mocks with no real time, disk, dialog, or timer involved, while the repositories are tested against a real SQLite file.

View models and views are the exception to one-interface-per-class: Avalonia compiled bindings and the `ViewLocator` resolve them by concrete type, so an interface would add nothing.

Models (`User`, `InventoryItem`) are immutable records. A change is expressed as a `with` copy that is passed to a repository, never as in-place mutation, so `AuthService.CurrentUser` is replaced whenever the signed-in user's own record changes.

## Flow

1. `MainWindow` opens and calls `MainWindowViewModel.Start`.
2. `Start` initializes the database (schema creation and in-place migration), ensures the built-in admin account exists, and shows `LoginViewModel`.
3. A successful sign-in through `IAuthService` makes the login view model call `INavigation.ShowInventory`, which resolves `InventoryViewModel` from the container and loads it before swapping it in, so a screen never appears half-populated.
4. From inventory the user can open the account screen, open user management (admins only), or sign out. Screens depend on `INavigation` and never construct each other.

## Results and errors

Operations that can fail for user-facing reasons on `IAuthService` return `(bool Success, string Error)`. The view model shows `Error` in its status banner; exceptions are reserved for genuinely unexpected failures. Every I/O method is async and takes an optional `CancellationToken`.
