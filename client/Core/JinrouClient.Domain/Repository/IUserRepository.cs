using System;
using System.Threading.Tasks;
using Reactive.Bindings;

namespace JinrouClient.Domain.Repository
{
    public interface IUserRepository
    {
        IReadOnlyReactiveProperty<User?> CurrentUser { get; }
        Task<User?> GetUserAsync();
        Task SetUserAsync(User user);
        bool RemoveUser(User user);
    }
}
