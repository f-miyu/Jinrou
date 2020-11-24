using System;
using System.Collections.Generic;

namespace JinrouClient.Domain
{
    public record Game
    {
        public static Game Default { get; } = new();

        public string GameId { get; init; } = string.Empty;
        public GameConfig Config { get; init; } = GameConfig.Default;
        public Phase Phase { get; init; }
        public int Day { get; init; }
        public IReadOnlyDictionary<ulong, Player> Players { get; init; } = new Dictionary<ulong, Player>();
    }
}
