---
name: csharp
description: Guidelines for building C# applications. Use this skill when writing, reviewing, or refactoring C# code, working with .NET projects, ASP.NET Core APIs, data access with Dapper/SQL Server, Azure services, or setting up multi-project solutions. Also applies when handling async patterns, dependency injection, exception handling, performance optimization, or deployment pipelines for .NET apps.
---
 
# C# Development Guidelines
 
## Official Documentation References
 
**ALWAYS** reference the latest official documentation when working with C# and .NET:
 
- **.NET Documentation**: https://learn.microsoft.com/en-us/dotnet/
- **.NET Fundamentals**: https://learn.microsoft.com/en-us/dotnet/fundamentals/
- **ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-10.0
- **Blazor**: https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor
- **Azure**: https://learn.microsoft.com/en-us/azure/?product=popular
## Syncfusion Components Integration
 
This application **ALWAYS** uses Syncfusion Blazor components. When writing C# code:
 
- **Syncfusion Documentation**: https://blazor.syncfusion.com/documentation/introduction
- **Syncfusion API Reference**: https://help.syncfusion.com/cr/blazor/Syncfusion.Blazor.html
- Ensure compatibility with Syncfusion.Blazor latest version and .NET latest version
- Use strongly-typed Syncfusion component properties and events
- Implement data models that work efficiently with Syncfusion Grid's data binding
- Handle Syncfusion component events (OnChange, OnActionComplete, etc.) properly
- Leverage Syncfusion's server-side data operations (filtering, sorting, paging) for performance
## C# Version and Language Features
 
- Always use the latest C# version and features:
  - Primary constructors for classes and structs
  - Collection expressions: `[]`, `[.. items]`
  - Inline arrays and improved pattern matching
  - Required members and init-only properties
  - File-scoped namespaces
  - Global and implicit usings
  - Record types and record structs
  - Raw string literals for multi-line text
  - Generic attributes
  - Lambda improvements and natural delegate types
  - List patterns and enhanced pattern matching
  - Static abstract members in interfaces
- Write clear and concise comments for each public method and complex logic
- Use C# XML documentation comments (`///`) for all public APIs
## General Instructions
 
- Make only high-confidence suggestions when reviewing code changes
- Write code with good maintainability practices, including comments explaining design decisions
- Handle edge cases and write clear exception handling with specific exception types
- For libraries or external dependencies, mention their usage and purpose in comments
- Follow SOLID principles and design patterns where appropriate
- Implement proper logging using ILogger<T> dependency injection
- Use async/await consistently for I/O-bound operations
- Implement cancellation tokens for long-running async operations
- Write code that is testable and follows dependency injection patterns
## Build and Error Resolution
 
**CRITICAL**: Always build the project and fix all errors after making changes:
 
- After creating or modifying any C# files, **ALWAYS** build the project and solution to verify compilation
- Check for and resolve ALL compilation errors, warnings, and code analysis issues
- Never leave code in a non-compiling state
- If errors occur:
  - Read error messages carefully and understand the root cause
  - Fix errors systematically, starting with the first error
  - Re-build after each fix to verify resolution
  - Check for missing using statements, incorrect namespaces, or typos
  - Verify NuGet package references and versions are correct
- Run `dotnet restore` if package-related errors occur
- Clear bin/obj folders if strange build errors persist: `dotnet clean`
## Naming Conventions
 
- Follow PascalCase for component names, method names, and public members
- Use camelCase for private fields and local variables
- Prefix interface names with "I" (e.g., IUserService)
## Formatting
 
