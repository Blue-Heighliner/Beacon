# Navigation

Covers `Navigation`, `MainWindowViewModel`, and `ViewLocator`: how screens are created and swapped.

## Screen swapping

`Navigation` owns the current view model. Showing a screen resolves its view model from the container (registered transient, so each visit is fresh), awaits `ILoadable.Load` when the view model has one, and only then assigns it and raises `Changed`, so bound views never render empty state that is about to be replaced. `MainWindowViewModel` forwards `Changed` as a property change on `CurrentViewModel`. Screens depend on `INavigation` for the transitions they trigger, so none of them knows how another is built.

## View resolution

`ViewLocator` is registered as a data template. It maps a view model type name to its view by replacing `ViewModel` with `View` in the full type name and instantiating it by reflection. This is why trimming is disabled.

## Concurrent refresh

In `InventoryViewModel`, filter property changes trigger a refresh command that allows concurrent execution, so a fast second change is never dropped. A generation counter discards the result of any query that has been superseded, so an older, slower query cannot overwrite newer filter results.
