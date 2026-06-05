---
name: lob-app-rulebook
description: >-
  The build rulebook for line-of-business apps built on the standard stack:
  .NET / C#, one shared Blazor UI (a Razor Class Library) hosted as both a
  Blazor Server web app and a .NET MAUI Hybrid app, Syncfusion Blazor + MudBlazor
  components, data access via Dapper calling stored procedures only, and an SSDT
  SQL Server database project. Use this skill whenever building or modifying such
  an app: adding or editing a page (dashboard, master/CRUD, report, or
  transaction), a data class, a model, an Excel/PDF export, or a stored
  procedure/table — and whenever deciding how a new feature should be structured.
  It defines the exact shape each piece must take so everything stays consistent,
  standardized, and simple. Trigger even when the user doesn't name a "page type"
  or "convention" — any time new code should match the established structure of a
  Blazor + Dapper + stored-procedure LOB app. Project-specific names differ per
  repo; and use this for the general architecture and patterns.
---
 
# Line-of-business app rulebook
 
The recipe book for **how to build anything** in a standard-stack LOB app. If you
are adding a page, a data class, an export, or a stored procedure, this tells you
the exact shape it must take. Find the established example in the repo, copy it,
rename it, and match it. The names below are placeholders — substitute the repo's
actual project/namespace/constant names.
 
> If the repo has its own `CLAUDE.md`, rulebook, or `.editorconfig`, those win for
> specifics. This skill is the general architecture and the patterns that recur
> across these apps.
 
## Assumed stack
 
- **.NET 10 / C# (latest).** Collection expressions `[]`, `is null` / `is not null`,
  expression-bodied members, pattern matching, file-scoped namespaces.
- **One shared Blazor UI** in a Razor Class Library (`<App>.Shared`), hosted twice:
  a **Blazor Server** web host (`<App>.Web`, `InteractiveServer` render mode) and a
  **.NET MAUI Hybrid** host (`<App>`, `Platforms/`).
- **Business logic + data access** in a no-UI class library (`<App>Library`),
  organized into feature folders.
- **UI toolkit:** Syncfusion Blazor (grids, inputs, dialogs) + MudBlazor (top-level
  menus via `MudMenu`, declarative hotkeys via `MudHotkey`, launchpad tabs via
  `MudTabs`), fronted by the repo's own reusable component wrappers.
- **Data access:** Dapper calling **stored procedures only** — no inline SQL, no
  EF Core.
- **Database:** an SSDT SQL Server project (`<App>DB`, MSBuild-only); tables and
  procs are `.sql` files registered in the `.sqlproj` `<Build>` group.
---
 
## Table of contents
 
