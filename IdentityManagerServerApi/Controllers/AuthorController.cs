using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuthorController(IAuthors authorsRepo) : ControllerBase
{
    [HttpGet]
    [Route("GetAuthors")]
    public async Task<ActionResult<List<AuthorDTO>>> GetAuthors()
    {
        List<AuthorDTO> authors = await authorsRepo.GetAuthors();
        return Ok(authors);
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
    public async Task<ActionResult<int>> AddAuthor(AuthorDTO author)
    {
        int result = await authorsRepo.AddAuthor(author);
        return Ok(result);
    }

    [HttpPut]
    [Route("UpdateAuthor/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<int>> UpdateAuthor(int id, AuthorDTO author)
    {
        int result = await authorsRepo.UpdateAuthor(id, author);

        if (result == 0)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete]
    [Route("DeleteAuthor/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GeneralResponse>> DeleteAuthor(int id)
    {
        GeneralResponse result = await authorsRepo.DeleteAuthor(id);
        return Ok(result);
    }
}
