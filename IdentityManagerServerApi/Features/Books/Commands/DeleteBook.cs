using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Features.Common;
using IdentityManagerServerApi.Repositories;

namespace IdentityManagerServerApi.Features.Books.Commands;

public record DeleteBookCommand(int Id);

public class DeleteBookHandler(IAuthorRepository authors) : ICommandHandler<DeleteBookCommand>
{
    public async Task<Result> Handle(DeleteBookCommand command)
    {
        Author? owner = await authors.GetByBookId(command.Id);

        if (owner is null)
            return Result.NotFound("Book not found.");

        owner.RemoveBook(command.Id);

        if (await authors.SaveChanges() <= 0)
            throw new InvalidOperationException("Book could not be deleted.");

        return Result.Ok("Book deleted successfully.");
    }
}
