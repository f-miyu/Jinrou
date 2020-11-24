using System;
namespace JinrouClient.Domain
{
    public record GameConfig
    {
        public static GameConfig Default { get; } = new();

        public int PlayerNum { get; init; }
        public int WerewolfNum { get; init; }
    }
}
