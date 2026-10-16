using IdentityManagerServerApi.Features.Books.Commands;
using IdentityManagerServerApi.Features.Books.Queries;
using IdentityManagerServerApi.Features.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SharedClassLibrary.DTOs;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace IdentityManagerServerApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BookController(
    IQueryHandler<GetBooksQuery, IEnumerable<BookDTO>> getBooks,
    IQueryHandler<GetAuthorBooksQuery, IEnumerable<BookDTO>> getAuthorBooks,
    IQueryHandler<GetBookQuery, BookDTO?> getBook,
    ICommandHandler<AddBookCommand> addBook,
    ICommandHandler<UpdateBookCommand> updateBook,
    ICommandHandler<DeleteBookCommand> deleteBook) : ControllerBase
{
    [HttpGet]
    [Route("GetBooks")]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooks()
    {
        IEnumerable<BookDTO> books = await getBooks.Handle(new GetBooksQuery());
        return Ok(books);
    }

    [HttpGet]
    [Route("GetAuthorBooks/{id}")]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetAuthorBooks(int id)
    {
        IEnumerable<BookDTO> books = await getAuthorBooks.Handle(new GetAuthorBooksQuery(id));
        return Ok(books);
    }

    [HttpGet]
    [Route("GetBook/{id}")]
    public async Task<ActionResult<BookDTO>> GetBook(int id)
    {
        BookDTO? book = await getBook.Handle(new GetBookQuery(id));

        if (book is null)
            return NotFound();

        return Ok(book);
    }

    [HttpPost]
    [Route("AddBook")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<GeneralResponse>> AddBook(BookDTO book)
    {
        Result result = await addBook.Handle(new AddBookCommand(book.Title, book.AuthorId));
        return this.ToActionResult(result);
    }

    [Route("UpdateBook/{id}")]
    [HttpPut]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<GeneralResponse>> UpdateBook(int id, BookDTO book)
    {
        Result result = await updateBook.Handle(new UpdateBookCommand(id, book.Title, book.AuthorId));
        return this.ToActionResult(result);
    }

    [HttpDelete]
    [Route("DeleteBook/{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<GeneralResponse>> DeleteBook(int id)
    {
        Result result = await deleteBook.Handle(new DeleteBookCommand(id));
        return this.ToActionResult(result);
    }
}
