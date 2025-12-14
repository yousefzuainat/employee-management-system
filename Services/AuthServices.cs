using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YousefZuaianatAPI.Data;
using YousefZuaianatAPI.DTOs;
using YousefZuaianatAPI.Models;

namespace YousefZuaianatAPI.Services
{
    public class AuthServices : IAuthServices
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthServices(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AuthResponseDto> Register(CreateUserDto userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return new AuthResponseDto { Success = false, Message = "User already exists." };
            }

            var user = new User
            {
                Name = userDto.Name,
                Email = userDto.Email,
                Role = userDto.Role,
                CreatedAt = DateTime.UtcNow
            };

            var hashedPassword = new PasswordHasher<User>().HashPassword(user, userDto.Password);
            user.PasswordHash = hashedPassword;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new AuthResponseDto { Success = true, Message = "User registered successfully." };
        }

        public async Task<AuthResponseDto> Login(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "User not found." };
            }

            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, loginDto.Password) == PasswordVerificationResult.Failed)
            {
                return new AuthResponseDto { Success = false, Message = "Wrong password." };
            }

            // Create Token
            var token = CreateToken(user);

            // Create Refresh Token
            var refreshToken = GenerateRefreshToken();

            // Save Refresh Token to DB
            user.RefreshToken = refreshToken.Token;
            user.RefreshTokenExpiryTime = refreshToken.Expires;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresOn = refreshToken.Expires
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = GetPrincipalFromExpiredToken(token);
            if (principal == null)
            {
                return new AuthResponseDto { Success = false, Message = "Invalid Token." };
            }

            var name = principal.FindFirstValue(ClaimTypes.Name);

            // Fetch user who has this refresh token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new AuthResponseDto { Success = false, Message = "Invalid or expired refresh token." };
            }

            var newToken = CreateToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken.Token;
            user.RefreshTokenExpiryTime = newRefreshToken.Expires;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Success = true,
                Token = newToken,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresOn = newRefreshToken.Expires
            };
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("AppSettings:Token")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                issuer: _configuration.GetValue<string>("AppSettings:Issuer"),
                audience: _configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };
            return refreshToken;
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // You might want to validate this in production
                ValidateIssuer = false,   // You might want to validate this in production
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("AppSettings:Token")!)),
                ValidateLifetime = false // Inherently we are validating an expired token
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null; // Invalid algo
                }
                return principal;
            }
            catch
            {
                return null; // Invalid token structure or signature
            }
        }
    }
}
