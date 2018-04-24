using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Workshop.Data
{
    public class UsersRepository : IUsersRepository
    {
        private readonly Context _ctx;

        public UsersRepository(Context ctx)
        {
            _ctx = ctx;
        }
    
        public Task<User> Get(Guid id)
        {
            return _ctx.Users.FindAsync(id);
        }

        public Task<User[]> Get()
        {
            return _ctx.Users.ToArrayAsync();
        }

        public Task Add(User u)
        {
            _ctx.Users.Add(u);
            return _ctx.SaveChangesAsync();
        }
    }
}