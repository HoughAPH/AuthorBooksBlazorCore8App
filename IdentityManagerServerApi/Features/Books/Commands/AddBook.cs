using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Features.Common;
using IdentityManagerServerApi.Repositories;

namespace IdentityManagerServerApi.Features.Books.Commands;

public record AddBookCommand(string Title, int AuthorId);

public class AddBookHandler(IAuthorRepository authors) : ICommandHandler<AddBookCommand>
{
    public async Task<Result> Handle(AddBookCommand command)
    {
        Author? author = await authors.GetById(command.AuthorId);

        if (author is null)
            return Result.NotFound("Author not found.");

        try
        {
            author.AddBook(command.Title);
        }
        catch (DomainException ex)
        {
            return Result.From(ex);
        }

        return await authors.SaveChanges() > 0
            ? Result.Ok("Book added successfully.")
            : Result.Invalid("Book could not be added.");
    }
}
