using SmartMailNotifier.DTOs;
using SmartMailNotifier.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartMailNotifier.Services.Interfaces
{
    public interface IEmailService
    {
        Task AddEmail(EmailDto emailDto, int userId);
        Task<List<Email>> GetEmailsByUserId(int userId);
        Task<Email?> GetEmailById(int emailId, int userId);

       }
}
