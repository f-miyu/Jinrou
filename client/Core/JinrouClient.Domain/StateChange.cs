using System;
using System.Collections.Generic;

namespace JinrouClient.Domain
{
    public record StateChange
    {
        public Game Game { get; init; } = Game.Default;
        public Phase OldPhase { get; init; }
    }

    public record PlayerJoinedStateChange : StateChange
    {
        public ulong PlayerId { get; init; }
    }

    public record PlayerLeftStateChange : StateChange
    {
        public ulong PlayerId { get; init; }
    }

    public record PlayerKilledStateChange : StateChange
    {
        public ulong PlayerId { get; init; }
    }

    public record GameOverStateChange : StateChange
    {
        public Side Winner { get; init; }
    }

    public record StartStateChange : StateChange
    {
        public IReadOnlyDictionary<ulong, Player> Players { get; init; } = new Dictionary<ulong, Player>();
    }
}
