using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using System.Security.Cryptography;
using WebAPI.Interfaces;
namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(AppDb context, ITokenService tokenService) : ControllerBase
    {
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = context.AppUsers.ToList();
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

            var userDto = new UserDto
            {
                Email = user.Email,
                Token = tokenService.GenerateToken(user) // Replace with actual token generation logic
            };
            return Ok(userDto);
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

            return Ok(user);
        }
    }
}
