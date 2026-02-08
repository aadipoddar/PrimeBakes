---
description: 'Guidelines for building C# applications'
applyTo: '**/*.cs'
---

# C# Development Guidelines

## Official Documentation References

**ALWAYS** reference the latest official documentation when working with C# and .NET:

- **.NET Documentation**: https://learn.microsoft.com/en-us/dotnet/
- **.NET Fundamentals**: https://learn.microsoft.com/en-us/dotnet/fundamentals/
- **ASP.NET Core**: https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-10.0
- **Blazor**: https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor
- **Azure**: https://learn.microsoft.com/en-us/azure/?product=popular
- **Azure China**: https://docs.azure.cn/en-us/

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
- Write code with good maintainability practices, including comments explaining why certain design decisions were made
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
- Use the editor's error diagnostics to identify and fix issues
- Never leave code in a non-compiling state
- If errors occur:
  - Read the error messages carefully and understand the root cause
  - Fix errors systematically, starting with the first error (subsequent errors may be cascading)
  - Re-build after each fix to verify the issue is resolved
  - Check for missing using statements, incorrect namespaces, or typos
  - Verify NuGet package references and versions are correct
- Test the changes functionally after successful compilation
- Run `dotnet restore` if package-related errors occur
- Clear bin/obj folders if strange build errors persist: `dotnet clean`

## Naming Conventions

- Follow PascalCase for component names, method names, and public members
- Use camelCase for private fields and local variables
- Prefix interface names with "I" (e.g., IUserService)

## Formatting

- Apply code-formatting style defined in `.editorconfig`
- Prefer file-scoped namespace declarations (namespace MyApp;)
- Use single-line using directives and global usings where appropriate
- Insert a newline before the opening curly brace of any code block
- Ensure that the final return statement of a method is on its own line
- Use pattern matching and switch expressions wherever possible
- Use `nameof` instead of string literals when referring to member names
- Ensure that XML doc comments (`///`) are created for any public APIs
- When applicable, include `<summary>`, `<param>`, `<returns>`, `<exception>`, `<example>`, and `<code>` documentation
- Use expression-bodied members for simple properties and methods
- Group using directives: System namespaces first, then third-party, then project namespaces
- Order class members: constants, fields, constructors, properties, methods, events
- Use `var` for local variables when the type is obvious

## Project Setup and Structure

- Guide users through creating new .NET projects with appropriate templates
- Explain the purpose of each generated file and folder to build understanding
- Demonstrate how to organize code using feature folders or domain-driven design principles
- Show proper separation of concerns with models, services, and data access layers
- Explain the Program.cs and configuration system in ASP.NET Core including environment-specific settings
- Use minimal hosting model in .NET with top-level statements
- Implement proper project references and NuGet package management
- Structure multi-project solutions (like Shared, Web, MAUI)
- Separate shared code into reusable class libraries
- Implement proper configuration using appsettings.json and user secrets

## Nullable Reference Types

- **IMPORTANT**: This project has Nullable disabled (`<Nullable>disable</Nullable>`)
- Be aware that null reference warnings are not enforced
- Still implement defensive null checks at entry points and public APIs
- Always use `is null` or `is not null` instead of `== null` or `!= null`
- Consider enabling nullable reference types in new projects for better null safety
- Use null-forgiving operator `!` sparingly and only when absolutely certain
- Add `#nullable enable` directive in individual files when migrating to nullable reference types

## Data Access Patterns

- Explain different options for data access based on the project (SQL Server, Azure SQL, Firebase, Blob Storage)
- This project uses:
  - SQL Server with Dapper (SqlDataAccess.cs)
  - Azure Blob Storage (BlobDataAccess.cs)
  - Firebase Admin SDK for push notifications
- Demonstrate repository pattern implementation and when it's beneficial
- Show how to implement database migrations using SQL Server Data Tools (SSDT) projects
- Explain efficient query patterns to avoid common performance issues (N+1 queries, over-fetching)
- Use parameterized queries to prevent SQL injection
- Implement proper connection string management using configuration and Azure Key Vault
- Use async data access methods consistently (ExecuteAsync, QueryAsync)
- Implement proper transaction handling when needed
- Consider using Entity Framework Core for complex domain models
- Implement data transfer objects (DTOs) to decouple data models from domain models

