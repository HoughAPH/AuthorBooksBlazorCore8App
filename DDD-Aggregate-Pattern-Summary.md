# The DDD Aggregate Pattern in AuthorBooksBlazorCore

An educational summary of the architecture on branch `APIDDDUpgrade`. Everything below refers to code that is
actually in this repository; file paths are given so you can read along.

---

## 1. The short version

| | Before | After |
|---|---|---|
| Entities | Plain data bags: public setters, `[Required]` attributes | `Author` / `Book` with private setters; behaviour lives on the entity |
| Where the rules lived | Scattered through `AuthorsRepository` / `BooksRepository` (or nowhere) | Inside `Author` (`Domain/Author.cs`) |
| How a book was added | `_dbContext.Books.Add(new Book { Title, AuthorId })` | `author.AddBook(title)` |
| Who could change a `Book` | Anything that could reach a `DbContext` | Only its `Author` |
| Request handling | One big repository class implementing `IBooks` / `IAuthors` | One small command/query handler per use case |
| Errors | `GeneralResponse(false, "...")` returned with HTTP 200/400 | `DomainException` -> `Result` -> proper status code + `problem+json` |

---

## 2. What the old structure was doing

The original design was the classic "anemic model + fat repository" shape:

```csharp
// Old Entities/Author.cs and Entities/Book.cs
public class Book
{
    [Key] public int Id { get; set; }
    [Required] public string Title { get; set; } = null!;
    [Required] public int AuthorId { get; set; }
    public virtual Author Author { get; set; } = null!;
}
```

and a repository that did all the thinking:

```csharp
// Old Repositories/BooksRepository.cs (abridged)
public async Task<GeneralResponse> AddBook(BookDTO book)
{
    _dbContext.Books.Add(new Book { Title = book.Title, AuthorId = book.AuthorId });
    int rowsAffected = await _dbContext.SaveChangesAsync();
    ...
}

public async Task<GeneralResponse> UpdateBook(int id, BookDTO book)
{
    Book? bookToUpdate = await _dbContext.Books.FindAsync(id);
    bookToUpdate.Title = book.Title;
    bookToUpdate.AuthorId = book.AuthorId;   // any caller can re-parent a book
    ...
}
```

Problems with that shape:

- **The entities protected nothing.** Every property had a public setter, so any code could put a book (or an
  author) into an invalid state: blank title, 10,000-character title, a duplicate title for the same author.
  `[Required]` only checks for `null`; it does not trim, check length, or know about duplicates.
- **Rules lived wherever someone remembered to put them.** If one endpoint checked for duplicate titles and another
  did not, the data could still be corrupted through the second one. There was no single place to read "what are the
  rules for a book?".
- **The repository implemented the contract the UI used** (`IBooks`, `IAuthors`), so HTTP concerns
  (`GeneralResponse`, status messages) were tangled into data access.
- **Books could be modified and queried independently of their author**, via `DbSet<Book>`. Nothing expressed that a
  book only makes sense as part of an author's catalogue.

---

## 3. What the new design does

### 3.1 Layers

```
Controllers/            HTTP only: bind the request, call a handler, map the Result to a status code
Features/{Authors,Books,Account}/{Commands,Queries}
                        One record + one handler per use case (CreateAuthor, AddBook, ...)
Domain/                 Author, Book, DomainException  <-- the business rules
Repositories/           IAuthorRepository: load / add / remove / save an Author aggregate
Data/                   AppDbContext (EF Core mapping)
```

A request now flows like this:

```
POST /api/Book/AddBook
  -> BookController                       (binds BookDTO, [Authorize], rate limit)
  -> AddBookHandler.Handle(AddBookCommand)
       author = await authors.GetById(id)         (loads the Author *with its books*)
       author.AddBook(title)                      (the rule check happens here; may throw DomainException)
       await authors.SaveChanges()                (one unit of work)
  -> Result.Ok / Result.From(domainException)
  -> ResultExtensions.ToActionResult              (200 GeneralResponse, or 404/409/400 problem+json)
```

The handler contains no business rules; it is orchestration. The rules are in the domain.

### 3.2 The domain model

`Domain/Author.cs` (trimmed):

```csharp
public class Author   // aggregate root
{
    private readonly List<Book> _books = [];
    public IReadOnlyCollection<Book> Books => _books.AsReadOnly();   // read-only view

    private Author() { }                                              // EF only
    public static Author Create(string name) => new() { Name = NormalizeName(name) };
    public void Rename(string name) => Name = NormalizeName(name);

    public Book AddBook(string title)
    {
        Book book = new(title);
        EnsureTitleIsUnique(book.Title, exceptBookId: null);
        _books.Add(book);
        return book;
    }

    public void UpdateBook(int bookId, string title) { ... }
    public void RemoveBook(int bookId) { ... }
    public void TransferBook(int bookId, Author target, string title) { ... }
    public void EnsureCanBeDeleted() { if (_books.Count > 0) throw new DomainException(..., Conflict); }
}
```

