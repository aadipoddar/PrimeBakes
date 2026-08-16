# PrimeBakes → API-backed architecture

Plan to move PrimeBakes to the structure Strada already runs (`C:\Dev\Strada`). Nothing here is built yet — this is the agreed shape and order of work.

Reference: Strada's own migration checklist is recoverable with `git show 4fd9c4d^:TODO.md` from `C:\Dev\Strada`.

## Why

Today `PrimeBakes.Shared` references `PrimeBakes.Library`, so the Blazor UI links Dapper, `Microsoft.Data.SqlClient`, MailKit, the Azure Blob SDK, SkiaSharp, QRCoder and Syncfusion's PDF + XlsIO engines — and `Secrets.cs`, holding the SQL connection string, blob keys and the email password, is compiled into every host. That rules out a WebAssembly host, forces the mobile app to hold direct SQL credentials, and means the DB must be reachable from every client.

Putting an API in the middle fixes all three. The connection string never leaves the server, and that whole dependency stack stops shipping to the client — which is the only reason a WASM host is viable at all.

**A WASM host is a confirmed goal** (decided 2026-08-16), matching Strada. See [WASM performance](#wasm-performance) — it is not fast by default, and the decisions that make it fast belong in Stages 1–3, not bolted on afterwards.

## Target

```
PrimeBakes.Models          models, request records, DatabaseNames, PageRouteNames, CommonSecrets
   ↑                              ↑
PrimeBakes.Library            PrimeBakes.Api.Client
(Dapper → SQL, server only)   (HTTP → API)
   ↑                              ↑
PrimeBakes.Api                PrimeBakes.Shared      ← references Client + Models ONLY
(Carter + Scalar)                  ↑
                    PrimeBakes.Web │ PrimeBakes (MAUI) │ PrimeBakes.Wasm (optional)
```

Strada also groups projects into `Api/`, `App/`, `Data/`, `Tools/` folders on disk. **Deferred here to a final, optional stage** — moving `PrimeBakes.Library/` to `Data/PrimeBakes.Library/` rewrites every workflow path, publish profile and relative `ProjectReference` for no functional gain. Keeping paths stable means CI keeps working through Stages 1–6. New projects are added at the repo root alongside the existing ones.

## The trick that makes this affordable

`PrimeBakes.Api.Client` sets `<RootNamespace>PrimeBakes.Library</RootNamespace>` and mirrors the library file-for-file — same class names, same method names, same parameters **minus** the trailing `SqlDataAccessTransaction`. Call sites compile unchanged; `PrimeBakes.Shared` swaps backends by changing one `ProjectReference`.

Verified this works here: **0** occurrences of `SqlDataAccessTransaction` anywhere in `PrimeBakes/` (the UI and hosts). The only `SqlDataAccess.` reference outside the library is `SetupConfiguration()` in the two host startups. So dropping the transaction parameter costs nothing at the UI layer, and all 964 `XData.*` + 170 `XExport.*` call sites in `PrimeBakes.Shared` stay untouched.

`PrimeBakes.Shared` must reference **exactly one** of Library / Api.Client. Referencing both makes every shared type ambiguous (`CS0433`).

## Measured scale

| | Count |
|---|---|
| `.cs` in `PrimeBakes.Library` | 195 |
| Models / Data / Exports files | 52 / 33 / 78 |
| `public static` methods on Data classes | 175 |
| `public static` methods on Export classes | 150 (70 returning `MemoryStream`) |
| `SaveTransaction` overloads needing request records | 33 |
| `XData.*` call sites in Shared | 964 |
| `XExport.*` call sites in Shared | 170 |
| `Secrets.*` uses in Shared | 3 — `DatabaseName`, `SuperAdminIds`, `AadiSoftWebsite` |

That last row is the good news: everything the UI reads from `Secrets` is client-safe, so the split is clean — no UI code depends on a server-only secret.

## Stages

Each stage ends at a compiling, runnable checkpoint. Stages 1 and 2 are independently shippable.

### Stage 1 — Extract `PrimeBakes.Models`

Target namespaces, copied from Strada:

| | Now | After |
|---|---|---|
| Feature model | `PrimeBakes.Library.Inventory.Kitchen.Models` | `PrimeBakes.Models.Inventory.Kitchen` |
| Feature data | `PrimeBakes.Library.Inventory.Kitchen.Data` | unchanged |
| Flat-feature model | `PrimeBakes.Library.Operations.User` | `PrimeBakes.Models.Operations` |
| Flat-feature data | `PrimeBakes.Library.Operations.User` | `PrimeBakes.Library.Operations.Data` |

Two shape changes fall out of that:

- **models lose the `Models/` folder** — the project name already says it, so `PrimeBakes.Models/Inventory/Kitchen/KitchenIssueModel.cs`
- **the five flat features collapse to module level.** `Operations/User`, `Operations/Settings`, `Operations/Location`, `Operations/AuditTrail` and `Store/PaymentMode` currently keep Data + Model + Export in one folder under one namespace; Strada folded the equivalent set into a single module-level `Data/` and `Exports/`. Do the same.

This is why the original Stage 0 is gone: normalizing those folders first and *then* extracting models would rewrite the same ~500 `using` lines twice. One pass instead.

Also in this stage:

- `Common/DatabaseNames.cs`, `PageRouteNames.cs`, `StorageFileNames.cs`, `Helper.cs`, `YesNoFilterOptions.cs`, `DecodeTransactionNoModel.cs` move to Models
- add `SanitizeClassName` / `MakeRouteFromEndpointFunction` to `Helper.cs`
- new `Secrets/CommonSecrets.cs` — `DatabaseName`, `SuperAdminIds`, `AadiSoftWebsite`, `AppWebsite`, `OnlineFullLogoPath`, `SyncfusionLicense`, plus `ApiBaseUrl` driven by a `ConnectionType { Local, Azure, AzureTesting }` enum
- `Secrets.cs` keeps the server-only half: connection strings, blob keys, `EmailPassword`
- `.gitignore` already covers `*.local_secrets.cs` by wildcard, so the new `CommonSecrets.local_secrets.cs` is ignored without a change

`PrimeBakes.Library` references `PrimeBakes.Models`. Everything still runs on direct SQL.

**Checkpoint:** app works exactly as today. Safe to ship alone.

### Stage 2 — Build `PrimeBakes.Api`

ASP.NET Core minimal API, Carter modules + Scalar + CORS + `GlobalExceptionHandler` + `ConfigureHttpJsonOptions(IncludeFields = true)`.

- `Endpoint/<Module>/<Feature>/{Data,Exports}/X<Endpoint>.cs`, mirroring the library tree
- routes derived, never hand-written: `SanitizeClassName(nameof(XEndpoint))` + `/MethodName`
- 33 request records for multi-argument saves (`KitchenIssueSaveRequest(model, details, recover)` etc.) — these live in `PrimeBakes.Models`
- 70 stream-returning exports become file endpoints; `Content-Disposition` must be in `WithExposedHeaders`, or downloads arrive named `download`
- `GlobalExceptionHandler` returns `{ message }` so the `InvalidOperationException` text from `ValidateTransaction` still reaches the user

**Checkpoint:** API runs, Scalar lists every endpoint, exercised manually. App still on direct SQL.

### Stage 3 — Build `PrimeBakes.Api.Client`

- `<RootNamespace>PrimeBakes.Library</RootNamespace>`, references Models only
- `ApiClient.cs`: `Get` / `Post` / `GetForFile` / `PostForFile` / `Upload`, `DateOnly`/`TimeOnly` JSON converters, `Init(HttpClient)` that throws a named exception if unset
- mirror all 33 Data + 78 Export classes

Pure `cart.Select(...)` converters (e.g. `KitchenIssueData.ConvertCartToDetails`) get duplicated in both projects for now — they must exist on both sides for the mirror to hold. Moving them to `PrimeBakes.Models` is cleanup for after the swap.

**Checkpoint:** Client compiles against Models alone.

### Stage 4 — Repoint the app

- `PrimeBakes.Shared`: swap `ProjectReference` from Library to Api.Client
- `ApiClient.Init(new HttpClient { BaseAddress = new Uri(CommonSecrets.ApiBaseUrl) })` in `Program.cs` and `MauiProgram.cs`; `SqlDataAccess.SetupConfiguration()` is replaced by Syncfusion licence registration only
- verify the Shared output no longer carries Dapper, SqlClient, MailKit or the Azure SDK

**Checkpoint:** Web + MAUI run entirely through the API.

### Stage 5 — `PrimeBakes.Wasm`

New `Microsoft.NET.Sdk.BlazorWebAssembly` host referencing `PrimeBakes.Shared`, with WASM implementations of the seven `I*` services (`FormFactor`, `UpdateService`, `VibrationService`, `NotificationService`, `SaveAndViewService`, `SoundService`, `DataStorageService`). `IBluetoothPrinterService` / `IDirectPrintService` get null implementations as on the web host.

Config carried over from Strada's `Strada.Wasm.csproj`, all of it load-time critical — see below.

### Stage 6 — CI/CD

- new `api-deploy.yml` and `wasm-deploy.yml` (Azure Static Web Apps; needs the `wasm-tools` workload installed in the job)
- **`version-checks.yml` breaks and must be rewritten.** It currently greps `SqlDataAccess.cs` for `_databaseConnection = Secrets.AzureConnectionString` as its deploy safety valve. After Stage 4 the app doesn't reference that file at all. Replace with a check on `CommonSecrets.DatabaseConnection == ConnectionType.Azure`.
- secret injection now writes **two** `*.local_secrets.cs` files (server `Secrets` + client `CommonSecrets`). A workflow writing only one produces a build that doesn't compile.
- the README / `AssemblyVersion` / `AndroidManifest` version-match gating is unaffected

## WASM performance

Blazor WASM ships the .NET runtime plus every referenced assembly to the browser. Getting it fast is mostly about what *doesn't* go in the payload, so these are Stage 1–3 decisions.

**What the migration buys for free.** Dapper, SqlClient, MailKit, Azure.Storage.Blobs, SkiaSharp, QRCoder, `Syncfusion.Pdf.Net.Core` and `Syncfusion.XlsIO.Net.Core` all stay behind the API. The 70 stream-returning export methods run server-side and come back over `GetForFile`/`PostForFile`. This is the single largest win and it is automatic once `Shared` is repointed.

**Copy from Strada's `Strada.Wasm.csproj`:**

- `<RunAOTCompilation>true</RunAOTCompilation>` under Release only — big speedup, slow builds, so keep it off in Debug
- service worker + `<ServiceWorkerAssetsManifest>` for offline caching of the payload
- the `TrimUnusedSyncfusionThemes` MSBuild target, which strips every Syncfusion theme CSS except the one actually used (`tailwind3.css`). This was Strada's `87fe9ea` "remove unused themes".

**PrimeBakes-specific, not present in Strada:**

- **`Syncfusion.Blazor.Diagram` is referenced for exactly one page** — `Pages/Restaurant/Bill/DiningDashbaord.razor`. It's a heavy package to put in the startup payload for a single screen. Either lazy-load the assembly for that route or rebuild that view without Diagram. Decide during Stage 5.
- **`InvariantGlobalization` must stay off.** `Helper.cs` formats currency with `new CultureInfo("hi-IN")` at 6 sites, so ICU data has to ship. (Strada has the identical constraint and also leaves it off.) Don't let a size-trimming pass turn this on — it would silently break every rupee figure.

**Caching — where Strada left value on the table.** `Strada.Wasm` registers `AddMemoryCache()` but nothing in `Strada.Shared` ever resolves `IMemoryCache`, so the client-side cache is unused. Worth actually doing here, on both sides:

- client: cache master/lookup reads (`CommonData.LoadTableData` for products, ledgers, locations, categories) for the session
- server: response caching on the same routes
- explicitly **not** `LoadCurrentDateTime` or `LoadLastTableData*` — the latter feeds `GenerateCodes`, and a stale read yields duplicate transaction numbers

## Decide before Stage 2

1. ~~Is a WASM host a goal?~~ **Yes** — confirmed, see above.
2. **API authentication.** PrimeBakes logs in by passcode and `AuthenticationService.ValidateUser` re-fetches the user on every page load. Exposed as an unauthenticated endpoint over `AllowAnyOrigin`, that hands out user records — including passcodes — to anyone who finds the URL. Strada shipped with this deferred and it's still open there. With a public WASM origin in the mix this gets worse, not better: the app is now a static site anyone can load. Recommend settling it as part of Stage 2 rather than inheriting the same debt.
3. **`ApiClient._http` is a static field.** Fine for WASM and MAUI (one user per process). Under Blazor Server it's shared across all circuits — so if auth lands as a token on that client, it leaks across users. Decide the shape before Stage 3, not after.

## Unaffected

Bluetooth/direct printing, notifications, sound, vibration, storage — all already behind `Shared/Services/I*.cs` per host. Mail and push currently fire inside library Data classes, so they simply move server-side with the API, which is where they belong.
