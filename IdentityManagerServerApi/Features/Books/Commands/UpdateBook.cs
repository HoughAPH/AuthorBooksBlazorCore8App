using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Features.Common;
using IdentityManagerServerApi.Repositories;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Books.Commands;

public record UpdateBookCommand(int Id, string Title, int AuthorId);

public class UpdateBookHandler(IAuthorRepository authors) : ICommandHandler<UpdateBookCommand>
{
    public async Task<Result> Handle(UpdateBookCommand command)
    {
        Author? owner = await authors.GetByBookId(command.Id);

        if (owner is null)
            return Result.NotFound("Book not found.");

        try
        {
            if (owner.Id == command.AuthorId)
            {
                owner.UpdateBook(command.Id, command.Title);
            }
            else
            {
                Author? target = await authors.GetById(command.AuthorId);

                if (target is null)
                    return Result.NotFound("Author not found.");

                owner.TransferBook(command.Id, target, command.Title);
            }
        }
        catch (DomainException ex)
        {
            return ex.ToResult();
        }

        return await authors.SaveChanges() > 0
            ? Result.Ok("Book updated successfully.")
            : Result.Invalid("Book could not be updated.");
    }
}
