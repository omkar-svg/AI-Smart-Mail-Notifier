using Microsoft.AspNetCore.Identity;
using SmartMailNotifier.Data;
using SmartMailNotifier.Models;
using Microsoft.EntityFrameworkCore;
using SmartMailNotifier.Repository.Interfaces;


namespace SmartMailNotifier.Repository 
{
    public class EmailRepository : IEmailRepository
    { 
        public readonly AppDbContext _context;
        public EmailRepository(AppDbContext context) {
          _context = context;
        }

        public async Task AddEmail(Email email)

        {
            _context.Emails.Add(email);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Email>> GetEmailsByUserId(int userId)
        {
            return await _context.Emails.Where(e => e.UserId == userId).ToListAsync();
        }

       public async Task<Email?> GetEmailById(int emailId, int userId)
        {
            return await _context.Emails.FirstOrDefaultAsync(e => e.Id == emailId && e.UserId == userId);
        }
        public async Task<bool> ActivateEmail(string email)
        {
            var token = await _context.GmailRefreshTokens.FirstOrDefaultAsync(u => u.GmailAddress == email);
            if (token == null)
                return false;
             token.IsActive = !token.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

    }     
}
