using IdentityManagerServerApi.Exceptions;

namespace IdentityManagerServerApi.Domain;

public class Book
{
    public const int TitleMaxLength = 200;

    public int Id { get; private set; }

    public string Title { get; private set; } = null!;

    public int AuthorId { get; private set; }

    public Author Author { get; private set; } = null!;

    private Book()
    {
    }

    internal Book(string title)
    {
        Title = NormalizeTitle(title);
    }

    internal void Rename(string title) => Title = NormalizeTitle(title);

    internal void MoveTo(Author author)
    {
        Author = author;
        AuthorId = author.Id;
    }

    internal static string NormalizeTitle(string? title)
    {
        string trimmed = title?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            throw new DomainException("Book title is required.");

        if (trimmed.Length > TitleMaxLength)
            throw new DomainException($"Book title cannot exceed {TitleMaxLength} characters.");

        return trimmed;
    }
}
