using Microsoft.EntityFrameworkCore;
using SmartMailNotifier.Models;

namespace SmartMailNotifier.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<GmailRefreshToken> GmailRefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GmailRefreshToken>()
                .HasIndex(g => new { g.UserId, g.GmailAddress })
                .IsUnique();
        }
    }
}