using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Features.Common;
using IdentityManagerServerApi.Repositories;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Authors.Commands;

public record RenameAuthorCommand(int Id, string Name, Guid? ExpectedVersion = null);

public class RenameAuthorHandler(IAuthorRepository authors) : ICommandHandler<RenameAuthorCommand>
{
    public async Task<Result> Handle(RenameAuthorCommand command)
    {
        Author? author = await authors.GetById(command.Id);

        if (author is null)
            return Result.NotFound("Author not found.");

        try
        {
            author.EnsureVersion(command.ExpectedVersion);
            author.Rename(command.Name);
        }
        catch (DomainException ex)
        {
            return ex.ToResult();
        }

        return await authors.SaveChanges() > 0
            ? Result.Ok("Author updated successfully.")
            : Result.Invalid("Author was not updated.");
    }
}
