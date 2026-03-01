using SmartMailNotifier.DTOs;
using System.Threading.Tasks;

namespace SmartMailNotifier.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(LoginDto loginDto);
        Task<bool> RegisterAsync(RegisterDto registerDto);
    }
}
