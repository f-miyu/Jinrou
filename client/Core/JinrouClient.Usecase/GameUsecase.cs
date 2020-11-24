using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using JinrouClient.Domain;
using JinrouClient.Domain.Repository;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace JinrouClient.Usecase
{
    public class GameUsecase : IGameUsecase
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuthRepository _authRepository;

        private readonly ReactivePropertySlim<Player?> _myPlayer = new ReactivePropertySlim<Player?>();
        private readonly List<Player> _players = new List<Player>();
        private readonly Subject<GameInfo> _phaseChanged = new Subject<GameInfo>();
        private readonly Subject<GameConfig> _startNewGameRequested = new Subject<GameConfig>();
        private readonly Subject<string> _joinRequested = new Subject<string>();
        private readonly Subject<(GameInfo Info, ulong PlayerId)> _voteRequested = new Subject<(GameInfo Info, ulong PlayerId)>();
        private readonly Subject<(GameInfo Info, ulong PlayerId)> _killRequested = new Subject<(GameInfo Info, ulong PlayerId)>();
        private readonly Subject<GameInfo> _nextRequested = new Subject<GameInfo>();
        private readonly Subject<(GameInfo Info, IReadOnlyDictionary<ulong, Player> Players)> _startRequested = new Subject<(GameInfo Info, IReadOnlyDictionary<ulong, Player> Players)>();
        private readonly Subject<Game> _startNewGameResponsed = new Subject<Game>();
        private readonly Subject<Game> _joinResponsed = new Subject<Game>();
        private readonly Subject<Unit> _voteResponsed = new Subject<Unit>();
        private readonly Subject<Unit> _killResponsed = new Subject<Unit>();
        private readonly Subject<Unit> _nextResponsed = new Subject<Unit>();
        private readonly Subject<Exception> _errorOccurred = new Subject<Exception>();
        private readonly Subject<Game> _observeStateRequested = new Subject<Game>();

        public GameUsecase(IGameRepository gameRepository, IUserRepository userRepository,
            IAuthRepository authRepository)
        {
            _gameRepository = gameRepository;
            _userRepository = userRepository;
            _authRepository = authRepository;

            _startNewGameRequested.SelectMany(config =>
                Observable.Return(config)
                    .WithLatestFrom(_userRepository.CurrentUser, (config, user) => (config, user))
                    .Where(t => t.user is not null)
                    .SelectMany(t => _gameRepository.CreateGameAsync(t.config, t.user!.Token))
                    .OnErrorRefreshRetry(_authRepository, _userRepository))
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Subscribe(game =>
                {
                    _startNewGameResponsed.OnNext(game);
                    _observeStateRequested.OnNext(game);
                });

            _joinRequested.SelectMany(gameId =>
                Observable.Return(gameId)
                    .WithLatestFrom(_userRepository.CurrentUser, (gameId, user) => (gameId, user))
                    .Where(t => t.user is not null)
                    .SelectMany(t => _gameRepository.JoinAsync(t.gameId, t.user!.Token))
                    .OnErrorRefreshRetry(_authRepository, _userRepository))
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Subscribe(game =>
                {
                    _joinResponsed.OnNext(game);
                    _observeStateRequested.OnNext(game);
                });

            _observeStateRequested
                .Select(game => (StateChange)new StartStateChange
                {
                    Game = game,
                    OldPhase = game.Phase,
                    Players = game.Players
                })
                .Select(state => Observable.Return(state)
                    .WithLatestFrom(_userRepository.CurrentUser, (state, user) => (state, user))
                    .Where(t => t.user is not null)
                    .SelectMany(t => _gameRepository.ObserveState(t.state.Game.GameId, t.user!.Token))
                    .StartWith(state)
                    .OnErrorRefreshRetry(_authRepository, _userRepository))
                .Switch()
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Repeat()
                .Subscribe(state => UpdateState(state));

            _voteRequested.SelectMany(t =>
                Observable.Return(t)
                    .WithLatestFrom(_userRepository.CurrentUser, (t, user) => (info: t.Info, playerId: t.PlayerId, user))
                    .Where(t => t.user is not null)
                    .SelectMany(t => _gameRepository.VoteAsync(t.info.GameId, t.playerId, t.user!.Token)
                        .ToObservable())
                    .OnErrorRefreshRetry(_authRepository, _userRepository))
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Subscribe(_ => _voteResponsed.OnNext(Unit.Default));

            _killRequested.SelectMany(t =>
                Observable.Return(t)
                    .WithLatestFrom(_userRepository.CurrentUser, (t, user) => (info: t.Info, playerId: t.PlayerId, user))
                    .Where(t => t.user is not null)
                    .SelectMany(t => _gameRepository.KillAsync(t.info.GameId, t.playerId, t.user!.Token)
                        .ToObservable())
                    .OnErrorRefreshRetry(_authRepository, _userRepository))
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Subscribe(_ => _killResponsed.OnNext(Unit.Default));

            _nextRequested.SelectMany(info =>
                Observable.Return(info)
                    .WithLatestFrom(_userRepository.CurrentUser, (info, user) => (info, user))
                    .Where(t => t.user is not null)
                    .SelectMany(t => _gameRepository.NextAsync(t.info.GameId, t.user!.Token)
                        .ToObservable())
                    .OnErrorRefreshRetry(_authRepository, _userRepository))
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Subscribe(_ => _nextResponsed.OnNext(Unit.Default));

            _startRequested.SelectMany(data =>
                Observable.Return(data.Info)
                    .WithLatestFrom(_userRepository.CurrentUser, (info, user) => (info, user))
                    .Where(t => t.user is not null)
                    .SelectMany(async t =>
                    {
                        var roles = await _gameRepository.GetRolesAsync(t.info.GameId, t.user!.Token);
                        var players = data.Players.Select(x =>
                        {
                            if (roles.TryGetValue(x.Key, out var role))
                            {
                                var player = x.Value with { Role = role, Side = role.ToSide() };
                                return player;
                            }
                            return x.Value;
                        }).OrderBy(p => p.Index);

                        return (info: data.Info, players);
                    })
                    .OnErrorRefreshRetry(_authRepository, _userRepository))
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Subscribe(t =>
                {
                    var myId = _userRepository.CurrentUser.Value?.Id;
                    _myPlayer.Value = t.players.FirstOrDefault(p => p.Id == myId);

                    _players.Clear();
                    _players.AddRange(t.players);

                    _phaseChanged.OnNext(t.info);
                });
        }

        public IObservable<GameInfo> PhaseChanged => _phaseChanged;
        public IReadOnlyList<Player> Players => _players;
        public IReadOnlyReactiveProperty<Player?> MyPlayer => _myPlayer;
        public IObservable<Game> StartNewGameResponsed => _startNewGameResponsed;
        public IObservable<Game> JoinResponsed => _joinResponsed;
        public IObservable<Unit> VoteResponsed => _voteResponsed;
        public IObservable<Unit> KillResponsed => _killResponsed;
        public IObservable<Unit> NextResponsed => _nextResponsed;
        public IObservable<Exception> ErrorOccurred => _errorOccurred;

        public void StartNewGame(GameConfig config)
        {
            _players.Clear();
            _myPlayer.Value = null;

            _startNewGameRequested.OnNext(config);
        }

        public void Join(string gameId)
        {
            _players.Clear();
            _myPlayer.Value = null;

            _joinRequested.OnNext(gameId);
        }

        public void Vote(GameInfo iofo, Player? player)
        {
            _voteRequested.OnNext((iofo, player?.Id ?? 0));
        }

        public void Kill(GameInfo iofo, Player? player)
        {
            _killRequested.OnNext((iofo, player?.Id ?? 0));
        }

        public void Next(GameInfo iofo)
        {
            _nextRequested.OnNext(iofo);
        }

        private void UpdateState(StateChange state)
        {
            Player? killedPlayer = null;
            Side? winner = null;
            bool shouldNotifyChanged = true;

            switch (state)
            {
                case StartStateChange startStateChange:
                    {
                        _players.AddRange(startStateChange.Players.Select(x => x.Value));
                        var user = _userRepository.CurrentUser.Value;
                        if (user is not null && state.Game.Players.TryGetValue(user.Id, out var player))
                        {
                            _myPlayer.Value = player;
                        }
                    }
                    break;
                case PlayerJoinedStateChange playerJoinedStateChange:
                    {
                        if (state.Game.Players.TryGetValue(playerJoinedStateChange.PlayerId, out var player))
                        {
                            if (_players.FirstOrDefault(p => p.Id == player.Id) is null)
                            {
                                _players.Add(player);
                            }
                        }
                        shouldNotifyChanged = false;
                    }
                    break;
                case PlayerLeftStateChange playerLeftStateChange:
                    {
                        var player = _players.FirstOrDefault(p => p.Id == playerLeftStateChange.PlayerId);
                        if (player is not null)
                        {
                            _players.Remove(player);
                        }
                        shouldNotifyChanged = false;
                    }
                    break;
                case PlayerKilledStateChange playerKilledStateChange:
                    {
                        if (playerKilledStateChange.Game.Players.TryGetValue(playerKilledStateChange.PlayerId, out var player))
                        {
                            var oldPlayer = _players.FirstOrDefault(p => p.Id == player.Id);
                            if (oldPlayer is not null)
                            {
                                var index = _players.IndexOf(oldPlayer);
                                _players[index] = oldPlayer with { IsDied = true };
                            }

                            if (_myPlayer.Value is not null && _myPlayer.Value.Id == playerKilledStateChange.PlayerId)
                            {
                                _myPlayer.Value = _myPlayer.Value with { IsDied = true };
                            }

                            killedPlayer = player;
                        }
                    }
                    break;
                case GameOverStateChange gameOverStateChange:
                    if (_myPlayer.Value is not null)
                    {
                        winner = gameOverStateChange.Winner;
                    }
                    break;
            }

            if (shouldNotifyChanged)
            {
                var gmaeInfo = new GameInfo
                {
                    GameId = state.Game.GameId,
                    Config = state.Game.Config,
                    Phase = state.Game.Phase,
                    Day = state.Game.Day,
                    KilledPlayer = killedPlayer,
                    Winner = winner
                };

                if (state.Game.Phase == Phase.Night && state.Game.Day == 1)
                {
                    _startRequested.OnNext((gmaeInfo, state.Game.Players));
                }
                else
                {
                    _phaseChanged.OnNext(gmaeInfo);
                }
            }
        }
    }
}
