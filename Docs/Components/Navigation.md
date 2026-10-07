# Navigation

Covers `MainWindowViewModel` and `ViewLocator`: how screens are created and swapped.

## Screen swapping

`MainWindowViewModel` exposes one `CurrentViewModel`. Showing a screen constructs its view model, awaits any `Load`, and only then assigns it, so bound views never render empty state that is about to be replaced. Screens receive `Func<Task>` callbacks for the transitions they trigger (sign-in succeeded, back, sign out) rather than a reference to the navigator.

## View resolution

`ViewLocator` is registered as a data template. It maps a view model type name to its view by replacing `ViewModel` with `View` in the full type name and instantiating it by reflection. This is why trimming is disabled.

## Concurrent refresh

In `InventoryViewModel`, filter property changes trigger a refresh command that allows concurrent execution, so a fast second change is never dropped. A generation counter discards the result of any query that has been superseded, so an older, slower query cannot overwrite newer filter results.
