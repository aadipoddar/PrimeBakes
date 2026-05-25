# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Prime Bakes is a cross-platform ERP for a bakery/food business (restaurant billing, store POS, inventory, double-entry accounting). One shared Blazor UI runs as both a Blazor Server web app and a .NET MAUI Hybrid app. Targets .NET 10 / C# 14. See `README.md` for the full feature/module catalog.

## Solution layout

Open `PrimeBakes.slnx`. Projects:

- **`PrimeBakes/PrimeBakes.Shared`** — Razor Class Library holding ALL pages, components, layouts, and UI service *interfaces*. This is where nearly all UI work happens; both hosts consume it.
- **`PrimeBakes/PrimeBakes.Web`** — Blazor Server host. `Program.cs` wires DI and provides web implementations of the device service interfaces.
- **`PrimeBakes/PrimeBakes`** — MAUI Hybrid host (Android/iOS/macOS/Windows). `MauiProgram.cs` wires DI with native implementations; `Platforms/` has per-OS code (push, Bluetooth, direct printing).
- **`PrimeBakesLibrary`** — all business logic: data access, models, exporting/printing/email. No UI. `net10.0`, `Nullable disabled`.
- **`DBPrimeBakes`** — SSDT SQL Server database project (`Tables/`, `StoredProcedures/`, `Views/`). All data access goes through stored procedures.
- **`PushNotificationsAPI`** — ASP.NET Core API fronting Azure Notification Hubs (API-key auth).
- **`ExcelImport`** — one-off bulk data import console tool.

## Build & run

```powershell
# Web app (fastest dev loop)
dotnet run --project PrimeBakes/PrimeBakes.Web

# MAUI desktop / mobile (build a specific TFM)
dotnet build PrimeBakes/PrimeBakes/PrimeBakes.csproj -f net10.0-windows10.0.19041.0
dotnet build PrimeBakes/PrimeBakes/PrimeBakes.csproj -f net10.0-android

# Whole solution
dotnet build PrimeBakes.slnx
```

There is no test project. Verify changes by running the Web host.

The SQL project is built/published from Visual Studio (right-click `DBPrimeBakes` → Publish) using the profiles in `DBPrimeBakes/*.publish.xml` (Local / AzureTesting / Azure).

## Secrets & connection target

`PrimeBakesLibrary/DataAccess/Secrets.cs` is a `partial class`. Most values come from .NET **user secrets** / environment variables via `GetSecret(...)`; connection strings and a few constants are inline. **`SqlDataAccess._databaseConnection` is hardcoded to `Secrets.LocalConnectionString`** — switch the field there to point at Azure vs. local. `SqlDataAccess.SetupConfiguration()` (called once at startup in both hosts) registers the Syncfusion license, Dapper command timeout, and `DateOnly`/`TimeOnly` type handlers.

## Data access pattern (follow this exactly)

All DB access flows through `PrimeBakesLibrary/DataAccess/SqlDataAccess.cs` (Dapper, `CommandType.StoredProcedure` only — never inline SQL):

- `SqlDataAccess.LoadData<T,U>(sproc, params, txn?)` → `List<T>`. Insert sprocs return the new `Id` as `int`, so inserts call `LoadData<int, dynamic>(...).FirstOrDefault()`.
- `SqlDataAccess.SaveData(sproc, params, txn?)` for fire-and-forget execs (e.g. deletes).
- Generic helpers in `Data/Common/CommonData.cs` cover the common queries: `LoadTableData<T>`, `LoadTableDataById<T>`, `LoadTableDataByStatus<T>`, `LoadTableDataByMasterId<T>` (loads detail rows of a transaction), `LoadTableDataByDate<T>`, etc. Prefer these over writing a new load sproc.
- **String name constants live in `DataAccess/DatabaseNames.cs`**: `TableNames`, `ViewNames`, `StoredProcedureNames`. Always reference these (e.g. `TableNames.RawMaterial`), never string literals.

### Transactions & soft-delete

