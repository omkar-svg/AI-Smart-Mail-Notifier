using SmartMailNotifier.Services.Interfaces;
using SmartMailNotifier.DTOs;
using SmartMailNotifier.Models;
using SmartMailNotifier.Repository.Interfaces;

namespace SmartMailNotifier.Services
{
    public class EmailService : IEmailService
    {
        private readonly IEmailRepository _emailRepository;

        public EmailService(IEmailRepository emailRepository)
        {
            _emailRepository = emailRepository;
        }

        public async Task AddEmail(EmailDto emailDto, int userId)
        {
            var email = new Email
            {
                Subject = emailDto.Subject,
                Sender = emailDto.Sender,
                Summary = emailDto.Summary,
                UserId = userId,
                IsImportant = "No"
            };

            await _emailRepository.AddEmail(email);
        }

        public async Task<List<Email>> GetEmailsByUserId(int userId)
        {
            return await _emailRepository.GetEmailsByUserId(userId);
        }

        public async Task<Email?> GetEmailById(int emailId
            , int userId)
        {
            return await _emailRepository.GetEmailById(emailId, userId);

        }
      

        }
}