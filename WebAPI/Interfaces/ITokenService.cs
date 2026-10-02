namespace WebAPI.Interfaces;
using WebAPI.Models;
public interface ITokenService
{
    string GenerateToken(AppUser user);
}
