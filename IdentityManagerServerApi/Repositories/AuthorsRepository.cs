using IdentityManagerServerApi.Data;
using Microsoft.EntityFrameworkCore;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;

namespace IdentityManagerServerApi.Repositories
{
    public class AuthorsRepository : IAuthors
    {
        private readonly AppDbContext _dbContext;
        public AuthorsRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AuthorDTO> GetAuthor(int id)
        {
            var author = await _dbContext.Authors.FindAsync(id);



            if (author == null)
                return null;
            AuthorDTO authorDTO = new AuthorDTO
            {
                Id = author.Id,
                Name = author.Name
            };

            return authorDTO;


        }

        public async Task<List<AuthorDTO>> GetAuthors()
        {
            //throw new KeyNotFoundException("Authors not found");
            var authors = await _dbContext.Authors
                .Select(a => new AuthorDTO
                {
                    Id = a.Id,
                    Name = a.Name
                })
                .ToListAsync();
            //throw new KeyNotFoundException("Author not found");
            return authors;

        }

        public async Task<int> AddAuthor(AuthorDTO author)
        {
            var newAuthor = new Author
            {
                Name = author.Name
            };
            _dbContext.Authors.Add(newAuthor);
            var response = await _dbContext.SaveChangesAsync();

            return response;

        }

        public async Task<int> UpdateAuthor(int id, AuthorDTO author)
        {
            var existingAuthor = await _dbContext.Authors.FindAsync(id);

            if (existingAuthor == null)
                return await Task.FromResult(0); //Error.NotFound("Author not found");

            existingAuthor.Name = author.Name;

            _dbContext.Authors.Update(existingAuthor);
            return await _dbContext.SaveChangesAsync();

           
        }

        public async Task<string> DeleteAuthor(int id)
        {
            var author = await _dbContext.Authors.FindAsync(id);

            if (await _dbContext.Books.AnyAsync(b => b.AuthorId == id))
            {
                return await Task.FromResult("Author has books"); //"Author has books, cannot delete";
                //return false;
            }
            else
            {
                _dbContext.Authors.Remove(author);
                var response = await _dbContext.SaveChangesAsync();
                return response > 0 ? await Task.FromResult("") : await Task.FromResult("Author not deleted");
            }


        }

    }
}
