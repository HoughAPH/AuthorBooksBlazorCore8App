namespace IdentityManagerServerApi.Domain;

/// <summary>
/// Aggregate root. <see cref="Book"/> can only be created, changed or removed through this type.
/// </summary>
/// <remarks>
/// <see cref="Version"/> is the aggregate's optimistic-concurrency token. Every change to the author or its books
/// replaces it, so two writers that loaded the same state cannot both save: the second one finds the stored version
/// no longer matches and is rejected as a conflict.
/// </remarks>
public class Author
{
    public const int NameMaxLength = 200;

    private readonly List<Book> _books = [];

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>Concurrency token; changes whenever the author or any of its books changes.</summary>
    public Guid Version { get; private set; } = Guid.NewGuid();

    public IReadOnlyCollection<Book> Books => _books.AsReadOnly();

    private Author()
    {
    }

    public static Author Create(string name) => new() { Name = NormalizeName(name) };

    /// <summary>
    /// Rejects a change made from a stale read. Passing <see langword="null"/> skips the check, which leaves the
    /// database-level token as the only guard against simultaneous writers.
    /// </summary>
    public void EnsureVersion(Guid? expected)
    {
        if (expected is not null && expected != Version)
            throw new DomainException(
                "This author was changed by someone else since you loaded it. Reload and try again.",
                DomainErrorKind.Conflict);
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        Touch();
    }

    public Book AddBook(string title)
    {
        Book book = new(title);
        EnsureTitleIsUnique(book.Title, exceptBookId: null);
        _books.Add(book);
        Touch();
        return book;
    }

    public void UpdateBook(int bookId, string title)
    {
        Book book = GetBook(bookId);
        string normalized = Book.NormalizeTitle(title);
        EnsureTitleIsUnique(normalized, exceptBookId: bookId);
        book.Rename(normalized);
        Touch();
    }

    public void RemoveBook(int bookId)
    {
        _books.Remove(GetBook(bookId));
        Touch();
    }

    /// <summary>Moves a book (keeping its identity) to another author, optionally renaming it.</summary>
    public void TransferBook(int bookId, Author target, string title)
    {
        Book book = GetBook(bookId);
        string normalized = Book.NormalizeTitle(title);
        target.EnsureTitleIsUnique(normalized, exceptBookId: null);

        _books.Remove(book);
        book.Rename(normalized);
        book.MoveTo(target);
        target._books.Add(book);
        Touch();
        target.Touch();
    }

    public bool HasBook(int bookId) => _books.Any(b => b.Id == bookId);

    public void EnsureCanBeDeleted()
    {
        if (_books.Count > 0)
            throw new DomainException("Author cannot be deleted because the author has books.", DomainErrorKind.Conflict);
    }

    private void Touch() => Version = Guid.NewGuid();

    private Book GetBook(int bookId) =>
        _books.FirstOrDefault(b => b.Id == bookId)
        ?? throw new DomainException("Book not found.", DomainErrorKind.NotFound);

    private void EnsureTitleIsUnique(string title, int? exceptBookId)
    {
        bool duplicate = _books.Any(b =>
            b.Id != exceptBookId && string.Equals(b.Title, title, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
            throw new DomainException("This author already has a book with that title.", DomainErrorKind.Conflict);
    }

    private static string NormalizeName(string? name)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            throw new DomainException("Author name is required.");

        if (trimmed.Length > NameMaxLength)
            throw new DomainException($"Author name cannot exceed {NameMaxLength} characters.");

        return trimmed;
    }
}
