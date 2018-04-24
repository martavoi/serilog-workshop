using System;
using System.Threading.Tasks;

namespace Workshop.Data
{
    public interface IUsersRepository
    {
        Task<User> Get(Guid id);
        Task<User[]> Get();
        Task Add(User u);
    }
}