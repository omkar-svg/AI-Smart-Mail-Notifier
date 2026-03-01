using SmartMailNotifier.Models;

namespace SmartMailNotifier.Repository.Interfaces
{
    public interface IEmailRepository
    {
        public Task AddEmail(Email email);

        public Task<List<Email>> GetEmailsByUserId(int userId);

        public Task<Email?> GetEmailById(int emailId, int userId);

        
    }
}
