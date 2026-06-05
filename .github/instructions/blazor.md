---
name: blazor
description: Blazor component and application patterns. Use this skill when building or modifying Razor components (.razor), code-behind files (.razor.cs), or component styles (.razor.css). Applies to Blazor Server, Blazor WebAssembly, Blazor Hybrid (MAUI), component lifecycle management, state management, Syncfusion UI integration, MudBlazor UI integration, form validation, API integration, Azure services, and performance optimization in Blazor apps.
---
 
# Blazor Development Guidelines
 
## Official Documentation References
 
**ALWAYS** fetch and use the latest official documentation — never rely on memory for API signatures:
 
- **ASP.NET Core & Blazor**: https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-10.0
- **Blazor Documentation**: https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-10.0
- **.NET 10 Documentation**: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview
- **Azure Documentation**: https://learn.microsoft.com/en-us/azure/?product=popular
- **Azure SDK for .NET**: https://learn.microsoft.com/en-us/dotnet/azure/
- **Azure MCP Server**: https://github.com/Azure/azure-mcp — use when integrating Azure services via MCP
> **Rule**: Before writing any Azure SDK call, service client instantiation, or API method, fetch the relevant docs page or use Context7 MCP to get the latest API shape. Never guess parameter names.
 
## Syncfusion Blazor Components
 
When the project uses Syncfusion:
 
- **Primary Reference**: https://blazor.syncfusion.com/documentation/introduction
- **API Reference**: https://help.syncfusion.com/cr/blazor/Syncfusion.Blazor.html
- **What's New / Changelog**: https://blazor.syncfusion.com/documentation/release-notes/
- Always use the **latest Syncfusion NuGet version** — check NuGet before pinning
- Use Syncfusion components instead of native HTML elements for grids, calendars, dropdowns, inputs, buttons, notifications, and popups
- Always include proper Syncfusion namespaces: `@using Syncfusion.Blazor.*`
- Reference Syncfusion themes from `Syncfusion.Blazor.Themes` package
- Follow Syncfusion's data binding patterns and event handling conventions
- Leverage Syncfusion's built-in accessibility and responsive design features
- Use Syncfusion's virtualization for large datasets (e.g., `SfGrid` with `EnableVirtualization`)
- Register in `Program.cs`: `builder.Services.AddSyncfusionBlazor();`
## MudBlazor Components (v9.x — .NET 8/9/10)
 
**Current stable: MudBlazor 9.4.0** — supports .NET 8, .NET 9, .NET 10.
 
- **Primary Reference**: https://mudblazor.com/docs/overview
- **Component Gallery**: https://mudblazor.com/components/
- **API Docs**: https://mudblazor.com/api/
- **Migration Guide**: https://mudblazor.com/mud/migration
- **GitHub**: https://github.com/MudBlazor/MudBlazor
### Setup (MudBlazor)
 
```csharp
// Program.cs
builder.Services.AddMudServices();
```
 
```html
<!-- In <head> (App.razor or _Host.cshtml) -->
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
 
<!-- Before </body> -->
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```
 
```razor
<!-- _Imports.razor -->
@using MudBlazor
```
 
```html
<!-- Wrap in App.razor layout -->
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```
 
### MudBlazor Usage Rules
 
- **Always** wrap page content in `<MudLayout>` → `<MudMainContent>` for consistent scaffolding
- Use `<MudAppBar>`, `<MudDrawer>`, `<MudNavMenu>` for navigation
- Use `<MudDataGrid T="TItem">` (not the old `MudTable`) for tabular data with sorting/filtering/pagination
- Use `<MudTextField>`, `<MudSelect>`, `<MudDatePicker>`, `<MudTimePicker>` for forms
- Use `<MudSnackbar>` / `ISnackbar` service for toast notifications — inject `ISnackbar Snackbar`
- Use `<MudDialog>` / `IDialogService` for modal dialogs
- **Static rendering is NOT supported** — always use an interactive render mode (`InteractiveServer`, `InteractiveWebAssembly`, or `InteractiveAuto`)
- Prefer `Variant.Filled` or `Variant.Outlined` consistently across a feature
- Use `Color.Primary`, `Color.Secondary`, `Color.Error` from the `Color` enum — never hardcode hex values in component params
- Use `MudTheme` for global theming; define once in `App.razor` or a `ThemeProvider` component
- For icons, use `Icons.Material.Filled.*` / `Icons.Material.Outlined.*` — do not use raw strings
### MudBlazor Code Example (DataGrid)
 
