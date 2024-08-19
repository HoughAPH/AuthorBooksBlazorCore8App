using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Security.Claims;

namespace IdentityManagerServerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<AuthorDTO> GetAuthor(int id)
        {
            return await authorsRepo.GetAuthor(id);
        }

        [HttpPost]
        [Route("AddAuthor")]
        //        [Authorize(Policy = "AdminPolicy")]
        public async Task<int> AddAuthor(AuthorDTO author)
        {
            var response = await authorsRepo.AddAuthor(author);

            return response;
        }

        [HttpPut]
        [Route("UpdateAuthor/{id}")]
        [Authorize(Roles = "Admin")]
        //[Authorize(Policy = "AdminPolicy")]
        public async Task<int> UpdateAuthor(int id, AuthorDTO author)
        {
            var test = User.Claims;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var response = await authorsRepo.UpdateAuthor(id, author);

            //if(!response.IsError)
            //{
            //    return response.Value;
            //}
            //else
            //{
            //    return response.Errors.First();
            //}


            return response;
        }

        [HttpDelete]
        [Route("DeleteAuthor/{id}")]
        //[Authorize(Policy = "AdminPolicy")]
        [Authorize(Roles = "Admin")]
        public async Task<string> DeleteAuthor(int id)
        {
            return await authorsRepo.DeleteAuthor(id);
        }

    }
}
