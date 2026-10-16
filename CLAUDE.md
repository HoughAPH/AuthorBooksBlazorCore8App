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
- `IdentityManagerServerApi` — ASP.NET Core Web API (`AppDbContext`: SQL Server, Identity tables included). Author/Book/Account endpoints use a hand-rolled command/query split under `Features/{Authors,Books,Account}/{Commands,Queries}`: each file holds a command or query record plus its handler (`ICommandHandler<T>` returns a `Result`; `IQueryHandler<TQuery,TResult>` returns DTOs; handlers that also return a value, such as the login token, return `Result<T>`). Handlers are registered individually in `Program.cs` (no MediatR) and injected straight into the controllers. `ResultExtensions.ToActionResult` maps a `Result` to HTTP: success is 200 with a `GeneralResponse` body (the UI shows its message); failure is `application/problem+json` with the message in `detail` — 404 not found, 409 conflict (duplicate title, author still has books, email already registered), 401 unauthorized (bad login), 400 otherwise. `DomainException.Kind` (Validation/Conflict/NotFound) decides which. `[Required]` DTO validation fails earlier as a standard `ValidationProblemDetails` (`errors` map, no `detail`). Account handlers use `UserManager`/`RoleManager` directly (see Authentication). User management (`IManageUsers`, `ManageUsersRepository`) is deliberately left as-is and out of scope until the planned "author" role (which would link `Author` to an Identity user id) is designed.
- **`Author` is an aggregate root; `Book` is its child** (both, plus `DomainException`, in `Domain/`; the Serilog `Logs` table entity lives in `Data/`). All writes go through `Author` methods (`Create`, `Rename`, `AddBook`, `UpdateBook`, `RemoveBook`, `TransferBook`, `EnsureCanBeDeleted`), which throw `DomainException` on rule violations (handlers turn that into `Result.Invalid`). `Book` setters/constructor are private/internal and there is no `DbSet<Book>`; load aggregates via `IAuthorRepository` (always includes books) and read books via projections over `Authors`. Use `SelectMany(a => a.Books, (a, b) => ...)`, not a nested `Select` (needs APPLY, unsupported on SQLite). A unique index on `Books(AuthorId, Title)` backs the duplicate-title rule; `AuthorRepository.SaveChanges` turns a unique-violation `DbUpdateException` (SQL Server 2601/2627, SQLite "UNIQUE constraint failed") into a `DomainException` Conflict, and `GlobalExceptionsHandler` maps any `DomainException` that escapes a handler to the matching problem+json status (404/409/400).
- `BlazorWebAssemblyApp` — client consuming the API. Its `Services/` classes (`BookService`, `AuthorsService`, `AccountService`, ...) implement the shared contract interfaces over `HttpClient`. `IAuthors`/`IBooks`/`IUserAccount` are now implemented only by the client (the pages still consume `GeneralResponse`); the server no longer implements them, so the controllers' HTTP routes and status codes are the real contract. The client services adapt responses via `Services/ApiResponseReader.cs` (problem details → `GeneralResponse`); that file is also compiled directly into the test project (referencing the Blazor project would clash with the API's top-level `Program`).

### Authentication (JWT, branch `APIDDDUpgrade`)

The README still says cookie auth; the code has moved to JWT bearer.
- API: `AddIdentity` for user/role storage, but `AddJwtBearer` is the default authenticate/challenge scheme. Tokens are issued by `Features/Account/Queries/Login.cs` (failed passwords count toward Identity lockout; unknown email and wrong password give the same 401); config keys `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` (`Jwt:Key` is required at startup and is not in the tracked appsettings — supply it via user secrets or environment; tests set their own in `CustomWebApplicationFactory`).
- Client: `Authentication/TokenStorage` persists the token, `BearerTokenHandler` attaches it to the `HttpClient`, and `ApiAuthenticationStateProvider` builds the `ClaimsPrincipal` from it. `CustomAuthenticationStateProvider` is a leftover/alternate provider; `ApiAuthenticationStateProvider` is the one registered.
- CORS policy `BlazorWebassembly` is driven by `Cors:AllowedOrigins`. Only GET/POST/DELETE/PUT/HEAD are allowed, so adding PATCH/OPTIONS-dependent endpoints requires updating the policy.

### Testing setup

Integration tests use `WebApplicationFactory<Program>` via `CustomWebApplicationFactory` (the API exposes `public partial class Program`). It sets the environment to `IntegrationTesting`, which in `Program.cs`: skips the SQL Server `AddDbContext` registration, the Serilog config sink and `SeedAdminAsync`. The factory substitutes an in-memory SQLite connection, recreates and seeds the DB (authors 1–3, books 1–2, built through the aggregate with generated ids) on every client creation, and supplies a test `Jwt:Key` via `UseSetting` (user secrets only load in Development), and replaces auth with `TestAuthHandler` (defined in `BooksApiIntegrationTests.cs`; role chosen via a request header — use `CreateAuthenticatedClient(role)`). Don't add environment-specific startup code that breaks under `IntegrationTesting`.
Account tests (`AccountApiIntegrationTests`, `AdminSeederTests`) use the real Identity stack on SQLite. The factory creates the `Admin`/`User` roles, swaps `IResetTokenSender` for `CapturingResetTokenSender` (exposed as `factory.ResetTokens`), and takes extra config through `factory.Settings` (set before the first client; used for `Seed:*`). Every `CreateAnonymousClient`/`CreateAuthenticatedClient` call wipes and reseeds the database, so create clients before registering users. To call authenticated endpoints as a real user, pass that user's Identity id as `userId`. `AdminSeeder.SeedAdminAsync(IServiceProvider)` is invoked explicitly by tests, since the startup call is skipped.

## Conventions

- Nullable reference types: optional fields are `string?`; required fields are `string Prop { get; set; } = null!;` rather than `= string.Empty` (see `NotesReNullableStrings.txt`).
- Errors flow through `GlobalExceptionsHandler` (ProblemDetails); security headers via `UseSecurityHeaders()` in `Exceptions/SecurityHeadersExtensions.cs`. Rate limiting lives in `Features/Common/RateLimitPolicies.cs`: the `"writes"` policy (fixed window, default 20/min, override with `RateLimiting:WritesPerMinute`) is partitioned per user (`NameIdentifier` claim, else IP) and applied with `[EnableRateLimiting(RateLimitPolicies.Writes)]` to the add/update/delete actions and the anonymous account actions (register, login, forgot/reset password); rejections are 429 `problem+json` with `Retry-After`. `app.UseRateLimiter()` runs after `UseAuthorization()` so the user is known. New write endpoints must opt in with the attribute.

### Accounts, admin seeding and password reset

- Registration always creates a `User`. The `Admin` is seeded at startup by `Features/Account/AdminSeeder.cs` from `Seed:AdminEmail` / `Seed:AdminPassword` (optional `Seed:AdminName`); it also ensures the `Admin` and `User` roles exist, is idempotent, and is skipped under `IntegrationTesting`. Set the values with user secrets; the database must be migrated first or startup fails.
- Forgot-password never returns the token. `ForgotPasswordHandler` passes it to `IResetTokenSender` and always answers with the same 200 message, whether or not the email exists. Development registers `LoggingResetTokenSender` (token written to the API log; the user pastes it into the reset page); other environments get `NoResetTokenSender`, which never exposes it. Replace with a real email sender when email is in scope.
- Logout is a no-op on the server (stateless JWTs); the client just discards its token.