```razor
<MudDataGrid T="Vehicle" Items="@vehicles" SortMode="SortMode.Multiple"
             Filterable="true" QuickFilter="@QuickFilter">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Fleet</MudText>
        <MudSpacer />
        <MudTextField @bind-Value="_searchString" Placeholder="Search"
                      Adornment="Adornment.Start" AdornmentIcon="@Icons.Material.Filled.Search"
                      IconSize="Size.Medium" Class="mt-0" />
    </ToolBarContent>
    <Columns>
        <PropertyColumn Property="x => x.RegistrationNumber" Title="Reg No" />
        <PropertyColumn Property="x => x.DriverName" Title="Driver" />
        <TemplateColumn Title="Actions">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit"
                               OnClick="@(() => Edit(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <MudDataGridPager T="Vehicle" />
    </PagerContent>
</MudDataGrid>
 
@code {
    private string _searchString = "";
    private List<Vehicle> vehicles = [];
 
    private Func<Vehicle, bool> QuickFilter => x =>
        string.IsNullOrWhiteSpace(_searchString) ||
        x.RegistrationNumber.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
}
```
 
## Blazor Code Style and Structure
 
- Write idiomatic and efficient Blazor and C# code following .NET conventions
- Use Razor Components appropriately for component-based UI development
- Prefer inline functions for smaller components; separate complex logic into code-behind or service classes
- Use async/await throughout to ensure non-blocking UI operations
- Use file-scoped namespaces and modern C# features
## CSS and Styling Hygiene
 
- Remove unused CSS selectors, style blocks, and duplicated styling from Razor pages/components
- Before adding new local styles, first check for existing reusable styles in the global `app.css`
- Prefer global CSS classes over creating page-local `<style>` blocks when an equivalent style already exists
- Keep only page-local styles that are truly page-specific and not reusable globally
- Do not keep dead/unused style definitions after UI refactors — clean them up in the same change
## Build and Error Resolution
 
**CRITICAL**: Always build the project and fix all errors after making changes:
 
- After creating or modifying any Blazor components or C# files, **ALWAYS** build the project
- Verify there are no compilation errors, warnings, or Razor component errors
- Never leave the project in a non-compiling state
- Common Blazor-specific errors to watch for:
  - Missing `@using` directives in components
  - Incorrect parameter bindings or missing `[Parameter]` attributes
  - Event handler signature mismatches
  - Syncfusion component property misconfigurations
  - Missing or incorrect render fragments
  - Cascading parameter type mismatches
- For Blazor-specific issues, check the browser console for JavaScript errors
- Run `dotnet clean` and rebuild if Razor compilation caching causes issues
## Naming Conventions
 
- Follow PascalCase for component names, method names, and public members
- Use camelCase for private fields and local variables
- Prefix interface names with "I" (e.g., IUserService)
## Blazor and .NET Specific Guidelines
 
- Utilize Blazor's built-in component lifecycle: OnInitializedAsync, OnParametersSetAsync, OnAfterRenderAsync
- Use data binding effectively with `@bind` and `@bind:after` for post-change callbacks
- Leverage Dependency Injection for services (register in MauiProgram.cs or Program.cs)
- Always use the latest C# version (C# 14) features:
  - Primary constructors for classes
  - Collection expressions `[..]`
  - Required members and init-only properties
  - Pattern matching and switch expressions
  - Global usings and implicit usings
  - Record types and record structs
  - File-scoped namespaces
  - Raw string literals for multi-line text
- Use InteractiveServer render mode for real-time updates in Blazor Web
- Implement proper component disposal (IDisposable/IAsyncDisposable) for event handlers and subscriptions
- Use `@key` directive to preserve element and component identity during re-renders
- Leverage `@rendermode` attribute to specify different render modes per component
## Code Freshness Rules
 
> **CRITICAL**: Always write code that matches the **current library API** — do not infer method signatures from memory.
> - For any MudBlazor component property → fetch https://mudblazor.com/api/ or the component's doc page first
> - For any Syncfusion component → fetch https://blazor.syncfusion.com/documentation/ for that component
> - For any Azure SDK class → use Context7 MCP (`resolve-library-id` then `query-docs`) before writing the call
> - For .NET BCL / ASP.NET Core APIs → fetch https://learn.microsoft.com/en-us/dotnet/api/
> - Write the **simplest correct code** — avoid workarounds for older versions
 
## Error Handling and Validation
 
- Use `<ErrorBoundary>` component to catch and display component errors gracefully
- Use `ILogger<T>` for server-side logging
- Implement validation using `DataAnnotations` or `FluentValidation`
- For MudBlazor forms: use `<MudForm @ref="_form">` + `await _form.Validate()` before submit, or `<EditForm>` + `<DataAnnotationsValidator>`
- For Syncfusion forms: use `<EditForm>` with `<DataAnnotationsValidator>`
- Display validation errors with MudBlazor `ISnackbar` service (`Snackbar.Add(...)`) or Syncfusion Toast
- Handle async exceptions in lifecycle methods with try-catch; surface errors via snackbar/toast
## Blazor API and Performance Optimization
 
