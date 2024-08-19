using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IdentityManagerServerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //    [Authorize(Roles = "Admin")]
    public class BookController(IBooks booksRepo) : ControllerBase
    {
        // GET: api/<BookController>
        [HttpGet]
        [Route("GetBooks")]
        public async Task<IEnumerable<BookDTO>> GetBooks()
        {
            return await booksRepo.GetBooks();
        }

        // GET api/<BookController>/5
        [HttpGet]
        [Route("GetAuthorBooks/{id}")]
        public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
        {
            return await booksRepo.GetAuthorBooks(id);
        }
        [HttpGet]
        [Route("GetBook/{id}")]
        public async Task<BookDTO> GetBook(int id)
        {
            return await booksRepo.GetBook(id);
        }

        //// POST api/<BookController>
        [HttpPost]
        [Route("AddBook")]
        [Authorize(Roles = "Admin")]
        //[Authorize(Policy = "AdminPolicy")]
        //[Authorize(Policy = "AuthorPolicy")]
        public async Task<int> AddBook(BookDTO book)
        {
            return await booksRepo.AddBook(book);

        }

        //// PUT api/<BookController>/5
        [Route("UpdateBook/{id}")]
        [HttpPut]
        [Authorize(Roles = "Admin")]
        //[Authorize(Policy = "AdminPolicy")]
        //[Authorize(Policy = "AuthorPolicy")]
        public async Task<int> UpdateBook(int id, BookDTO book)
        {
            return await booksRepo.UpdateBook(id, book);
        }

        // DELETE api/<BookController>/5
        [HttpDelete]
        [Route("DeleteBook/{id}")]
        [Authorize(Roles = "Admin")]
        //[Authorize(Policy = "AdminPolicy")]
        //[Authorize(Policy = "AuthorPolicy")]
        public async Task<bool> DeleteBook(int id)
        {
            return await booksRepo.DeleteBook(id);
        }
    }
}