0. [Guiding principles](#guiding-principles)
1. [Golden rules](#1-golden-rules)
2. [Solution layout & where things go](#2-solution-layout--where-things-go)
3. [Cross-cutting conventions (every page)](#3-cross-cutting-conventions)
4. [Reusable components](#4-reusable-components)
5. [Page type: Launchpad (tabbed dashboard)](#5-page-type-launchpad-tabbed-dashboard)
6. [Page type: Master / CRUD](#6-page-type-master--crud)
7. [Page type: Report](#7-page-type-report)
8. [Page type: Transaction — cart-based](#8-page-type-transaction--cart-based)
9. [Page type: Transaction — simple form + grid](#9-page-type-transaction--simple-form--grid)
10. [Data layer](#10-data-layer)
11. [Models](#11-models)
12. [Exports](#12-exports)
13. [SQL](#13-sql)
14. [End-to-end checklist](#14-end-to-end-checklist)
---
 
## Guiding principles
 
Three values, in priority order. They override personal preference and cleverness.
The rules below exist only to encode them.
 
1. **Consistency above all.** Every page/data/export/SQL of the same type must look
   and behave like its siblings. Find the canonical example, copy it, rename it.
   **Diverging from an established pattern is itself a defect** — even if the
   divergent code works, it taxes the next reader and breeds more drift. If a
   pattern is wrong, change it *everywhere* (and update the rulebook), never fork
   one spot.
2. **Standardization.** One way to do each thing: one place for names, one set of
   reusable components, one region order, one method skeleton. Don't add a second
   way to solve a problem the codebase already solves.
3. **Simplicity.** The simplest correct code on the current API. No dead code, no
   unused fields/usings, no speculative abstraction, no clever one-liners that hurt
   readability. Remove what you replace.
**Commit-time test:** *Does this look like the same person who wrote the rest of
the codebase wrote it?* If not, fix it until it does.
 
---
 
## 1. Golden rules
 
Non-negotiable, everywhere.
 
1. **Match the surrounding code.** Indentation matches the file (these repos use
   **tabs**). File-scoped namespaces. Latest-C# idioms.
2. **No hardcoded strings for procs/tables/routes/keys.** Proc & table names live in
   a central `DatabaseNames` class (grouped per area). Routes live in a central
   `PageRouteNames`. Local-storage keys, settings keys, etc. each have their own
   central constants. Reference the constant; never inline the literal.
3. **All data access = Dapper calling stored procedures.** Reads go through generic
   `CommonData` helpers where possible; writes go through a feature `Data` class.
4. **Records are never hard-deleted** (except deliberate join/period tables —
   [§10.4](#104-exception-hard-delete-tables)). "Delete" sets `Status = false`,
   "Recover" sets `Status = true`; both re-run the upsert proc.
5. **Every mutation writes an audit-trail row** inside the same transaction.
6. **Multi-step writes run inside the transaction wrapper** (e.g.
   `SqlDataAccessTransaction.Run`) and pass the transaction into every data call.
7. **Pages are `.razor` + `.razor.cs` code-behind.** No `@code` logic blocks. Logic
   sits in `#region` blocks in a fixed order.
8. **Verify external APIs against current docs** before coding (Syncfusion/MudBlazor,
   cloud SDKs) — match the version the repo references.
9. **Prefer the repo's component wrappers** over raw Syncfusion/MudBlazor/HTML.
---
 
## 2. Solution layout & where things go
 
| Project | Holds | Nullable |
|---|---|---|
| **`<App>Library`** | All business logic + data access. No UI. Feature folders with `Data`/`Models`/`Exports`. | per repo |
| **`<App>.Shared`** | Every page, component, layout, route, platform-service *interfaces*. Both hosts reference it. | per repo |
| **`<App>.Web`** | Blazor Server host; `Program.cs` wires DI + `InteractiveServer`. | — |
| **`<App>`** | MAUI Hybrid host; `MauiProgram.cs` wires DI; `Platforms/`. | — |
| **`<App>DB`** | SSDT SQL project; tables + procs as `.sql`; MSBuild-only. | — |
 
A feature folder in `<App>Library` is always three parts:
 
```
<Domain>/<Feature>/
	Models/    POCOs matching the DB table (+ overview / cart models)
	Data/      static XxxData with Save/Delete/Recover/Validate transaction
	Exports/   Excel/PDF builders delegating to the shared export utils
```
 
**Order you touch things for a new feature:** DB (table + procs) → `DatabaseNames`
constants → `<App>Library/<Domain>/<Feature>/{Models,Data,Exports}` → `PageRouteNames`
→ `<App>.Shared/Pages/...` page → launchpad tile.
 
**Platform-capability services** (file save/view, secure storage, notifications,
update, vibration, sound, form-factor): declare the interface in `<App>.Shared` and
implement it in **both** hosts.
 
---
 
## 3. Cross-cutting conventions
 
Apply to every page regardless of type.
 
### 3.1 File shape
 
`XxxPage.razor` is markup only (page-local `<style>` only when no global class
fits). `XxxPage.razor.cs` is `public partial class XxxPage` in a file-scoped
namespace, logic in `#region` blocks. Global `@using`/`@inject` live in the shared
`_Imports.razor`; the code-behind adds `using` for the feature's `Data`/`Models`/`Exports`.
 
### 3.2 Region order (code-behind)
 
In this order, omitting any that don't apply:
 
```
#region Load Data
#region Changed Events
#region Cart           // cart-based transaction pages only
#region Saving
#region Actions        // delete / recover / accept / reject
#region Exporting
#region Uploading <X>  // only if the page uploads a blob/file
#region Utilities      // grid context-menu handler, toggles, ResetPage, auto-refresh
```
 
Method order within a region matches the canonical sibling. Keep `ToggleDeleted` /
`ToggleDetailsView` in **Utilities**, not Actions.
 
### 3.3 Standard fields
 
```csharp
private UserModel _user;
private bool _isLoading = true;
private bool _isProcessing = false;
private bool _showDeleted = false;     // grids of soft-deletable rows
private bool _showAllColumns = false;  // report pages
 
private ToastNotification _toastNotification;
private SfGrid<TModel> _sfGrid;
private <FirstInput> _sfFirstFocus;     // first focusable control
 
// One generic confirmation dialog per page (§3.9), not per-action dialogs:
private ConfirmationDialog _confirmationDialog;
private string _confirmTitle = string.Empty;
private string _confirmMessage = string.Empty;
private Func<Task> _confirmAction;
```
 
### 3.4 Auth — first thing on first render
 
Authorize in `OnAfterRenderAsync(firstRender)`. The `catch` choice depends on
whether the page has a cart/draft:
 
- **Cart-based transaction pages** ([§8](#8-page-type-transaction--cart-based)) →
  `catch { await ResetPage(); }`. A failed init is most likely a corrupt
  local-storage draft; `ResetPage()` clears it and reloads fresh, fixing the cause.
- **Every other page** → `catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }`. No draft to recover, so leave
  rather than reload the failed URL (which risks an auth loop).
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
	if (!firstRender)
		return;
 
	try
	{
		_user = await AuthenticationService.ValidateUser(/* services */, [UserRoles.<Area>]);
		await InitializePage();   // or LoadData() for simple pages
	}
	catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }     // cart-based pages: catch { await ResetPage(); }
}
```
 
Reports require **both** the domain role and a `Reports` role. **Re-check
`_user.Admin` before any mutating action** and throw a clear permission message if
not allowed.
 
### 3.5 `_isProcessing` guard + finally
 
Every async action that mutates or exports:
 
```csharp
private async Task DoThing()
{
	if (_isProcessing)
		return;
	try
	{
		_isProcessing = true;
		StateHasChanged();
		await _toastNotification.ShowAsync("Processing", "…", ToastType.Info);
		// work
		await _toastNotification.ShowAsync("Done", "…", ToastType.Success);
	}
	catch (Exception ex)
	{
		await _toastNotification.ShowAsync("Error While …", ex.Message, ToastType.Error);
	}
	finally
	{
		_isProcessing = false;
		StateHasChanged();
	}
}
```

**`SaveTransaction` omits the trailing `StateHasChanged()`** in its `finally` (just
`_isProcessing = false;`): its success path ends in `ResetPage()` (full re-render) and
its error path's toast already re-renders, so it is redundant there. Delete/Recover/
Export keep the `StateHasChanged()`. Match the canonical `GroupPage`.
 
### 3.6 Toasts
 
All user feedback via the page's `ToastNotification`. Conventional titles:
`Processing`/`Loading` (Info); `Saved`/`Deleted`/`Recovered`/`Exported`/`Success`
(Success); `Error While Saving`/`Error While Exporting`/`Error` (Error); warnings
like `Cannot View` (Warning). Keep wording identical across pages.
 
### 3.7 Auth-failure escape hatch
 
There is no visible Back button, no Ctrl+B hotkey, and no `NavigateBack()` helper. Every feature page hangs off the single launchpad ([§5](#5-page-type-launchpad-tabbed-dashboard)). Non-cart pages handle a failed init by navigating directly to the launchpad inline:
 
```csharp
catch { NavigationManager.NavigateTo(PageRouteNames.Dashboard); }
```
 
Cart pages catch with `catch { await ResetPage(); }` instead, which clears the local-storage draft before reloading ([§3.4](#34-auth--first-thing-on-first-render)).
 
### 3.8 Hotkeys, menus & confirmations — MudBlazor
 
**Hotkeys are markup, not JS.** Drop `MudBlazor.MudHotkey` elements at the top of the
`.razor`, one per binding, each `Disabled="@_isProcessing"`, bound to the same method
the menu item calls:
 
```razor
<MudBlazor.MudHotkey Key=JsKey.KeyS KeyModifiers=[JsKeyModifier.ControlLeft] OnHotkeyPressed="SaveTransaction" Disabled="@_isProcessing" />
<MudBlazor.MudHotkey Key=JsKey.KeyF KeyModifiers=[JsKeyModifier.ControlLeft] OnHotkeyPressed="() => _sfFirstFocus.FocusAsync()" Disabled="@_isProcessing" />
<MudBlazor.MudHotkey Key=JsKey.Insert OnHotkeyPressed="EditSelectedItem" Disabled="@_isProcessing" />
<MudBlazor.MudHotkey Key=JsKey.Delete OnHotkeyPressed="DeleteRecoverSelectedItem" Disabled="@_isProcessing" />
```
 
**Top-of-page menus are `MudBlazor.MudMenu`/`MudMenuItem`** with a direct `OnClick` per
item — **not** `SfMenu`. No item `Id`s and **no `OnMenuSelected` switch**; the click
calls the method. Group under `MudMenu Label="File"`/`"Transaction"`/`"Open"`/`"Periods"`,
separate groups with `MudDivider`, each `MudMenu` is `Dense="true" Disabled="_isProcessing"`.
The **grid row** context menu is the only place the `Id`+switch pattern remains
(Syncfusion `ContextMenuItems` + `OnGridContextMenuItemClicked`).
 
**One generic `ConfirmationDialog` per page** handles delete/recover/accept/reject/
discard — there are no per-action dialog components. Stash a `Func<Task>` and show it:
 
```csharp
private async Task ShowConfirmation(string title, string message, Func<Task> action)
{
	_confirmTitle = title; _confirmMessage = message; _confirmAction = action;
	StateHasChanged();
	await _confirmationDialog.ShowAsync();
}
private async Task OnConfirmed()
{
	await _confirmationDialog.HideAsync();
	if (_confirmAction is not null) await _confirmAction();
	_confirmAction = null;
}
private async Task OnCancelled() { _confirmAction = null; await _confirmationDialog.HideAsync(); }
```
 
Keep bindings consistent and spell the shortcut out in the menu item text
("Export PDF (Ctrl + P)", "Show Deleted (Ctrl + Delete)" — exact same wording
everywhere). Typical set: `Ctrl+N` new, `Ctrl+S` save, `Ctrl+F`
focus first input, `Ctrl+R`/`F5` refresh, `Ctrl+Q` toggle detail columns,
`Ctrl+Delete` toggle show-deleted, `Insert` edit selected row, `Delete` delete/recover
row, `Ctrl+E`/`Ctrl+P` export Excel/PDF, `Alt+O`/`Alt+E`/`Alt+P`
view/export-Excel/export-PDF selected row. (No `Ctrl+B` — there is no back button.)
 
### 3.9 Layout skeleton
 
```razor
@* MudHotkey bindings (§3.8) go here, above the markup *@
 
@if (_isLoading || _user is null)
{
	<LoadingScreen />
}
else
{
	<div class="page-shell">
		<Header Title="…">
			<LeftContent>
				<MudBlazor.MudMenu Label="File" Dense="true" Disabled="_isProcessing">
					<MudBlazor.MudMenuItem Label="Save (Ctrl + S)" OnClick="SaveTransaction" />
					<MudBlazor.MudDivider />
					<MudBlazor.MudMenuItem Label="Export PDF (Ctrl + P)" OnClick="() => ExportMaster()" />
				</MudBlazor.MudMenu>
			</LeftContent>
		</Header>
		<div class="page-container">
			<div class="section-card">
				<div class="section-header"><div class="section-title-wrapper"> …icon… <h2 class="section-title">…</h2></div></div>
				<div class="section-body"> … </div>
			</div>
		</div>
		<Footer />
	</div>
}
 
<ConfirmationDialog @ref="_confirmationDialog" Title="@_confirmTitle" Message="@_confirmMessage"
					OnConfirm="OnConfirmed" OnCancel="OnCancelled" />
<ToastNotification @ref="_toastNotification" />
<CloseGuard Block="_isProcessing" />
```
 
The `ConfirmationDialog`, `ToastNotification`, and `CloseGuard` sit **outside** the
`_isLoading` branch so they live for the whole page. `CloseGuard` blocks window/tab
close and external navigation while `_isProcessing` is true.
 
Use global CSS classes (`page-shell`, `page-container`, `section-card`,
`section-header`, `section-title`, `filters-grid`, `form-field`, money classes like
`grid-debit`/`grid-credit`/`grid-total`). Add page-local `<style>` only when nothing
global fits, and delete dead CSS in the same change.
 
---
 
## 4. Reusable components
 
Reach for the repo's wrappers before raw Syncfusion/MudBlazor/HTML. The standard set:
 
| Component | Use for |
|---|---|
| `Header` (`LeftContent`/`RightContent`), `Footer`, `LoadingScreen` | Page chrome |
| `IconButton` | Toolbar icons (Excel/PDF export, refresh, view, delete, add) |
| `FioriTile` | Launchpad navigation tiles (one per feature page) |
| `BalanceInfoCard` (or similar) | Summary tiles on report pages |
| `CustomTextField`, `CustomNumericField<T>`, `CustomAutoComplete<T>`, `CustomDatePicker`, `CustomDateRangePicker`, `CustomCheckBox` | Form inputs |
| `ConfirmationDialog` | The **single** confirm dialog for all confirmations (delete, recover, accept, reject, discard) — stash a `Func<Task>` and call `ShowConfirmation(title, message, action)` (§3.8) |
| `DocumentUploadDialog` | Blob/file upload-download-remove |
| `ToastNotification` | All feedback |
| `CloseGuard` | Block window close / external nav while `_isProcessing` (`<CloseGuard Block="_isProcessing" />`) |
 
Grids are `SfGrid` with the shared CssClass and a Syncfusion `ContextMenuItems` menu
for row actions; top-of-page menus are `MudBlazor.MudMenu`/`MudMenuItem`, and hotkeys
are `MudBlazor.MudHotkey` (§3.8). Use `@ref` only — don't set a literal `ID=` on
`SfGrid`. Upload dialogs own their own `SfUploader` internally; pass data + callbacks,
don't hold a parent uploader reference.
 
---
 
## 5. Page type: Launchpad (tabbed dashboard)

**Purpose:** the single root navigation surface. Inspired by SAP Fiori — one page,
one tab per functional area, role-gated tabs, each tab is a vertical stack of
labelled sections holding a grid of `FioriTile`s that navigate to feature pages.
There is **no other dashboard**. The legacy per-area dashboard pages
(`AccountsDashboard`, `OperationsDashboard`, `Fleet*Dashboard`, etc.) are deprecated;
delete them when porting an older app and point every back-style navigation at this page.

### 5.1 Shape

- One route, mounted at `PageRouteNames.Dashboard` (typically `"/"`).
- Standard `_isLoading`/`_user` guard + `LoadingScreen`. Validate the user (no role
  filter — every authenticated user lands here; role gating happens per tab).
- Layout: `page-shell` → `Header` → `page-container.launchpad` →
  `MudBlazor.MudTabs` → one `MudTabPanel` per area, each with
  `Visible="_user.<Role>"` (and combined flags for cross-cutting tabs like
  `Reports && Fleet`) → inside each panel, one or more `<div class="launchpad-section">`
  blocks containing `<h2 class="launchpad-section-title">…</h2>` + a
  `<div class="dashboard-grid">` of `FioriTile`s → `Footer`.
- The first tab is **Analysis / Overview** — KPI tiles (active count, pending
  count, monthly totals). Tiles here may be non-clickable placeholders until live
  data is wired; that's fine.
- Subsequent tabs mirror the role areas, each grouped into sections (e.g.
  *Entry Pages*, *Reports*, *Master Data* / *Routes & Drivers*, *OMC*, *Documents*,
  *Vehicle Masters*).

### 5.2 Tile

```razor
<FioriTile Title="Trip"
           Subtitle="Record a new trip with route, driver and trip expenses"
           OnClick="() => NavigationManager.NavigateTo(PageRouteNames.Trip)">
	<IconContent>
		<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none"
		     stroke="currentColor" stroke-width="2" stroke-linecap="round"
		     stroke-linejoin="round">
			<!-- icon paths -->
		</svg>
	</IconContent>
</FioriTile>
```

Use inline SVG icons (no icon font), 24x24 viewbox, `stroke="currentColor"` so the
tile theme controls the colour. Pick a distinct icon per feature — reuse the same
icon across the report-list and the per-row export so users learn the symbol.

### 5.3 Rules

- **Every feature page is reachable from exactly one tile** on this page; that
  tile's `OnClick` uses a `PageRouteNames` constant. No orphan routes.
- Wrap every panel with `Visible="_user.<Role>"`; never branch with `@if` around the
  whole `MudTabPanel` (Razor compile errors if the wrapper has no body).
---
 
## 6. Page type: Master / CRUD
 
**Purpose:** CRUD a reference table. Form + grid of existing rows; edit-in-place;
soft delete + recover; Excel/PDF export of the list.
 
### 6.1 Fields
 
```csharp
private TModel _model = new();
private List<TModel> _items = [];
// + a _selectedXxx + List<XxxModel> per related dropdown
private readonly List<ContextMenuItemModel> _gridContextMenuItems =
[
	new() { Text = "Edit (Insert)", Id = "EditSelectedItem", IconCss = "e-icons e-edit", Target = ".e-content" },
	new() { Text = "Delete / Recover (Del)", Id = "DeleteRecoverSelectedItem", IconCss = "e-icons e-trash", Target = ".e-content" }
];
private <FirstInput> _sfFirstFocus;
private ConfirmationDialog _confirmationDialog;
private string _confirmTitle = string.Empty;
private string _confirmMessage = string.Empty;
private Func<Task> _confirmAction;   // the pending operation to run on confirm
```
 
### 6.2 Methods by region
 
- **Load Data:** `OnAfterRenderAsync` → `LoadData()`. Load the list via
  `CommonData.LoadTableData<T>(Names.X)`, load related lists, order them, resolve
  `_selectedXxx` from `_model`, filter `Status` when `!_showDeleted`, refresh grid,
  clear `_isLoading`, focus `_sfFirstFocus`.
- **Saving:** `SaveTransaction()` — guard `_isProcessing`; admin check; map
  `_selectedXxx?.Id ?? 0` onto the model; call
  `XxxData.SaveTransaction(_model, _user.Id, platform)`; toast; `ResetPage()`.
- **Actions:** `EditSelectedItem()`, one unified
  `DeleteRecoverTransaction(int id, bool isRecover)` (admin check, load fresh by id,
  set audit fields, call `XxxData.Delete/RecoverTransaction`, toast, `ResetPage()`),
  `DeleteRecoverSelectedItem()` (read selected row, branch on `Status`, call
  `ShowConfirmation(title, message, () => DeleteRecoverTransaction(id, !status))`),
  and the shared confirm trio `ShowConfirmation` / `OnConfirmed` (hide → invoke
  `_confirmAction`) / `OnCancelled` (§3.8).
- **Exporting:** one `ExportMaster(bool isExcel = false)` (PDF default) →
  `XxxExport.ExportMaster(_items, isExcel ? ReportExportType.Excel : ReportExportType.PDF)`
  → `SaveAndViewService.SaveAndView`. **Not** separate `ExportExcel`/`ExportPdf`.
- **Utilities:** `OnGridContextMenuItemClicked` switch (the only `Id`+switch left),
  `ToggleDeleted()` (`_showDeleted = !_showDeleted; await LoadData();`),
  `ResetPage() => PageRefresh.Request();`. No `OnMenuSelected`.
`platform` = `FormFactor.GetFormFactor() + FormFactor.GetPlatform()` (or repo
equivalent).
 
---
 
## 7. Page type: Report
 
**Purpose:** read-only grid over an `*_Overview` view with filters (date range +
entity dropdowns), aggregates, column show/hide, period presets, row actions, and
**auto-refresh**. Implements `IAsyncDisposable`.
 
### 7.1 Methods
 
Reports keep **two lists**: `_allXxxOverviews` (the raw date-range load) and
`_xxxOverviews` (what the grid binds). The expensive DB read happens only when the
date range changes; entity-dropdown filters re-slice the in-memory list.
 
- **Load Data:** `OnAfterRenderAsync` → `InitializePage()` → `LoadData()` (filter
  sources + default dates), `LoadXxxOverviews()`, `StartAutoRefresh()`, clear loading,
  focus.
- `LoadXxxOverviews()` — guard `_isProcessing`; toast Loading; load **all** rows for
  the date range via `CommonData.LoadTableDataByDate<TOverview>(Names.X_Overview, from, to)`
  into `_allXxxOverviews`; then `await ApplyFilters()`; `finally` clear processing.
- `ApplyFilters()` — set `_xxxOverviews = [.. _allXxxOverviews.Where(...)]` applying
  `(_showDeleted || x.Status)` and each `_selectedYyy is null || _selectedYyy.Id == 0
  || x.YyyId == _selectedYyy.Id`; order; refresh grid; `StateHasChanged()`. No DB hit.
- **Changed Events:** each entity `OnYyyChanged` sets its field then `await ApplyFilters()`
  (in-memory). `OnDateRangeChanged` and `HandleDatesChanged(DateRangeType)` (period
  presets) reset the dates and call `LoadXxxOverviews()` (re-query). A filter that
  changes another dropdown's candidate set (e.g. company → vehicles) reloads that
  dropdown before `ApplyFilters()`.
- **Exporting:** one whole-report `ExportReport(bool isExcel = false)` via
  `XxxReportExport.ExportReport(_xxxOverviews, …, _showAllColumns, _showDeleted, active filters)`,
  plus a per-row `ExportSelectedTransaction(bool isExcel = false)` and
  `ViewSelectedTransaction()` (decode transaction no, navigate via `AuthenticationService.NavigateToRoute`).
- **Actions:** admin-gated `DeleteRecoverTransaction(id, transactionNo, isRecover)`
  driven through `ShowConfirmation` + the shared confirm trio (§3.8).
- **Utilities:** `OnGridContextMenuItemClicked` switch, `ToggleDetailsView()` (flip
  `_showAllColumns`, refresh grid), `ToggleDeleted()` (flip then `await ApplyFilters()`),
  and the auto-refresh trio.
### 7.2 Auto-refresh + dispose (verbatim)
 
```csharp
private async Task StartAutoRefresh()
{
	var timerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
	var refreshMinutes = int.TryParse(timerSetting?.Value, out var minutes) ? minutes : 5;
	_autoRefreshCts = new CancellationTokenSource();
	_autoRefreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));
	_ = AutoRefreshLoop(_autoRefreshCts.Token);
}
 
private async Task AutoRefreshLoop(CancellationToken cancellationToken)
{
	try { while (await _autoRefreshTimer.WaitForNextTickAsync(cancellationToken)) await LoadXxxOverviews(); }
	catch (OperationCanceledException) { /* expected on dispose */ }
}
 
async ValueTask IAsyncDisposable.DisposeAsync()
{
	if (_autoRefreshCts is not null) { await _autoRefreshCts.CancelAsync(); _autoRefreshCts.Dispose(); }
	_autoRefreshTimer?.Dispose();
	GC.SuppressFinalize(this);
}
```
 
### 7.3 Grid
 
`SfGrid` with paging/sorting/resizing/filtering/grouping, Excel filter type, page
sizes `10/20/50/100/All`, `Toolbar=["Search"]`, shared CssClass. Money columns
`Format="N2" TextAlign=Right` with the money templates. Aggregates with Footer +
GroupFooter + GroupCaption templates. Detail-only columns use `Visible="_showAllColumns"`.
For an entity column, the active filter hides it (redundant) and the verbosity choice
sets the connective:
- **Always-visible column** that should only hide when it *is* the active filter →
  OR: `Visible="@(_showAllColumns || _selectedX is null || _selectedX.Id <= 0)"`.
- **Detail-only column** that should appear only in details view *and* not as the
  active filter → AND: `Visible="@(_showAllColumns && (_selectedX is null || _selectedX.Id <= 0))"`.
 
---
 
## 8. Page type: Transaction — cart-based
 
**Purpose:** create/edit a header + line-items ("cart") transaction with draft
persistence and invoice export.
 
### 8.1 Distinctive elements
 
- `[Parameter] public int? Id { get; set; }` — edit mode when present.
- A `_cart` `List<TCartModel>` + `_selectedCart` + an `SfGrid<TCartModel>` with an
  Edit/Delete context menu.
- **Local-storage draft** via the storage service + central key constants.
- Invoice export via `XxxInvoiceExport.ExportInvoice(id, InvoiceExportType.PDF|Excel)`.
### 8.2 Methods
 
- **Load Data:** `OnAfterRenderAsync` → `InitializePage()` → `LoadData()`,
  `ResolveTransaction()`, `LoadSelections()`, `LoadCart()`, clear loading,
  `SaveTransactionFile()`, focus.
- `ResolveTransaction()` tries in order: `LoadExistingTransaction()` (by `Id`) →
  `TryRestoreFromLocalStorage()` → `CreateNewTransaction()`; failure toasts and
  `await ResetPage()`.
- **Cart:** `OnItemChanged`, `AddItemToCart` (validate → append → clear → focus →
  `SaveTransactionFile`), `EditSelectedCartItem` (rehydrate then remove),
  `RemoveSelectedCartItem`.
- **Saving:** `UpdateFinancialDetails()` (clean cart, compute totals, resolve period
  + lock check, generate transaction no for new, stamp audit fields),
  `SaveTransactionFile()` (persist draft when unsaved & non-empty; delete otherwise),
  `SaveTransaction(bool savePDF=false, bool saveExcel=false)` (persist via
  `XxxData.SaveTransaction(header, lines)`, optional invoice, `ResetPage`).
- **Utilities:** `OnGridContextMenuItemClicked` switch, `DeleteLocalFiles()`,
  `ResetPage()` = `await DeleteLocalFiles(); PageRefresh.Request();`.
Both save methods guard `_isProcessing || _isLoading`. The `OnAfterRenderAsync`
`catch` calls `await ResetPage()` here (clears the draft) — this is the defining
difference from non-cart pages (see [§3.4](#34-auth--first-thing-on-first-render)).
 
---
 
## 9. Page type: Transaction — simple form + grid
 
**Purpose:** a transaction without a cart — either a single dedicated form launched
per record (`Id` param + `ResolveTransaction` + `LoadSelections`, no cart) or a
master-style grid+form with edit-in-place.
 
- Use the master template ([§6](#6-page-type-master--crud)) for grid+form
  edit-in-place; use the dedicated-form shape for a per-record form.
- On a failed init, navigate to the launchpad inline ([§3.7](#37-auth-failure-escape-hatch)) — same as every other feature page.
- Soft delete + recover unless the table is a deliberate hard-delete table
  ([§10.4](#104-exception-hard-delete-tables)).
- On save, stamp `Status`, `CreatedBy/At`, `LastModifiedBy/At`, and the platform
  string using the current server time and the form-factor helper.
---
 
## 10. Data layer
 
**Location:** `<App>Library/<Domain>/<Feature>/Data/XxxData.cs` — a `public static class`.
 
### 10.1 Standard members (master / simple feature)
 
```csharp
public static class XxxData
{
	private static async Task<int> InsertXxx(XxxModel x, SqlDataAccessTransaction transaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(Names.InsertXxx, x, transaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Xxx.");
 
	public static async Task DeleteTransaction(XxxModel x, int userId, string platform) =>
		await SqlDataAccessTransaction.Run(async transaction =>
		{
			x.Status = false;
			await InsertXxx(x, transaction);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = Names.Xxx, RecordNo = x.Name,
				CreatedBy = userId, CreatedFromPlatform = platform
			}, transaction);
		});
 
	public static async Task RecoverTransaction(XxxModel x, int userId, string platform) =>
		// identical, Status = true, AuditTrailActionTypes.Recover
 
	private static async Task ValidateTransaction(XxxModel item)
	{
		// 1. normalize: Name = Name?.Trim().ToUpper() ?? ""; nullable strings -> null if whitespace; Status = true
		// 2. required-field throws with a clear "… is required. Please …" message
		// 3. format checks (phone/email helpers)
		// 4. generate Code for new rows (item.Id == 0)
		// 5. uniqueness: load all, FirstOrDefault(x => x.Id != item.Id && x.Name.Equals(..., OrdinalIgnoreCase))
	}
 
	public static async Task<int> SaveTransaction(XxxModel x, int userId, string platform)
	{
		await ValidateTransaction(x);
		var isUpdate = x.Id > 0;
		var previous = isUpdate ? await CommonData.LoadTableDataById<XxxModel>(Names.Xxx, x.Id) : null;
		return await SqlDataAccessTransaction.Run(async transaction =>
		{
			var id = await InsertXxx(x, transaction);
			var diff = AuditTrailData.GetDifference(previous, x);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = isUpdate ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
				TableName = Names.Xxx, RecordNo = x.Name, RecordValue = diff,
				CreatedBy = userId, CreatedFromPlatform = platform
			}, transaction);
			return id;
		});
	}
}
```
 
### 10.2 Header + lines (cart) features
 
`InsertXxx` (header) + `InsertXxxLine` helpers; `ConvertCartToLines(cart, masterId)`;
`SaveTransaction(header, lines)` runs one transaction: upsert header → (for edits)
**soft-delete the existing lines** — load them by `MasterId`, set `Status = false`,
re-run the line upsert — then insert the new line set → write audit; return header id.
Lines are not hard-deleted on edit; they stay as `Status = 0` rows and the generic
`Load_TableData_By_MasterId` reader returns only the active set (see
`FinancialAccountingData`). The audit `RecordNo` is the
transaction's natural key.
 
### 10.3 Reads
 
Use generic `CommonData` helpers: `LoadTableData<T>`, `LoadTableDataById<T>`,
`LoadTableDataByStatus<T>`, `LoadTableDataByDate<T>`, `LoadTableDataByMasterId<T>`,
`LoadTableDataByCode<T>`, `LoadLastTableData<T>`, `LoadCurrentDateTime()`. These call
generic `Load_TableData*` procs — **most tables need only an `Insert_*` proc**, no
bespoke read proc. Write bespoke procs only for joins/overviews/aggregations.
 
### 10.4 Exception: hard-delete tables
 
Join/period tables intentionally **not** soft-deleted have **no `Status` column**, a
`Delete_Xxx` proc that runs a real `DELETE`, and a `DeleteTransaction` that calls it
and still writes an audit row. Their pages have **no recover / no "Show Deleted"**,
and the menu reads `"Delete (Del)"`. Use only for relationship/period rows that are
meaningless once removed; everything else soft-deletes.
 
---
 
## 11. Models
 
`<App>Library/<Domain>/<Feature>/Models/XxxModel.cs`. Plain POCO, file-scoped
namespace, property names matching table columns exactly (Dapper maps by name),
ordered to mirror the table. Transactional tables carry `int Id`, `bool Status`, and
audit columns (`CreatedBy/At/FromPlatform`, `LastModifiedBy/At/FromPlatform`,
`TransactionNo`, `TransactionDateTime`). Add an `XxxOverviewModel` (matching the
`*_Overview` view) and, for cart pages, an `XxxCartModel`.

- **Nullability:** annotate optional columns with `?` (`string?`, `decimal?`,
  `DateTime?`) **even though `<Nullable>` is disabled** — that is the house style
  (`LedgerModel`, `FinancialYearModel`). Match the column; don't add or strip `?` against it.
- **Dates:** a pure date/period column is `DateOnly` (`TimeOnly` for time), e.g.
  `FinancialYearModel.StartDate`. But a date **edited via the `CustomDatePicker`
  wrapper** must be `DateTime`/`DateTime?` — the component binds `DateTime` — so
  form-entered dates (transaction / voucher dates) are `DateTime`, not `DateOnly`.
- **Line/detail models:** name the parent FK property (and column) `MasterId`, not
  `<Header>Id`, so the generic `Load_TableData_By_MasterId` reader resolves it.
 
---
 
## 12. Exports
 
`<App>Library/<Domain>/<Feature>/Exports/XxxExport.cs` (master/report) and
`XxxInvoiceExport.cs` (single record), delegating to the shared export utils.
 
### 12.1 Master/report export shape
 
```csharp
public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
	IEnumerable<XxxModel> data, ReportExportType exportType)
{
	// 1. load reference tables for name resolution
	// 2. var enrichedData = data.Select(x => new { x.Id, x.Name, Related = refs.FirstOrDefault(...)?.Name ?? "N/A", …,
	//                                               Status = x.Status ? "Active" : "Deleted" });
	// 3. var columnSettings = new Dictionary<string, ReportColumnSetting> { [nameof(XxxModel.Id)] = new() { DisplayName="ID", Alignment=CellAlignment.Center, IncludeInTotal=false }, … };
	// 4. List<string> columnOrder = [ nameof(...), "Related", … ];
	// 5. var fileName = $"Xxx_Master_{await CommonData.LoadCurrentDateTime():yyyyMMdd_HHmmss}";
	// 6. PDF   -> PDFReportExportUtil.ExportToPdf(enrichedData, "XXX MASTER", …, columnSettings, columnOrder, useLandscape:<fit>) -> fileName+".pdf"
	//    Excel -> ExcelReportExportUtil.ExportToExcel(enrichedData, "XXX", "Xxx Data", …, columnSettings, columnOrder)            -> fileName+".xlsx"
}
```
 
### 12.2 Rules
 
- Always return `(MemoryStream stream, string fileName)`; the page hands it to the
  save-and-view service.
- File name = `<Entity>_<Master|Report|Invoice>_<yyyyMMdd_HHmmss>` + extension.
- Use `nameof(Model.Prop)` for keys/order; string literals only for enriched/computed
  columns. Pick `useLandscape` by column count. Report exports also take the active
  filters + `_showAllColumns`/`_showDeleted` and add a heading/period subtitle.
---
 
## 13. SQL
 
SSDT project, MSBuild-only. Per-feature folders `Table/`, `Insert/`, `Load/`,
`Delete/`. **Every new `.sql` must be added to the `<Build>` ItemGroup in the
`.sqlproj`**, and a matching name constant added to `DatabaseNames`.
 
### 13.1 Table
 
```sql
CREATE TABLE [dbo].[Xxx]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[Name] VARCHAR(250) NOT NULL,
	-- … columns matching XxxModel …
	[Status] BIT NOT NULL DEFAULT 1,
	CONSTRAINT [FK_Xxx_ToYyy] FOREIGN KEY ([YyyId]) REFERENCES [Yyy](Id)
)
```
 
Soft-deletable tables carry `[Status] BIT NOT NULL DEFAULT 1`. Unique business keys
get `UNIQUE`. FKs named `FK_<Table>_To<Ref>`. Transaction tables add audit columns +
`TransactionNo`/`TransactionDateTime`.
 
### 13.2 Insert (upsert) proc
 
`Insert_Xxx` takes `@Id INT OUTPUT` + one param per column. `IF @Id = 0` → `INSERT` +
`SET @Id = SCOPE_IDENTITY()`, `ELSE` → `UPDATE … WHERE [Id] = @Id`. End with
`SELECT @Id AS Id;`. This one proc backs Save, Delete (`Status=0`), and Recover
(`Status=1`).
 
### 13.3 Read procs
 
Don't write per-table read procs — the generic `Load_TableData*` procs cover standard
reads. They build dynamic SQL **safely** with `QUOTENAME(@TableName)` + parameterized
`sp_executesql`; follow that exact pattern if extending them. Write a bespoke
`<Feature>_Overview` / `Load_<Thing>_By_<Criteria>` proc only for joins/aggregations.
 
### 13.4 Delete proc (hard-delete tables only)
 
`Delete_Xxx @Id INT` → `DELETE FROM [dbo].[Xxx] WHERE [Id] = @Id; SELECT @@ROWCOUNT;`
— only for the join/period tables in [§10.4](#104-exception-hard-delete-tables).
 
### 13.5 Style
 
Tabs for proc bodies; bracket all identifiers (`[dbo].[Xxx]`). The table designer may
emit spaces in `Table/*.sql` — acceptable since the SSDT designer owns those; keep
`Insert_*`/`Load_*` procs consistently tab-indented.
 
---
 
## 14. End-to-end checklist
 
Adding a soft-deletable master "Widget", in order:
 
- [ ] **Table** `…/Widget/Table/Widget.sql` (Id, columns, `Status BIT DEFAULT 1`, FKs).
- [ ] **Insert proc** `…/Insert/Insert_Widget.sql` (upsert, `@Id OUTPUT`, `SELECT @Id`).
- [ ] **Register** both `.sql` in the `.sqlproj` `<Build>`.
- [ ] **Constants** in `DatabaseNames`: `Widget` (table) + `InsertWidget` (proc).
- [ ] **Model** `…/Widget/Models/WidgetModel.cs` (props = columns).
- [ ] **Data** `…/Widget/Data/WidgetData.cs` (copy the canonical master Data class).
- [ ] **Export** `…/Widget/Exports/WidgetExport.cs` (`ExportMaster`).
- [ ] **Route** in `PageRouteNames`.
- [ ] **Page** `<App>.Shared/Pages/<Domain>/WidgetPage.razor` + `.razor.cs` (copy the
      canonical master page; set role, `MudMenu` items + matching `MudHotkey` bindings,
      grid columns + context menu, form inputs, the single `ConfirmationDialog` +
      `ToastNotification` + `CloseGuard`).
- [ ] **Launchpad tile** in the right tab + section.
- [ ] **Build** library, shared, web host, and SQL project; fix all errors.
> **Reports** add: `*_Overview` view/proc, `WidgetOverviewModel`, `ExportReport`, an
> `IAsyncDisposable` report page with filters + aggregates + auto-refresh, a `Reports`
> role, and a Reports-tab tile on the launchpad.
> **Cart transactions** add: line table + `Insert_WidgetLine`, `WidgetLineModel` +
> `WidgetCartModel`, `ConvertCartToLines`, `SaveTransaction(header, lines)`,
> `WidgetInvoiceExport`, draft storage keys, and the cart page shape from
> [§8](#8-page-type-transaction--cart-based).