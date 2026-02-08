---
description: 'Blazor component and application patterns'
applyTo: '**/*.razor, **/*.razor.cs, **/*.razor.css'
---

# Blazor Development Guidelines

## Official Documentation References

**ALWAYS** reference the latest official documentation when working with Blazor:

- **ASP.NET Core & Blazor**: https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-10.0
- **Blazor Documentation**: https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor
- **.NET Fundamentals**: https://learn.microsoft.com/en-us/dotnet/fundamentals/
- **.NET Documentation**: https://learn.microsoft.com/en-us/dotnet/
- **Azure Documentation**: https://learn.microsoft.com/en-us/azure/?product=popular

## Syncfusion Blazor Components

This application **ALWAYS** uses Syncfusion components. When implementing UI features:

- **Primary Reference**: https://blazor.syncfusion.com/documentation/introduction
- **API Reference**: https://help.syncfusion.com/cr/blazor/Syncfusion.Blazor.html
- Ensure Syncfusion version matches project dependencies (currently using Syncfusion.Blazor v32.1.25 with .NET 10.0)
- Use Syncfusion components instead of native HTML elements for grids, calendars, dropdowns, inputs, buttons, notifications, and popups
- Always include proper Syncfusion namespaces: `@using Syncfusion.Blazor.*`
- Reference Syncfusion themes from `Syncfusion.Blazor.Themes` package
- Follow Syncfusion's data binding patterns and event handling conventions
- Leverage Syncfusion's built-in accessibility and responsive design features
- Use Syncfusion's virtualization for large datasets (e.g., SfGrid with EnableVirtualization)
- Implement Syncfusion's global settings and localization when needed

## Blazor Code Style and Structure

- Write idiomatic and efficient Blazor and C# code following .NET 10 conventions
- Follow .NET and Blazor best practices from official Microsoft documentation
- Use Razor Components appropriately for component-based UI development
- Prefer inline functions for smaller components but separate complex logic into code-behind or service classes
- Async/await should be used where applicable to ensure non-blocking UI operations
- Use file-scoped namespaces and modern C# 14 features

## Build and Error Resolution

**CRITICAL**: Always build the project and fix all errors after making changes:

- After creating or modifying any Blazor components or C# files, **ALWAYS** build the project
- Use `dotnet build` for the specific project or `dotnet build PrimeBakes.slnx` for the entire solution
- Verify there are no compilation errors, warnings, or Razor component errors
- Check the Problems panel in the editor for all diagnostics
- Never leave the project in a non-compiling state
- Common Blazor-specific errors to watch for:
  - Missing `@using` directives in components
  - Incorrect parameter bindings or missing `[Parameter]` attributes
  - Event handler signature mismatches
  - Syncfusion component property misconfigurations
  - Missing or incorrect render fragments
  - Cascading parameter type mismatches
- After fixing errors, test the component in the running application
- For Blazor-specific issues, check the browser console for JavaScript errors
- Run `dotnet clean` and rebuild if Razor compilation caching causes issues

## Naming Conventions

- Follow PascalCase for component names, method names, and public members
- Use camelCase for private fields and local variables
- Prefix interface names with "I" (e.g., IUserService)

## Blazor and .NET Specific Guidelines

- Utilize Blazor's built-in features for component lifecycle (e.g., OnInitializedAsync, OnParametersSetAsync, OnAfterRenderAsync)
- Use data binding effectively with `@bind` and `@bind:after` for post-change callbacks
- Leverage Dependency Injection for services in Blazor (register in MauiProgram.cs or Program.cs)
- Structure Blazor components and services following Separation of Concerns
- Always use the latest C# version (C# 14) features:
  - Primary constructors for classes
  - Collection expressions `[..]`
  - Required members and init-only properties
  - Pattern matching and switch expressions
  - Global usings and implicit usings
  - Record types and record structs
  - File-scoped namespaces
  - Raw string literals for multi-line text
  - Generic attributes
- Use InteractiveServer render mode for real-time updates in Blazor Web
- Implement proper component disposal (IDisposable/IAsyncDisposable) for event handlers and subscriptions
- Use `@key` directive to preserve element and component identity during re-renders
- Leverage `@rendermode` attribute to specify different render modes per component

## Error Handling and Validation

