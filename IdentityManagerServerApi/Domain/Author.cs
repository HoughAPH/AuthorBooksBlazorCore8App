using IdentityManagerServerApi.Exceptions;

namespace IdentityManagerServerApi.Domain;

/// <summary>
/// Aggregate root. <see cref="Book"/> can only be created, changed or removed through this type.
/// </summary>
public class Author
{
    public const int NameMaxLength = 200;

    private readonly List<Book> _books = [];

    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public IReadOnlyCollection<Book> Books => _books.AsReadOnly();

    private Author()
    {
    }

    public static Author Create(string name) => new() { Name = NormalizeName(name) };

    public void Rename(string name) => Name = NormalizeName(name);

    public Book AddBook(string title)
    {
        Book book = new(title);
        EnsureTitleIsUnique(book.Title, exceptBookId: null);
        _books.Add(book);
        return book;
    }

    public void UpdateBook(int bookId, string title)
    {
        Book book = GetBook(bookId);
        string normalized = Book.NormalizeTitle(title);
        EnsureTitleIsUnique(normalized, exceptBookId: bookId);
        book.Rename(normalized);
    }

    public void RemoveBook(int bookId) => _books.Remove(GetBook(bookId));

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
    }

    public bool HasBook(int bookId) => _books.Any(b => b.Id == bookId);

    public void EnsureCanBeDeleted()
    {
        if (_books.Count > 0)
            throw new DomainException("Author cannot be deleted because the author has books.", DomainErrorKind.Conflict);
    }

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