`Domain/Book.cs`:

```csharp
public class Book
{
    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    private Book() { }                        // EF only
    internal Book(string title) { ... }       // only code in the same assembly (i.e. Author) can construct one
    internal void Rename(string title) { ... }
    internal void MoveTo(Author author) { ... }
}
```

Key design choices:

- **Private setters, private/internal constructors.** The only way to make or change an `Author` or `Book` is through
  the methods above, and every one of those methods validates.
- **`_books` is private; `Books` is a read-only view.** Callers cannot do `author.Books.Add(...)` and sidestep the
  duplicate-title rule.
- **No `DbSet<Book>`** in `AppDbContext`. You cannot even write `db.Books.Add(...)` any more. EF still maps `Book`
  (via the `Author.Books` navigation, backed by the `_books` field), but the application cannot reach it directly.
- **Rule violations throw `DomainException`** carrying a `Kind` (`Validation`, `Conflict`, `NotFound`). Handlers catch
  it and turn it into a `Result`, which `ToActionResult` turns into 400 / 409 / 404.
- **Reads are separate.** Queries (`GetBooksHandler`, ...) project straight to DTOs with `SelectMany` over `Authors`.
  They don't load aggregates, so reading stays cheap while writing stays safe. (This is the light "CQRS" split in
  the `Features/` folders.)

### 3.3 Why the handlers are small

`CreateAuthorHandler`, `AddBookHandler`, etc. each do one thing: load the aggregate, call one domain method, save.
Because there is one class per use case you can change "add a book" without touching "delete an author", and each
handler can be unit-tested with a fake `IAuthorRepository`.

---

## 4. What exactly is an Aggregate Root (for Authors and Books)?

An **aggregate** is a cluster of objects that must always be consistent *together*, treated as a single unit when
you load, change and save. The **aggregate root** is the one object in the cluster that the outside world is
allowed to talk to. Everything else in the cluster is reached *through* it.

In your model:

```
   Author  (aggregate root)          <- the only object handlers load, change, add, remove
     |
     +-- Book   (child entity)       <- exists only inside an Author; reached via author.AddBook / UpdateBook / ...
     +-- Book
```

Why `Author` is the root and `Book` is the child here:

- **A book has no life of its own in your domain.** It always belongs to exactly one author, and its most important
  rule ("this author already has a book with that title") is a rule about *the author's whole collection of books*.
- **To check that rule you need all of the author's books.** Only the object that owns the collection can check it
  reliably, so the collection owner (`Author`) is the root.
- **Deleting an author with books is forbidden**, which is again a rule about the whole cluster
  (`EnsureCanBeDeleted`).
- **The aggregate is the transaction boundary.** `AddBook` loads one author (with `Include(a => a.Books)` in
  `AuthorRepository`, which *always* includes books), changes it in memory, and a single `SaveChangesAsync()` persists
  the change. A half-applied change to one author is not possible.

Rules of thumb that the code follows:

1. Outside code holds references to the **root** only (`IAuthorRepository` has `GetById`, `GetByBookId`, `Add`,
   `Remove`; there is no `IBookRepository`). `GetByBookId` still returns the *author*, not the book.
2. All changes go through **methods on the root**.
3. The root enforces the **invariants** (rules that must always be true).
4. Other aggregates would be referenced by id, not by object. (Not needed yet. It becomes relevant if you add the
   planned "author" user role: you would store the owning Identity user id on `Author`, not link the objects.)

`TransferBook` shows the cost/benefit nicely: moving a book between authors involves *two* aggregates, so it is
exposed as a method on one root that takes the other as a parameter, checks the target's uniqueness rule, and then
moves the book, with both authors loaded and saved in one unit of work.

---

## 5. Why this is considered good practice for .NET APIs

- **A single source of truth for rules.** If "titles must be unique per author and at most 200 characters" is
  written in exactly one place, no endpoint can forget it. New endpoints get the rule for free.
- **Invalid states become unrepresentable.** With private setters you cannot construct a blank-titled `Book` from
  outside the domain; the compiler stops you. Tests become simpler (`AuthorAggregateTests` exercises rules without
  HTTP or a database).
- **Clear boundaries.** Controllers deal with HTTP, handlers with orchestration, the domain with rules,
  repositories with persistence. Each can change independently (e.g. swap SQL Server for another store without
  touching a rule).
