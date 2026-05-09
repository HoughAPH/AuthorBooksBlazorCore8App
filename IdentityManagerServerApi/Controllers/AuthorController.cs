using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuthorController(IAuthors authorsRepo) : ControllerBase
{
    [HttpGet]
    [Route("GetAuthors")]
    public async Task<List<AuthorDTO>> GetAuthors()
    {
        return await authorsRepo.GetAuthors();
    }

    [HttpGet]
    [Route("GetAuthor/{id}")]
    public async Task<ActionResult<AuthorDTO>> GetAuthor(int id)
    {
        AuthorDTO? author = await authorsRepo.GetAuthor(id);

        if (author is null)
            return NotFound();

        return Ok(author);
    }

    [HttpPost]
    [Route("AddAuthor")]
    [Authorize(Roles = "Admin")]
    public async Task<int> AddAuthor(AuthorDTO author)
    {
        return await authorsRepo.AddAuthor(author);
    }

    [HttpPut]
    [Route("UpdateAuthor/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<int> UpdateAuthor(int id, AuthorDTO author)
    {
        return await authorsRepo.UpdateAuthor(id, author);
    }

    [HttpDelete]
    [Route("DeleteAuthor/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<string> DeleteAuthor(int id)
    {
        return await authorsRepo.DeleteAuthor(id);
    }
}
