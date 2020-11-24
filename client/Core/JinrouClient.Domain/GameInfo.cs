using System;
namespace JinrouClient.Domain
{
    public record GameInfo
    {
        public static GameInfo Default { get; } = new();

        public string GameId { get; init; } = string.Empty;
        public GameConfig Config { get; init; } = GameConfig.Default;
        public Phase Phase { get; init; }
        public int Day { get; init; }
        public Player? KilledPlayer { get; init; }
        public Side? Winner { get; init; }
    }
}
