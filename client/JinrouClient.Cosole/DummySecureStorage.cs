using System;
using System.Threading.Tasks;
using Xamarin.Essentials.Interfaces;

namespace JinrouClient.Cosole
{
    public class DummySecureStorage : ISecureStorage
    {
        public Task<string> GetAsync(string key)
        {
            return Task.FromResult<string>(null);
        }

        public bool Remove(string key)
        {
            return false;
        }

        public void RemoveAll()
        {
        }

        public Task SetAsync(string key, string value)
        {
            return Task.CompletedTask;
        }
    }
}
