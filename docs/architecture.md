# Architecture

## Projects

| Project | Role |
|---|---|
| `IdentityManagerServerApi` | ASP.NET Core Web API: controllers, command/query handlers (`Features/`), the `Author` / `Book` domain model, `AppDbContext` and migrations, repository, Identity, JWT, rate limiting, exception handling, Serilog, Swagger. |
| `BlazorWebAssemblyApp` | Browser client that consumes the API (authors, books, login, registration, password reset, user management). Its `Services/` classes implement the shared contracts over `HttpClient`. |
| `SharedClassLibrary` | Contracts (interfaces), DTOs and the `Result` types shared by the API and the client, so request and response shapes are not duplicated. |
| `AuthorBooksBlazorCore.Test` | NUnit tests: domain, controllers, API integration (in-memory SQLite) and the client's `ResultReader`. |

The account and user-management pages are intentionally pragmatic: they exist to exercise the API and Identity features, not as a production identity UI.

## Design notes

- **Aggregate and repository.** `Author` is the aggregate root; `Book` is only reachable through it, and there is no `DbSet<Book>`. `IAuthorRepository` loads and saves whole aggregates, so every rule lives in one place.
- **Commands and queries.** Writes are commands that load the aggregate, call a domain method and save. Reads are queries that project straight from `AppDbContext` to DTOs (`AsNoTracking`) without loading aggregates. Handlers are registered individually in `Program.cs`; there is no MediatR.
- **Results, not exceptions, across the API boundary.** Handlers return `Result` / `Result<T>`. `ResultExtensions.ToActionResult` turns them into HTTP responses, and a `DomainException` that escapes a handler is mapped by the global exception handler (404 / 409 / 400).
- **Concurrency.** `Author.Version` is an EF concurrency token that the aggregate replaces on every change. The client sends back the version it read; a mismatch, or a race caught by EF, is a `409`. The unique `(AuthorId, Title)` index backs the duplicate-title rule for writes that bypass the version.
- **Paging.** `GetAuthorsPage` and `GetBooksPage` page and search on the server with a capped page size. The un-paged `GetAuthors` / `GetBooks` remain for dropdowns and simple consumers.

For a system this small the structure is more than strictly necessary. It is deliberate, to show where each concern lives and how it is tested.

## Authentication and security

- The API uses ASP.NET Core Identity for user and role storage and issues **JWT bearer tokens** at login. The Blazor client stores the token and attaches it to API calls.
- Role-based authorization (`Admin` / `User`) restricts the write endpoints and some pages. Failed logins count toward Identity lockout, and write and account endpoints are rate limited per user (or IP).
- Registration always creates a `User`; the `Admin` is seeded at startup from configuration.
- Forgot-password never returns the token and never reveals whether an email exists. In Development the token is written to the API log; elsewhere it is not exposed, because no email sender is configured.
- This is a learning and demonstration project rather than a production security template, in particular around password recovery and token handling.

## Why there is a Blazor client

The API alone satisfies the original task. The client was added to check, from a real browser application, that CORS, JWT authentication, protected endpoints, CRUD flows and the shared DTOs all work together.
