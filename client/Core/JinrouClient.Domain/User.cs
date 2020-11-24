using System;
namespace JinrouClient.Domain
{
    public record User
    {
        public ulong Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }
}