- Use asynchronous methods (async/await) for API calls, database operations, and UI actions
- Optimize Razor components by reducing unnecessary renders using `ShouldRender()` and `StateHasChanged()` efficiently
- Minimize the component render tree by avoiding re-renders unless necessary
- Use EventCallbacks (`EventCallback<T>`) for handling user interactions efficiently
- Implement virtualization for large lists using Syncfusion's EnableVirtualization or Blazor's Virtualize component
- Use streaming rendering with `@attribute [StreamRendering]` for improved perceived performance
- Implement debouncing for input events to reduce excessive processing
- Minimize JavaScript interop calls; batch when necessary
- Use Blazor's built-in cascading parameters for dependency injection in component trees
## Caching Strategies
 
- Implement in-memory caching for frequently used data using IMemoryCache
- For Blazor WebAssembly or Hybrid, utilize localStorage or sessionStorage to cache application state
- Consider Distributed Cache strategies (Redis or SQL Server Cache) for shared state across multiple users
- Cache API calls by storing responses to avoid redundant calls when data is unlikely to change
## State Management
 
- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing
- Implement the AppState or StateContainer pattern for cross-component state management
- For advanced scenarios, use libraries like Fluxor or BlazorState
- For client-side state persistence, consider Blazored.LocalStorage or Blazored.SessionStorage
- For server-side Blazor, use Scoped Services and the StateContainer pattern within user sessions
- In .NET MAUI Blazor Hybrid apps, leverage platform-specific storage (Preferences, SecureStorage, FileSystem)
## API Design and Integration
 
- Use `IHttpClientFactory` for creating `HttpClient` instances (registered in DI)
- Implement error handling for API calls using try-catch and provide proper user feedback
- Use MudBlazor `ISnackbar` or Syncfusion Toast/Dialog for user notifications and error messages
- Implement retry logic with **Polly v8** (`AddResilienceHandler` on `IHttpClientBuilder`) — do NOT use old `AddPolicyHandler`
- Use typed `HttpClient` services for better maintainability
- Implement proper authentication with JWT tokens or **Microsoft.Identity.Web** (Azure AD / Entra ID)
## Azure Service Integration
 
**ALWAYS fetch the latest Azure SDK docs before writing service client code.** Use Context7 MCP (`resolve-library-id` → `query-docs`) or fetch from https://learn.microsoft.com/en-us/dotnet/azure/ for current API shapes.
 
### Key packages and docs (verify versions on NuGet before use)
 
- **Azure Blob Storage** — `Azure.Storage.Blobs` — https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction
- **Azure Service Bus** — `Azure.Messaging.ServiceBus` — https://learn.microsoft.com/en-us/azure/service-bus-messaging/
- **Azure Key Vault** — `Azure.Security.KeyVault.Secrets` — https://learn.microsoft.com/en-us/azure/key-vault/
- **Azure Cosmos DB** — `Microsoft.Azure.Cosmos` — https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/sdk-dotnet-v3
- **Azure SQL** — `Microsoft.Data.SqlClient` + Dapper — https://learn.microsoft.com/en-us/azure/azure-sql/
- **Azure SignalR** — `Microsoft.Azure.SignalR` — https://learn.microsoft.com/en-us/azure/azure-signalr/
- **Azure Entra ID Auth** — `Microsoft.Identity.Web` — https://learn.microsoft.com/en-us/azure/active-directory/develop/
- **Azure MCP Server** — https://github.com/Azure/azure-mcp — use for MCP-based Azure resource interactions
### Azure SDK Rules
 
- Use `DefaultAzureCredential` from `Azure.Identity` — never hardcode connection keys
- Register Azure clients in DI via `builder.Services.AddAzureClients(...)` from `Microsoft.Extensions.Azure`
- All Azure SDK methods are async-first — always use `async`/`await`
- Catch `RequestFailedException` for Azure Core-specific errors
- Store connection strings in environment variables / Azure App Configuration / Key Vault — never in source code
```csharp
// Program.cs — correct Azure SDK DI registration
builder.Services.AddAzureClients(clients =>
{
    clients.AddBlobServiceClient(builder.Configuration["Azure:StorageConnection"]);
    clients.UseCredential(new DefaultAzureCredential());
});
```
 
## Project-Specific Guidelines
 
- **Shared Components**: Reusable components are in the `.Shared` project
- **Web Application**: Blazor Server/Web app is in the `.Web` project
- **MAUI Hybrid**: Cross-platform mobile/desktop app is in the MAUI project
- **Business Logic**: Shared business logic and data access is in the shared project
- **Component Reusability**: Always check `Shared/Components` before creating new UI components
- **Service Layer**: Data services are centralized in `Data` and `DataAccess` folders
- **Dependency Injection**: Register services in `MauiProgram.cs` (MAUI) or `Program.cs` (Web)
- **Routing**: Use `Routes.razor` in Shared project for consistent routing across platforms
- **Global Usings**: Leverage defined Using statements in project files for cleaner code
## Accessibility and Responsiveness
 
- Ensure all Syncfusion components have proper accessibility attributes
- Use Syncfusion's built-in responsive features for adaptive layouts
- Implement keyboard navigation and screen reader support
- Test components with different viewport sizes for mobile and desktop
- Use Syncfusion's adaptive UI patterns for consistent cross-platform experience
 