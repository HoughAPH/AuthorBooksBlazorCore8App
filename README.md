# AuthorBooksBlazorCore

This solution started from the original task requirement on the `master` branch, where only the API was needed.

The Blazor WebAssembly application was added afterwards as a client so that the API endpoints could be exercised properly from a UI and not only through Swagger or ad hoc HTTP calls. It is therefore not the original requirement, but an extension to prove the API surface and authentication flow end to end.

## Solution overview

This is a .NET 10 solution built around a simple author and book management domain.

At a high level the solution uses:

- ASP.NET Core Web API
- Blazor WebAssembly
- Entity Framework Core
- ASP.NET Core Identity
- Repository pattern
- Shared contracts and DTOs through a class library

The design is not pure MVC in the classic ASP.NET MVC sense, but it follows the same separation of concerns:

- presentation/client concerns in the Blazor app
- HTTP endpoint concerns in the API
- data access concerns in repositories
- shared contracts, DTOs, and models in the shared library

## Projects

### `IdentityManagerServerApi`

This is the main API project and the original focus of the task.

Responsibilities include:

- exposing HTTP endpoints
- configuring EF Core and SQL Server access
- configuring ASP.NET Core Identity
- handling authentication and authorization
- registering repository implementations
- exception handling and problem details
- Swagger/OpenAPI support
- Serilog configuration

This project also contains:

- `AppDbContext`
- migrations
- controllers
- repository implementations
- Identity user implementation

### `BlazorWebAssemblyApp`

This project was added afterwards as a consumer of the API.

Its purpose is mainly practical:

- call the API from a browser client
- test authentication and authorization flow
- expose the CRUD endpoints through a simple UI
- validate the shared contracts from a real client application

It includes pages for:

- authors
- books
- login
- registration
- forgot password
- reset password
- user management

The account and user management pages are intentionally pragmatic. They are there to exercise the API and Identity features. They are not intended to represent a production-complete identity UI. In particular, there is no email service wired in for a realistic password reset flow.

### `SharedClassLibrary`

This project contains the shared application types used across both the API and the Blazor client.

Typical contents include:

- contracts/interfaces
- DTOs
- domain models
- response wrappers

The intention is to avoid duplicating request/response contracts between server and client.

## Architecture notes

The solution uses the repository pattern on top of EF Core.

That was a deliberate choice for this exercise, mainly to keep controller concerns and data-access concerns separated and to make the shared contracts explicit.

The flow is roughly:

- controller receives HTTP request
- controller delegates to a contract/repository service
- repository works through `AppDbContext`
- DTOs / response models are returned to the caller

For a small system like this, that level of abstraction is arguably more than EF Core strictly requires, but it keeps the code organised and aligns with the original intent of the exercise.

## Authentication and user management

The API uses ASP.NET Core Identity and persists authentication by means of secure cookies.

The Blazor client includes pages for account-related actions and a basic user manager page so that the Identity endpoints can be exercised from the UI.

This is functional, but deliberately lightweight:

- no email sender is configured
- forgot/reset password is therefore limited by that constraint
- the user management surface is mainly there to support testing and demonstration

## Security

This solution is a simple demonstrator for authentication and authorization in ASP.NET Core.

At present, authentication is implemented using ASP.NET Core Identity with cookies rather than JWT bearer tokens. Role-based authorization is also used so that selected endpoints and application actions can be restricted according to user role.

That said, this repository should still be viewed as a learning and demonstration project rather than a production security template. Some areas are intentionally simplified, especially around password recovery, operational hardening, and full production identity workflows.

## Why the Blazor client was added

The API on its own satisfies the original task intent. The Blazor client was added to answer a practical question: can the API actually be consumed cleanly from an application that has to authenticate, call endpoints, and handle user workflows?

That addition made it easier to validate:

- CORS setup
- cookie authentication behaviour
- protected endpoints
- CRUD flows
- shared DTO usage
- basic account flows

## Running the solution

### Prerequisites

- .NET 10 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio 2026 or equivalent tooling

### Configuration

Before running the solution, configure the following values:

#### API project (`IdentityManagerServerApi`)
- `ConnectionStrings:DefaultConnection`
- `Cors:AllowedOrigins`
- Serilog sink settings if SQL logging is enabled

#### Blazor WebAssembly project (`BlazorWebAssemblyApp`)
- `ApiSettings:BaseUrl` in `wwwroot/appsettings.json`

For local development, these values can be stored in:
- `appsettings.json`
- `appsettings.Development.json`
- user secrets for sensitive values

Do not commit real secrets to source control.

### Database

The API project contains `AppDbContext` and migrations.

Run EF Core migrations against the API project.

### Startup

The solution can be started with both:

- `IdentityManagerServerApi`
- `BlazorWebAssemblyApp`

The Blazor app is configured to call the API locally.

## Current status

This is a working exercise solution with a practical client added on top of the API.

It should be viewed as:

- a task implementation for the API
- plus a Blazor client used to expose and invoke the API in a more realistic way

Some parts are intentionally simplified, especially around account recovery and production-grade identity workflows.

## Known limitations / demo scope

This repository is intended as a demonstration project rather than a production template.

Current limitations include:

- the UI is intentionally simple and focused on exercising API flows
- password reset flow is limited because no email sender is configured
- configuration still assumes local development defaults unless overridden
- error handling is pragmatic rather than fully standardized across every feature
- the solution favors clarity and end-to-end demonstration over production hardening

If this were being taken further, the next steps would include stronger configuration management, more consistent API conventions, improved validation/error contracts, and a more polished user experience.

## Summary

If reviewing the repository against the original requirement, the `master` branch represents the original task scope more directly.

This branch/solution goes further by adding:

- a Blazor WebAssembly client
- account pages
- a simple user manager UI
- a fuller demonstration of how the API can be consumed

That extra work was mainly to validate the API properly rather than leave it as a server-only exercise.That extra work was mainly to validate the API properly rather than leave it as a server-only exercise.