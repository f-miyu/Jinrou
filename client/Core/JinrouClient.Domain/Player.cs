using System;
namespace JinrouClient.Domain
{
    public record Player
    {
        public static Player Default { get; } = new();

        public ulong Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsDied { get; init; }
        public Role Role { get; init; } = Role.Unkown;
        public Side Side { get; init; } = Side.Neutral;
        public int Index { get; init; }
    }
}
