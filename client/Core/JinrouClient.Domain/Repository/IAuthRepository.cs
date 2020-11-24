using System;
using System.Threading.Tasks;

namespace JinrouClient.Domain.Repository
{
    public interface IAuthRepository
    {
        Task<User> RegisterAsync(string name);
        Task<(string Token, string RefreshToken)> RefreshAsync(string refreshToken);
    }
}
