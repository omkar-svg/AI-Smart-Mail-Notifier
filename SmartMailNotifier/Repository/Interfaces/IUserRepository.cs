using SmartMailNotifier.Models;

namespace SmartMailNotifier.Repository.Interfaces
{
    public interface IUserRepository 
    {
        Task<User> GetUserByEmailAsync(string email);
        Task AddUserAsync(User user);

    }
}
