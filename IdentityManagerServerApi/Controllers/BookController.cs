using IdentityManagerServerApi.Features.Books.Commands;
using IdentityManagerServerApi.Features.Books.Queries;
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
public class BookController(
    IQueryHandler<GetBooksQuery, IEnumerable<BookDTO>> getBooks,
    IQueryHandler<GetAuthorBooksQuery, IEnumerable<BookDTO>> getAuthorBooks,
    IQueryHandler<GetBookQuery, BookDTO?> getBook,
    IQueryHandler<GetBooksPageQuery, PagedResult<BookDTO>> getBooksPage,
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

    /// <summary>One page of books ordered by title, optionally for one author and/or matching a search term.</summary>
    [HttpGet]
    [Route("GetBooksPage")]
    public async Task<ActionResult<PagedResult<BookDTO>>> GetBooksPage(
        int pageNumber = 1, int pageSize = Paging.DefaultPageSize, string? search = null, int? authorId = null) =>
        Ok(await getBooksPage.Handle(new GetBooksPageQuery(pageNumber, pageSize, search, authorId)));

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
    public async Task<ActionResult<Result>> AddBook(BookDTO book)
    {
        Result result = await addBook.Handle(new AddBookCommand(book.Title, book.AuthorId));
        return this.ToActionResult(result);
    }

    [Route("UpdateBook/{id}")]
    [HttpPut]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Result>> UpdateBook(int id, BookDTO book)
    {
        Result result = await updateBook.Handle(new UpdateBookCommand(id, book.Title, book.AuthorId));
        return this.ToActionResult(result);
    }

    [HttpDelete]
    [Route("DeleteBook/{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    public async Task<ActionResult<Result>> DeleteBook(int id)
    {
        Result result = await deleteBook.Handle(new DeleteBookCommand(id));
        return this.ToActionResult(result);
    }
}