- Implement proper error handling for Blazor pages and API calls
- Use ErrorBoundary component to catch and display component errors gracefully
- Use logging for error tracking in the backend with ILogger<T>
- Capture UI-level errors and log them appropriately
- Implement validation using FluentValidation or DataAnnotations in forms
- Use Syncfusion's EditForm with DataAnnotationsValidator for form validation
- Display validation errors clearly using Syncfusion Toast or ValidationSummary
- Handle async exceptions in lifecycle methods properly

## Blazor API and Performance Optimization

- Utilize Blazor Hybrid (MAUI), Blazor Server, or Blazor WebAssembly optimally based on project requirements
- Use asynchronous methods (async/await) for API calls, database operations, and UI actions
- Optimize Razor components by reducing unnecessary renders using `ShouldRender()` and `StateHasChanged()` efficiently
- Minimize the component render tree by avoiding re-renders unless necessary
- Use EventCallbacks (`EventCallback<T>`) for handling user interactions efficiently
- Implement virtualization for large lists using Syncfusion's EnableVirtualization or Blazor's Virtualize component
- Use streaming rendering with `@attribute [StreamRendering]` for improved perceived performance
- Leverage enhanced navigation and form handling in Blazor Web Apps
- Implement debouncing for input events to reduce excessive processing
- Consider using prerendering for Blazor Web Apps to improve initial load time
- Use `@attribute [RenderModeInteractiveServer]` or `@attribute [RenderModeInteractiveAuto]` strategically
- Minimize JavaScript interop calls; batch when necessary
- Use Blazor's built-in cascading parameters for dependency injection in component trees

## Caching Strategies

- Implement in-memory caching for frequently used data, especially for Blazor Server apps using IMemoryCache
- For Blazor WebAssembly or Blazor Hybrid, utilize localStorage or sessionStorage to cache application state between user sessions
- Consider Distributed Cache strategies (like Redis or SQL Server Cache) for larger applications that need shared state across multiple users
- Cache API calls by storing responses to avoid redundant calls when data is unlikely to change

## State Management Libraries

- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing across components
- Implement the AppState or StateContainer pattern for cross-component state management
- Implement advanced state management solutions using libraries like Fluxor or BlazorState when the application grows in complexity
- For client-side state persistence in Blazor WebAssembly or Blazor Hybrid, consider using Blazored.LocalStorage or Blazored.SessionStorage
- For server-side Blazor, use Scoped Services and the StateContainer pattern to manage state within user sessions
- In .NET MAUI Blazor Hybrid apps, leverage platform-specific storage (Preferences, SecureStorage, FileSystem)
- Consider using CascadingValue with CascadingParameter for app-wide state
- Implement proper state synchronization when using multiple render modes in the same app

## API Design and Integration

- Use IHttpClientFactory for creating HttpClient instances (registered in DI)
- Implement error handling for API calls using try-catch and provide proper user feedback in the UI
- Use Syncfusion Toast or Dialog components for user notifications and error messages
- Implement retry logic with Polly for transient failures
- Use typed HttpClient services for better maintainability
- Leverage minimal APIs in ASP.NET Core 10 for simple endpoints
- Implement proper authentication with JWT tokens or Azure AD B2C
- Use Azure services integration when deploying to Azure (App Service, Functions, Storage, etc.)

## Project-Specific Guidelines

- **Shared Components**: Reusable components are in `PrimeBakes.Shared` project
- **Web Application**: Blazor Server/Web app is in `PrimeBakes.Web` project  
- **MAUI Hybrid**: Cross-platform mobile/desktop app is in `PrimeBakes` project
- **Business Logic**: Shared business logic and data access is in `PrimeBakesLibrary` project
- **Component Reusability**: Always check `PrimeBakes.Shared/Components` before creating new UI components
- **Service Layer**: Data services are centralized in `PrimeBakesLibrary/Data` and `PrimeBakesLibrary/DataAccess`
- **Dependency Injection**: Register services in `MauiProgram.cs` (MAUI) or `Program.cs` (Web)
- **Routing**: Use `Routes.razor` in Shared project for consistent routing across platforms
- **Global Usings**: Leverage defined Using statements in project files for cleaner code

## Accessibility and Responsiveness

- Ensure all Syncfusion components have proper accessibility attributes
- Use Syncfusion's built-in responsive features for adaptive layouts
- Implement keyboard navigation and screen reader support
- Test components with different viewport sizes for mobile and desktop
- Use Syncfusion's adaptive UI patterns for consistent cross-platform experience
