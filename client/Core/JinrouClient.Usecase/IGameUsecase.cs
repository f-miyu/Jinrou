using System;
using System.Collections.Generic;
using System.Reactive;
using JinrouClient.Domain;
using Reactive.Bindings;

namespace JinrouClient.Usecase
{
    public interface IGameUsecase
    {
        IObservable<GameInfo> PhaseChanged { get; }
        IReadOnlyList<Player> Players { get; }
        IReadOnlyReactiveProperty<Player?> MyPlayer { get; }
        IObservable<Game> StartNewGameResponsed { get; }
        IObservable<Game> JoinResponsed { get; }
        IObservable<Unit> VoteResponsed { get; }
        IObservable<Unit> KillResponsed { get; }
        IObservable<Unit> NextResponsed { get; }
        IObservable<Exception> ErrorOccurred { get; }
        void StartNewGame(GameConfig config);
        void Join(string gameId);
        void Vote(GameInfo iofo, Player? player);
        void Kill(GameInfo iofo, Player? player);
        void Next(GameInfo iofo);
    }
}
