namespace ProjectManager.Application.Interfaces;
using ProjectManager.Domain.Entities;
public interface ITokenService
{
    string GenerateToken(AppUser user);
}
