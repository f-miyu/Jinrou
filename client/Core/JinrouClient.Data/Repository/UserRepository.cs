using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using JinrouClient.Domain;
using JinrouClient.Domain.Repository;
using Newtonsoft.Json;
using Reactive.Bindings;
using Xamarin.Essentials.Interfaces;

namespace JinrouClient.Data.Repository
{
    public class UserRepository : IUserRepository
    {
        private const string key = "user";

        private readonly ISecureStorage _storage;

        private readonly ReactivePropertySlim<User?> _currentUser = new ReactivePropertySlim<User?>();
        public IReadOnlyReactiveProperty<User?> CurrentUser => _currentUser;

        public UserRepository(ISecureStorage storage)
        {
            _storage = storage;

            Observable.FromAsync(_ => GetUserAsync())
                .Subscribe(user => _currentUser.Value = user);
        }

        public async Task<User?> GetUserAsync()
        {
            var value = await _storage.GetAsync(key);

            if (value is null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<User>(value);
        }

        public async Task SetUserAsync(User user)
        {
            var value = JsonConvert.SerializeObject(user);
            await _storage.SetAsync(key, value);
            _currentUser.Value = user;
        }

        public bool RemoveUser(User user)
        {
            var removed = _storage.Remove(key);
            if (removed)
            {
                _currentUser.Value = null;
            }
            return removed;
        }
    }
}
