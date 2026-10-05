using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using WebAPI.Interfaces;
namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(AppDb context, ITokenService tokenService) : ControllerBase
    {
        [HttpGet("users")]
        [Authorize]
        public IActionResult GetUsers()
        {
            var users = context.AppUsers.Select(u => ToAccountDto(u)).ToList();
            return Ok(users);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto loginDto)
        {
            var user = context.AppUsers.FirstOrDefault(u => u.Email == loginDto.Email);
            if (user == null)
            {
                return BadRequest("Invalid username or password.");
            }

            if (!PasswordCheck(user.PasswordHash, user.PasswordSalt, loginDto.Password))
            {
                return BadRequest("Invalid username or password.");
            }

            return Ok(ToUserDto(user));
        }

        private static bool PasswordCheck(byte[] storedHash, byte[] storedSalt, string password)
        {
            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i])
                {
                    return false;
                }
            }
            return true;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto registerDto)
        {
            var user = context.AppUsers.FirstOrDefault(u => u.Email == registerDto.Email);
            if (user != null)
            {
                return BadRequest("User with this email already exists.");
            }

            using var hmac = new HMACSHA512();
            user = new AppUser
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            context.AppUsers.Add(user);
            context.SaveChanges();

            return Ok(ToUserDto(user));
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMe()
        {
            var user = CurrentUser();
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(ToAccountDto(user));
        }

        [HttpPut("me")]
        [Authorize]
        public IActionResult UpdateMe(UpdateAccountDto updateDto)
        {
            var user = CurrentUser();
            if (user == null)
            {
                return Unauthorized();
            }

            user.FirstName = updateDto.FirstName.Trim();
            user.LastName = updateDto.LastName.Trim();
            context.SaveChanges();

            return Ok(ToAccountDto(user));
        }

        [HttpPut("password")]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordDto passwordDto)
        {
            var user = CurrentUser();
            if (user == null)
            {
                return Unauthorized();
            }

            if (!PasswordCheck(user.PasswordHash, user.PasswordSalt, passwordDto.CurrentPassword))
            {
                return BadRequest("Current password is incorrect.");
            }

            using var hmac = new HMACSHA512();
            user.PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(passwordDto.NewPassword));
            user.PasswordSalt = hmac.Key;
            context.SaveChanges();

            return NoContent();
        }

        private AppUser? CurrentUser()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var userId) ? context.AppUsers.Find(userId) : null;
        }

        private UserDto ToUserDto(AppUser user) => new()
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = tokenService.GenerateToken(user)
        };

        private static AccountDto ToAccountDto(AppUser user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
