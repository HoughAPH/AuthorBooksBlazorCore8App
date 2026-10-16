using IdentityManagerServerApi.Domain;

namespace AuthorBooksBlazorCore.Test;

[TestFixture]
public class AuthorAggregateTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithBlankName_Throws(string? name)
    {
        Assert.That(() => Author.Create(name!), Throws.TypeOf<DomainException>());
    }

    [Test]
    public void Create_WithTooLongName_Throws()
    {
        Assert.That(() => Author.Create(new string('a', Author.NameMaxLength + 1)),
            Throws.TypeOf<DomainException>());
    }

    [Test]
    public void Create_TrimsName()
    {
        Assert.That(Author.Create("  Eric Evans  ").Name, Is.EqualTo("Eric Evans"));
    }

    [Test]
    public void Rename_WithBlankName_KeepsOriginalName()
    {
        Author author = Author.Create("Eric Evans");

        Assert.That(() => author.Rename(" "), Throws.TypeOf<DomainException>());
        Assert.That(author.Name, Is.EqualTo("Eric Evans"));
    }

    [Test]
    public void AddBook_WithDuplicateTitle_IgnoringCase_Throws()
    {
        Author author = Author.Create("Eric Evans");
        author.AddBook("Domain Driven Design");

        Assert.That(() => author.AddBook("  domain driven design "), Throws.TypeOf<DomainException>());
        Assert.That(author.Books, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddBook_WithBlankTitle_Throws()
    {
        Author author = Author.Create("Eric Evans");

        Assert.That(() => author.AddBook(""), Throws.TypeOf<DomainException>());
    }

    [Test]
    public void EnsureCanBeDeleted_WhenAuthorHasBooks_Throws()
    {
        Author author = Author.Create("Eric Evans");
        author.AddBook("Domain Driven Design");

        Assert.That(author.EnsureCanBeDeleted, Throws.TypeOf<DomainException>()
            .With.Message.EqualTo("Author cannot be deleted because the author has books."));
    }

    [Test]
    public void EnsureCanBeDeleted_WhenAuthorHasNoBooks_DoesNotThrow()
    {
        Assert.That(Author.Create("Eric Evans").EnsureCanBeDeleted, Throws.Nothing);
    }

    [Test]
    public void UpdateBook_WithUnknownBook_Throws()
    {
        Author author = Author.Create("Eric Evans");

        Assert.That(() => author.UpdateBook(42, "Title"), Throws.TypeOf<DomainException>()
            .With.Message.EqualTo("Book not found."));
    }

    [Test]
    public void UpdateBook_RenamesBook_AndRejectsDuplicateOfSibling()
    {
        Author author = Author.Create("Eric Evans");
        Book first = WithId(author.AddBook("First"), 1);
        WithId(author.AddBook("Second"), 2);

        author.UpdateBook(1, "First Edition");

        Assert.That(first.Title, Is.EqualTo("First Edition"));
        Assert.That(() => author.UpdateBook(1, "second"), Throws.TypeOf<DomainException>());
    }

    [Test]
    public void RemoveBook_RemovesOnlyThatBook()
    {
        Author author = Author.Create("Eric Evans");
        WithId(author.AddBook("First"), 1);
        WithId(author.AddBook("Second"), 2);

        author.RemoveBook(1);

        Assert.That(author.Books.Select(b => b.Title), Is.EqualTo(new[] { "Second" }));
    }

    [Test]
    public void TransferBook_MovesBookToTarget_KeepingIdentity()
    {
        Author source = Author.Create("Source");
        Author target = Author.Create("Target");
        Book book = WithId(source.AddBook("Moving Book"), 7);

        source.TransferBook(7, target, "Moved Book");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(source.Books, Is.Empty);
            Assert.That(target.Books, Has.Count.EqualTo(1));
            Assert.That(target.Books.Single(), Is.SameAs(book));
            Assert.That(book.Id, Is.EqualTo(7));
            Assert.That(book.Title, Is.EqualTo("Moved Book"));
            Assert.That(book.Author, Is.SameAs(target));
        }
    }

    [Test]
    public void TransferBook_WhenTargetAlreadyHasTitle_ThrowsAndLeavesSourceUntouched()
    {
        Author source = Author.Create("Source");
        Author target = Author.Create("Target");
        WithId(source.AddBook("Shared Title"), 7);
        target.AddBook("Shared Title");

        Assert.That(() => source.TransferBook(7, target, "Shared Title"), Throws.TypeOf<DomainException>());
        Assert.That(source.Books, Has.Count.EqualTo(1));
        Assert.That(target.Books, Has.Count.EqualTo(1));
    }

    // Ids are database-generated; tests that address a book by id assign one directly.
    private static Book WithId(Book book, int id)
    {
        typeof(Book).GetProperty(nameof(Book.Id))!.SetValue(book, id);
        return book;
    }
}
