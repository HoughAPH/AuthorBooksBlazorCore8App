using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BookController(IBooks booksRepo) : ControllerBase
{
    [HttpGet]
    [Route("GetBooks")]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooks()
    {
        IEnumerable<BookDTO> books = await booksRepo.GetBooks();
        return Ok(books);
    }

    [HttpGet]
    [Route("GetAuthorBooks/{id}")]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetAuthorBooks(int id)
    {
        IEnumerable<BookDTO> books = await booksRepo.GetAuthorBooks(id);
        return Ok(books);
    }

    [HttpGet]
    [Route("GetBook/{id}")]
    public async Task<ActionResult<BookDTO>> GetBook(int id)
    {
        BookDTO? book = await booksRepo.GetBook(id);

        if (book is null)
            return NotFound();

        return Ok(book);
    }

    [HttpPost]
    [Route("AddBook")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GeneralResponse>> AddBook(BookDTO book)
    {
        GeneralResponse result = await booksRepo.AddBook(book);

        if (!result.Flag)
            return BadRequest(result);

        return Ok(result);
    }

    [Route("UpdateBook/{id}")]
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GeneralResponse>> UpdateBook(int id, BookDTO book)
    {
        GeneralResponse result = await booksRepo.UpdateBook(id, book);

        if (!result.Flag && result.Message == "Book not found.")
            return NotFound(result);

        if (!result.Flag)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete]
    [Route("DeleteBook/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GeneralResponse>> DeleteBook(int id)
    {
        GeneralResponse result = await booksRepo.DeleteBook(id);

        if (!result.Flag)
            return NotFound(result);

        return Ok(result);
    }
}