## Validation and Error Handling

- Guide the implementation of model validation using Data Annotations and FluentValidation
- Explain the validation pipeline and how to customize validation responses
- Demonstrate a global exception handling strategy using middleware in ASP.NET Core
- Show how to create consistent error responses across APIs
- Explain Problem Details (RFC 9457) implementation for standardized error responses
- Implement custom exception types for domain-specific errors
- Use try-catch-finally blocks appropriately
- Log exceptions with proper context using ILogger
- Handle specific exceptions before general ones
- Consider using Result patterns or OneOf for functional error handling
- Implement validation for Syncfusion component inputs
- Provide user-friendly error messages in the UI using Syncfusion Toast or Dialog

## API Versioning and Documentation

- Guide users through implementing and explaining API versioning strategies
- Demonstrate Swagger/OpenAPI implementation with proper documentation
- Show how to document endpoints, parameters, responses, and authentication
- Explain versioning in both controller-based and Minimal APIs
- Guide users on creating meaningful API documentation that helps consumers

## Performance Optimization

- Guide users on implementing caching strategies (in-memory, distributed, response caching)
- Use IMemoryCache for in-memory caching
- Consider Azure Redis Cache for distributed caching scenarios
- Explain asynchronous programming patterns and why they matter for performance
- Use Task.WhenAll for parallel async operations when appropriate
- Demonstrate pagination, filtering, and sorting for large datasets (especially with Syncfusion Grid)
- Show how to implement compression and other performance optimizations
- Explain how to measure and benchmark performance using BenchmarkDotNet
- Implement database query optimization (proper indexing, avoiding SELECT *, using projections)
- Use ValueTask<T> for hot paths where allocations matter
- Implement object pooling for frequently allocated objects (ArrayPool, ObjectPool)
- Minimize allocations in hot paths
- Use Span<T> and Memory<T> for efficient buffer management
- Leverage Syncfusion's virtualization features for large data grids

## Deployment and DevOps

- Guide users through containerizing applications using .NET's built-in container support:
  - `dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer`
- Explain CI/CD pipelines for .NET applications (GitHub Actions, Azure DevOps)
- Demonstrate deployment to:
  - Azure App Service (Blazor Server/Web)
  - Azure Container Apps
  - Azure Static Web Apps (Blazor WebAssembly)
  - Google Play Store / Apple App Store (MAUI apps)
- Show how to implement health checks and readiness probes
- Explain environment-specific configurations for different deployment stages
- Use Azure Key Vault for secrets management
- Implement Application Insights for monitoring production applications
- Set up proper staging and production environments
- Configure Azure SQL Database and Azure Blob Storage connections
- Implement automated testing in CI/CD pipelines
- Use Azure DevOps or GitHub Actions for automated deployments

## Project-Specific Guidelines

- **Multi-Project Solution**: PrimeBakes has multiple projects (MAUI, Web, Shared, Library, API)
- **Shared Library**: Business logic is in `PrimeBakesLibrary` - always check here before adding new data access code
- **Database**: SQL Server database project in `DBPrimeBakes` with stored procedures and views
- **API**: Push notifications API in `PushNotificationsAPI` using Firebase Admin SDK
- **Excel Import**: Utility project `ExcelImport` for data import operations
- **Code Reuse**: Maximize code sharing between Web and MAUI projects through `PrimeBakes.Shared`
- **Dependency Injection**: Register services appropriately for each project type
- **Configuration**: Use appsettings.json for Web, MauiProgram.cs for MAUI
- **Data Models**: Keep models in `PrimeBakesLibrary/Models` for consistency
- **Export Logic**: Implement export functionality in `PrimeBakesLibrary/Exporting`

## Security Best Practices

- Use HTTPS everywhere and enforce it in production
- Implement proper CORS policies for API access
- Use secure headers middleware
- Validate and sanitize all user inputs
- Implement rate limiting for APIs
- Use secure cookies with HttpOnly and Secure flags
- Follow principle of least privilege for service accounts and database access
- Keep all NuGet packages and dependencies up to date
- Regular security scanning with tools like OWASP Dependency-Check
