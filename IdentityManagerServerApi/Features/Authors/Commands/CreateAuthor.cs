using IdentityManagerServerApi.Domain;
using IdentityManagerServerApi.Features.Common;
using IdentityManagerServerApi.Repositories;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Features.Authors.Commands;

public record CreateAuthorCommand(string Name);

public class CreateAuthorHandler(IAuthorRepository authors) : ICommandHandler<CreateAuthorCommand>
{
    public async Task<Result> Handle(CreateAuthorCommand command)
    {
        Author author;

        try
        {
            author = Author.Create(command.Name);
        }
        catch (DomainException ex)
        {
            return ex.ToResult();
        }

        authors.Add(author);

        return await authors.SaveChanges() > 0
            ? Result.Ok("Author added successfully.")
            : Result.Invalid("Author was not added.");
    }
}
