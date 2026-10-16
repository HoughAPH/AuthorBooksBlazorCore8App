# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

.NET 10 solution (`AuthorBooksBlazorCore.slnx`). Run from the repo root.

- Build: `dotnet build AuthorBooksBlazorCore.slnx`
- Run API: `dotnet run --project IdentityManagerServerApi` (HTTPS; Swagger is enabled in Development; health check at `/health`)
- Run client: `dotnet run --project BlazorWebAssemblyApp` (default origin `https://localhost:7254`)
- All tests: `dotnet test AuthorBooksBlazorCore.Test`
- Single test: `dotnet test AuthorBooksBlazorCore.Test --filter "FullyQualifiedName~BooksApiIntegrationTests.<TestName>"`
- EF migrations (API project owns `AppDbContext`): `dotnet ef migrations add <Name> --project IdentityManagerServerApi` / `dotnet ef database update --project IdentityManagerServerApi`

No linter is configured.

## Architecture

Three projects plus tests:

- `SharedClassLibrary` — referenced by both API and client. Holds the `Contracts/` interfaces (`IBooks`, `IAuthors`, `IUserAccount`, `IManageUsers`, ...), DTOs, response models, and `JwtHelper`.
- `IdentityManagerServerApi` — ASP.NET Core Web API. Controllers delegate to repositories in `Repositories/`, which use `AppDbContext` (SQL Server, Identity tables included).
- `BlazorWebAssemblyApp` — client consuming the API. Its `Services/` classes (`BookService`, `AuthorsService`, `AccountService`, ...) implement the **same shared contract interfaces** as the server repositories, but over `HttpClient`. So a contract change in `SharedClassLibrary/Contracts` must be updated in both the server repository and the client service.

### Authentication (JWT, branch `APIDDDUpgrade`)

The README still says cookie auth; the code has moved to JWT bearer.
- API: `AddIdentity` for user/role storage, but `AddJwtBearer` is the default authenticate/challenge scheme. Tokens are issued in `Repositories/IdentityAccountService.cs`; config keys `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` (`Jwt:Key` is required at startup and is not in the tracked appsettings — supply it via user secrets or environment).
- Client: `Authentication/TokenStorage` persists the token, `BearerTokenHandler` attaches it to the `HttpClient`, and `ApiAuthenticationStateProvider` builds the `ClaimsPrincipal` from it. `CustomAuthenticationStateProvider` is a leftover/alternate provider; `ApiAuthenticationStateProvider` is the one registered.
- CORS policy `BlazorWebassembly` is driven by `Cors:AllowedOrigins`. Only GET/POST/DELETE/PUT/HEAD are allowed, so adding PATCH/OPTIONS-dependent endpoints requires updating the policy.

### Testing setup

Integration tests use `WebApplicationFactory<Program>` via `CustomWebApplicationFactory` (the API exposes `public partial class Program`). It sets the environment to `IntegrationTesting`, which in `Program.cs`: skips the SQL Server `AddDbContext` registration and the Serilog config sink. The factory substitutes an in-memory SQLite connection, recreates and seeds the DB (authors 1–3, books 1–2) on every client creation, and replaces auth with `TestAuthHandler` (defined in `BooksApiIntegrationTests.cs`; role chosen via a request header — use `CreateAuthenticatedClient(role)`). Don't add environment-specific startup code that breaks under `IntegrationTesting`.

## Conventions

- Nullable reference types: optional fields are `string?`; required fields are `string Prop { get; set; } = null!;` rather than `= string.Empty` (see `NotesReNullableStrings.txt`).
- Errors flow through `GlobalExceptionsHandler` (ProblemDetails); security headers via `UseSecurityHeaders()` in `Exceptions/SecurityHeadersExtensions.cs`. A `"writes"` rate-limit policy is defined in `Program.cs`.
