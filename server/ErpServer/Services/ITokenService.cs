using ErpServer.Models;

namespace ErpServer.Services;

public interface ITokenService
{
    string GenerateToken(User user, string? roleName = null);
}
