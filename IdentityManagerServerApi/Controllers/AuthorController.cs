using IdentityManagerServerApi.Features.Authors.Commands;
using IdentityManagerServerApi.Features.Authors.Queries;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Models;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuthorController(
    IQueryHandler<GetAuthorsQuery, List<AuthorDTO>> getAuthors,
    IQueryHandler<GetAuthorQuery, AuthorDTO?> getAuthor,
    ICommandHandler<CreateAuthorCommand> createAuthor,
    ICommandHandler<RenameAuthorCommand> renameAuthor,
    ICommandHandler<DeleteAuthorCommand> deleteAuthor) : ControllerBase
{
    [HttpGet]
    [Route("GetAuthors")]
    public async Task<ActionResult<List<AuthorDTO>>> GetAuthors()
    {
        List<AuthorDTO> authors = await getAuthors.Handle(new GetAuthorsQuery());
        return Ok(authors);
    }

    [HttpGet]
    [Route("GetAuthor/{id}")]
    public async Task<ActionResult<AuthorDTO>> GetAuthor(int id)
    {
        AuthorDTO? author = await getAuthor.Handle(new GetAuthorQuery(id));

        if (author is null)
            return NotFound();

        return Ok(author);
    }

    [HttpPost]
    [Route("AddAuthor")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Result>> AddAuthor(AuthorDTO author)
    {
        Result result = await createAuthor.Handle(new CreateAuthorCommand(author.Name));
        return this.ToActionResult(result);
    }

    [HttpPut]
    [Route("UpdateAuthor/{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Result>> UpdateAuthor(int id, AuthorDTO author)
    {
        Result result = await renameAuthor.Handle(new RenameAuthorCommand(id, author.Name));
        return this.ToActionResult(result);
    }

    [HttpDelete]
    [Route("DeleteAuthor/{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Result>> DeleteAuthor(int id)
    {
        Result result = await deleteAuthor.Handle(new DeleteAuthorCommand(id));
        return this.ToActionResult(result);
    }
}
