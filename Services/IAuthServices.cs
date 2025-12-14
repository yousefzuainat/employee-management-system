using YousefZuaianatAPI.DTOs;
using YousefZuaianatAPI.Models;

namespace YousefZuaianatAPI.Services
{
    public interface IAuthServices
    {
        Task<AuthResponseDto> Register(CreateUserDto userDto);
        Task<AuthResponseDto> Login(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken);
    }
}