`SqlDataAccessTransaction` wraps a connection + `IDbTransaction`. Multi-table transaction writes use this shape (see `Data/Restaurant/Bill/BillData.cs` as the canonical example):

```csharp
using SqlDataAccessTransaction txn = new();
try
{
    txn.StartTransaction();
    // ... multiple Insert/Save calls, all passing txn ...
    txn.CommitTransaction();
}
catch { txn.RollbackTransaction(); throw; }
```

A transactional `SaveTransaction(...)` method typically calls itself: the outer call (txn == null) opens the transaction, exports the "previous" invoice, then re-invokes itself with the live `txn`; the inner call does the real work. Notifications fire only *after* commit.

Records are **soft-deleted**, not removed: set `Status = false` and re-insert (the insert sproc is an upsert keyed on `Id`). "Delete" of a transaction also reverses its `ProductStock` / `RawMaterialStock` rows and the linked `FinancialAccounting` entry. Sales/bills auto-post double-entry accounting via `FinancialAccountingData.SaveTransaction`, gated on `LocationId == 1` (main location) and driven by ledger IDs read from `SettingsData.LoadSettingsByKey(SettingsKeys.*)`.

`Data/` is organized by domain (`Store/`, `Restaurant/`, `Inventory/`, `Accounts/`, `Operations/`, `Common/`); data classes are `static`.

## UI conventions

- **Routes**: every page route is a `const` in `PrimeBakesLibrary/Data/Common/PageRouteNames.cs`. Pages declare `@attribute [Route(PageRouteNames.X)]` rather than `@page`. Navigation uses these constants.
- **Code-behind**: pages are `.razor` + `.razor.cs` partial classes. State fields are `_camelCase`; loading/processing flags are `_isLoading` / `_isProcessing`.
- **Auth**: pages load the user in `OnAfterRenderAsync(firstRender)` via `AuthenticationService.ValidateUser(..., [UserRoles.X], ...)` which redirects if the passcode-based session lacks the role. Roles: Admin, Sales, Order, Inventory, Accounts.
- **Components**: Syncfusion Blazor (`SfGrid`, `SfTextBox`, `SfNumericTextBox`, `SfMenu`, autocomplete) + MudBlazor. Reuse the shared wrappers in `PrimeBakes.Shared/Components/` (`Header`, `Footer`, `AnimatedLoader`, `IconButton`, `AutoCompleteWithAdd`, `DeleteConfirmationDialog`, `RecoverConfirmationDialog`, `ToastNotification`) instead of raw components.
- **Keyboard shortcuts** via `MudHotkey` are standard on entry pages: Ctrl+N new, Ctrl+S save, Ctrl+B back, Ctrl+E Excel, Ctrl+P PDF, Ctrl+Delete toggle deleted, Insert edit, Delete delete/recover. A `SfMenu` mirrors these.
- **Device capabilities** (printing, Bluetooth, notifications, sound, vibration, storage, updates, form factor) are abstracted as interfaces in `PrimeBakes.Shared/Services/`, implemented per-host. Inject the interface; never call platform APIs from shared UI.

## Models, exporting, naming

- Models live in `PrimeBakesLibrary/Models/<Domain>/`, suffixed `Model` (`RawMaterialModel`); transaction line items use `...DetailModel` (linked by `MasterId`) and UI carts use `...CartModel`.
- Exporting (`PrimeBakesLibrary/Exporting/`) generates PDF (Syncfusion.Pdf) / Excel (Syncfusion.XlsIO) invoices & reports, thermal-print payloads, and MailKit email. Per-transaction exporters live under `Exporting/<Domain>/<Entity>/`.
- When adding a feature, create the matching pieces in lockstep: SQL table + stored procedures in `DBPrimeBakes`, name constants in `DatabaseNames.cs`, model(s), a `static` data class, the route constant, and the page (`.razor` + `.razor.cs`). Match the structure of an existing sibling (e.g. copy `RawMaterial` for a master, `Bill`/`Sale` for a transaction).
