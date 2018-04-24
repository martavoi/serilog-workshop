using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workshop.Api.Models;
using Workshop.Data;
using User = Workshop.Data.User;

namespace Workshop.Api.Controllers
{
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly IUsersRepository _repository;

        public UsersController(IUsersRepository repository)
        {
            _repository = repository;
        }
    
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var users = await _repository.Get();
            return Ok(new GetUsersResponse
            {
                Users = users.Select(u => new Models.User
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName
        
                }).ToArray()});
        }
    
        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var user = await _repository.Get(id);
            return Ok(new User
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody]CreateUserRequest request)
        {
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Id = Guid.NewGuid()
            };
            await _repository.Add(user);
        
            return CreatedAtAction(nameof(Get), new {id = user.Id}, user);
        }
    }
}