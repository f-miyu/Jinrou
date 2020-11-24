using System;
using System.Threading.Tasks;
using Grpc.Core;
using Jinrou;
using JinrouClient.Domain;
using JinrouClient.Domain.Repository;

namespace JinrouClient.Data.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly Jinrou.Jinrou.JinrouClient _client;

        public AuthRepository(IJinrouClientProvider provider)
        {
            _client = provider.Client;
        }

        public async Task<User> RegisterAsync(string name)
        {
            try
            {
                var request = new RegisterRequest
                {
                    PlayerName = name
                };

                var response = await _client.RegisterAsync(request);

                return new User
                {
                    Id = response.PlayerId,
                    Name = name,
                    Token = response.Token,
                    RefreshToken = response.RefreshToken,
                };
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task<(string Token, string RefreshToken)> RefreshAsync(string refreshToken)
        {
            try
            {
                var request = new RefreshRequest
                {
                    RefreshToken = refreshToken
                };

                var response = await _client.RefreshAsync(request);

                return (response.Token, response.RefreshToken);
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }
    }
}