- Apply code-formatting style defined in `.editorconfig`
- Prefer file-scoped namespace declarations (`namespace MyApp;`)
- Use single-line using directives and global usings where appropriate
- Insert a newline before the opening curly brace of any code block
- Ensure the final return statement of a method is on its own line
- Use pattern matching and switch expressions wherever possible
- Use `nameof` instead of string literals when referring to member names
- Ensure XML doc comments (`///`) are created for all public APIs
- When applicable, include `<summary>`, `<param>`, `<returns>`, `<exception>`, `<example>`, and `<code>` tags
- Use expression-bodied members for simple properties and methods
- Group using directives: System namespaces first, then third-party, then project namespaces
- Order class members: constants, fields, constructors, properties, methods, events
- Use `var` for local variables when the type is obvious
## Project Setup and Structure
 
- Guide users through creating new .NET projects with appropriate templates
- Show proper separation of concerns with models, services, and data access layers
- Use minimal hosting model in .NET with top-level statements
- Implement proper project references and NuGet package management
- Structure multi-project solutions (e.g., Shared, Web, MAUI)
- Separate shared code into reusable class libraries
## Nullable Reference Types
 
- **IMPORTANT**: This project has Nullable disabled (`<Nullable>disable</Nullable>`)
- Still implement defensive null checks at entry points and public APIs
- Always use `is null` or `is not null` instead of `== null` or `!= null`
- Use null-forgiving operator `!` sparingly and only when absolutely certain
## Data Access Patterns
 
- This project uses:
  - SQL Server with Dapper (SqlDataAccess.cs)
  - Azure Blob Storage (BlobDataAccess.cs)
  - Firebase Admin SDK for push notifications
- Use parameterized queries to prevent SQL injection
- Use async data access methods consistently (ExecuteAsync, QueryAsync)
- Implement proper transaction handling when needed
- Implement data transfer objects (DTOs) to decouple data models from domain models
- Demonstrate repository pattern implementation where beneficial
## Validation and Error Handling
 
- Implement model validation using Data Annotations and FluentValidation
- Use global exception handling middleware in ASP.NET Core
- Implement Problem Details (RFC 9457) for standardized error responses
- Create custom exception types for domain-specific errors
- Handle specific exceptions before general ones
- Log exceptions with proper context using ILogger
- Provide user-friendly error messages in the UI using Syncfusion Toast or Dialog
## Performance Optimization
 
- Use IMemoryCache for in-memory caching
- Consider Azure Redis Cache for distributed caching scenarios
- Use Task.WhenAll for parallel async operations when appropriate
- Implement pagination, filtering, and sorting for large datasets
- Use ValueTask<T> for hot paths where allocations matter
- Use Span<T> and Memory<T> for efficient buffer management
- Leverage Syncfusion's virtualization features for large data grids
## Deployment and DevOps
 
- Containerize using .NET's built-in container support:
  `dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`
- Deploy to Azure App Service, Container Apps, or Static Web Apps as appropriate
- Use Azure Key Vault for secrets management
- Implement Application Insights for monitoring
- Configure Azure SQL Database and Azure Blob Storage connections
## Project-Specific Guidelines
 
- **Multi-Project Solution**: Solution has multiple projects (MAUI, Web, Shared, Library, API)
- **Shared Library**: Business logic is in the shared project — check here before adding new data access code
- **Database**: SQL Server database project with stored procedures and views
- **Code Reuse**: Maximize code sharing between Web and MAUI via the Shared project
- **Dependency Injection**: Register services appropriately for each project type
- **Configuration**: Use appsettings.json for Web, MauiProgram.cs for MAUI
- **Data Models**: Keep models in `Models` folder for consistency
- **Export Logic**: Implement export functionality in `Exports` folder
- **Styling Consistency**: Reuse global styles from `Shared/wwwroot/app.css` — do not duplicate
- **CSS Cleanup**: Remove unused CSS introduced by UI changes in the same update
## Security Best Practices
 
- Use HTTPS everywhere and enforce it in production
- Implement proper CORS policies for API access
- Validate and sanitize all user inputs
- Implement rate limiting for APIs
- Use secure cookies with HttpOnly and Secure flags
- Follow principle of least privilege for service accounts and database access
- Keep all NuGet packages and dependencies up to date