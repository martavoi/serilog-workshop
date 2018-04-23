using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Workshop.Api.Models;
using Workshop.Data;
using User = Workshop.Data.User;

namespace Workshop.Api.Controllers
{
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly IUsersRepository _repository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUsersRepository repository, ILogger<UsersController> logger)
        {
            _repository = repository;
            _logger = logger;
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
            var user = new Data.User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Id = Guid.NewGuid()
            };
            await _repository.Add(user);
            _logger.LogInformation("{@user} has been created", user);
    
            return CreatedAtAction(nameof(Get), new {id = user.Id}, user);
        }
    }
}