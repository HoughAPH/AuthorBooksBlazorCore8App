using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Features.Common;
using IdentityManagerServerApi.Repositories;

namespace IdentityManagerServerApi.Features.Authors.Commands;

public record DeleteAuthorCommand(int Id);

public class DeleteAuthorHandler(IAuthorRepository authors) : ICommandHandler<DeleteAuthorCommand>
{
    public async Task<Result> Handle(DeleteAuthorCommand command)
    {
        Author? author = await authors.GetById(command.Id);

        if (author is null)
            return Result.NotFound("Author not found.");

        try
        {
            author.EnsureCanBeDeleted();
        }
        catch (DomainException ex)
        {
            return Result.From(ex);
        }

        authors.Remove(author);

        return await authors.SaveChanges() > 0
            ? Result.Ok("Author deleted successfully.")
            : Result.Invalid("Author was not deleted.");
    }
}