- **Correct HTTP semantics.** Because rule failures carry a `Kind`, the API returns 404 / 409 / 400 with
  `application/problem+json` instead of "200 OK, flag = false". Clients and monitoring tools can rely on status
  codes.
- **Small, focused use-case classes** are easier to read, review, test and secure (each write action opts in to
  authorization and rate limiting explicitly).
- **EF Core fits it well.** Backing fields (`UsePropertyAccessMode(PropertyAccessMode.Field)`), private
  constructors and owned collections are all first-class EF Core features designed for this style.

It is not free: there is more ceremony than "set a property and save", and for pure CRUD with no rules a simple
repository is fine. It pays off as soon as rules exist and more than one place can write the data, which is true of
your Authors/Books.

---

## 6. How it protects your data integrity

Integrity is protected in layers, each catching what the previous one cannot:

| Layer | What it enforces | Where |
|---|---|---|
| Request validation | Required fields, formats, password confirmation | `[Required]` etc. on DTOs -> automatic `ValidationProblemDetails` |
| **Domain invariants** | Name/title required, trimmed, <= 200 chars; no duplicate title per author (case-insensitive); book must exist to be updated/removed; author with books cannot be deleted | `Author`, `Book` |
| Encapsulation | Nothing can bypass the rules: no public setters, no public `Book` constructor, no `DbSet<Book>`, read-only `Books` | `Domain/`, `AppDbContext` |
| Database schema | NOT NULL, max length 200 on `Name` / `Title`, required foreign key, cascade delete, unique `(AuthorId, Title)` | `AppDbContext.OnModelCreating` |
| Authorization / abuse limits | Only `Admin` can write; per-user write rate limit | Controllers, `RateLimitPolicies` |

Concrete examples of what the old code allowed and the new code stops:

- `UpdateBook` used to copy `book.AuthorId` straight onto the entity. Now re-parenting is an explicit
  `TransferBook` that checks the destination author's duplicate rule.
- A duplicate title for the same author used to be possible. Now `AddBook`/`UpdateBook`/`TransferBook` all go through
  `EnsureTitleIsUnique`.
- Deleting an author used to depend on the caller remembering to check for books. Now the handler must call
  `author.EnsureCanBeDeleted()`; and the database relationship is additionally configured so a book cannot exist
  without an author.

### Honest limitations (worth knowing)

- **Duplicate titles are enforced twice (this one is now closed).** The domain rejects them in memory  (`EnsureTitleIsUnique`), and a unique index on `Books(AuthorId, Title)` (migration `UniqueBookTitlePerAuthor`)  catches two concurrent requests that both pass that check. `AuthorRepository.SaveChanges` translates the database  error into the same 409 conflict. The index uses the column collation, which is case-insensitive on SQL Server's  default, so it agrees with the domain's case-insensitive check.
- **There is no optimistic-concurrency token** (e.g. a `rowversion`). Two editors renaming the same author at once:
  last write wins.
- **Cascade delete is configured at the database level**, so the "author with books can't be deleted" rule exists
  only in the domain; anything bypassing the aggregate (raw SQL, another service) could cascade-delete books. The
  aggregate is the guard, so keep all writes going through it.
- The aggregate rules cover Authors/Books only. Account/Identity handling (`Features/Account`) uses ASP.NET Core
  Identity's `UserManager` directly and is not modelled as an aggregate.

---

## 7. Where to look in the code

| Concept | File |
|---|---|
| Aggregate root and rules | `IdentityManagerServerApi/Domain/Author.cs` |
| Child entity | `IdentityManagerServerApi/Domain/Book.cs` |
| Domain error kinds | `IdentityManagerServerApi/Domain/DomainException.cs` |
| EF mapping (field access, no `DbSet<Book>`) | `IdentityManagerServerApi/Data/AppDbContext.cs` |
| Aggregate repository | `IdentityManagerServerApi/Repositories/AuthorRepository.cs` |
| Use-case handlers | `IdentityManagerServerApi/Features/{Authors,Books}/{Commands,Queries}/` |
| `Result` -> HTTP mapping | `IdentityManagerServerApi/Features/Common/ResultExtensions.cs` |
| Rule tests (no HTTP/DB) | `AuthorBooksBlazorCore.Test/AuthorAggregateTests.cs` |
| End-to-end tests | `AuthorBooksBlazorCore.Test/*IntegrationTests.cs` |

Suggested reading order: `Author.cs` -> `AddBook.cs` -> `ResultExtensions.cs` -> `AuthorAggregateTests.cs`.
