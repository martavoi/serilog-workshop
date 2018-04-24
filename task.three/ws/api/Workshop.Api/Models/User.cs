using System;

namespace Workshop.Api.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    
    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    
    public class GetUsersResponse
    {
        public User[] Users { get; set; }
    }
}