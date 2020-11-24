using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Acr.UserDialogs;
using JinrouClient.Domain;
using JinrouClient.Extensions;
using JinrouClient.Models;
using JinrouClient.Usecase;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace JinrouClient.ViewModels
{
    public class GamePageViewModel : ViewModelBase
    {
        public const string GameInfoParameterKey = "gameInfo";

        private readonly IGameUsecase _gameUsecase;
        private readonly IUserDialogs _userDialogs;

        private readonly ReactivePropertySlim<GameInfo> _gameInfo = new ReactivePropertySlim<GameInfo>();
        private readonly ReactivePropertySlim<IEnumerable<PlayerData>> _players = new ReactivePropertySlim<IEnumerable<PlayerData>>();
        private readonly ReactivePropertySlim<string> _title = new ReactivePropertySlim<string>();
        private readonly ReactivePropertySlim<string> _description = new ReactivePropertySlim<string>();
        private readonly ReactivePropertySlim<string> _actionName = new ReactivePropertySlim<string>();
        private readonly ReactivePropertySlim<bool> _isActionEnabled = new ReactivePropertySlim<bool>();
        private readonly ReactivePropertySlim<ActionType> _actionType = new ReactivePropertySlim<ActionType>();

        private readonly Subject<Unit> _voteRequested = new Subject<Unit>();
        private readonly Subject<Unit> _killRequested = new Subject<Unit>();
        private readonly Subject<Phase> _nextRequested = new Subject<Phase>();

        private readonly BusyNotifier _busyNotifier = new BusyNotifier();

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public GamePageViewModel(INavigationService navigationService, IGameUsecase gameUsecase, IUserDialogs userDialogs) : base(navigationService)
        {
            _gameUsecase = gameUsecase;
            _userDialogs = userDialogs;

            _gameUsecase.PhaseChanged
                .Subscribe(info => _gameInfo.Value = info)
                .AddTo(_disposables);

            Observable.CombineLatest(
                _gameUsecase.MyPlayer.Where(p => p is not null).Take(1),
                _gameInfo.Where(info => info is not null && info.Phase is Phase.Noon or Phase.Night),
                (myPlayer, info) => (myPlayer, info))
                .Subscribe(t =>
                {
                    var canSelect =
                    !(t.myPlayer!.IsDied ||
                    t.info.Phase == Phase.Start || t.info.Phase == Phase.End ||
                    (t.info.Phase == Phase.Night && t.info.Day == 1) ||
                    (t.info.Phase == Phase.Night && t.myPlayer!.Role != Role.Werewolf));

                    _players.Value = _gameUsecase.Players.Select(player =>
                    {
                        var actionType = ActionType.None;

                        if (_gameInfo.Value is not null)
                        {
                            actionType = _gameInfo.Value.Phase switch
                            {
                                Phase.Noon => ActionType.Vote,
                                Phase.Night => ActionType.Kill,
                                _ => ActionType.None,
                            };
                        }

                        var statusList = new List<string>();
                        if (t.myPlayer!.Id == player.Id)
                        {
                            statusList.Add("自分");
                        }
                        if (player.Role != Role.Unkown)
                        {
                            statusList.Add(player.Role.ToName());
                        }
                        if (player.IsDied)
                        {
                            statusList.Add("死亡");
                        }

                        var status = string.Join(" ", statusList);

                        return new PlayerData
                        {
                            Id = player.Id,
                            Name = player.Name,
                            Index = player.Index,
                            Role = player.Role,
                            Side = player.Side,
                            IsDied = player.IsDied,
                            IsMe = t.myPlayer!.Id == player.Id,
                            CanSelect = canSelect &&
                            t.myPlayer!.Id != player.Id &&
                            !player.IsDied,
                            ActionType = actionType,
                            Status = status
                        };
                    })
                    .ToArray();
                })
                .AddTo(_disposables);

            Observable.CombineLatest(
                _gameInfo.Where(info => info is not null && info.Phase == Phase.Night && info.Day == 1),
                _gameUsecase.MyPlayer.Where(player => player is not null && player.Role != Role.Unkown),
                (info, player) => (info, player))
                .Delay(TimeSpan.FromMilliseconds(500))
                .Subscribe(async t =>
                {
                    await _userDialogs.AlertAsync(
                        $"あなたは、{t.player!.Index}番 {t.player!.Role.ToName()}です",
                        "確認");
                })
                .AddTo(_disposables);

            _gameInfo.Where(info => info is not null && info.Phase == Phase.Night && info.Day > 1)
                .Subscribe(info =>
                {
                    if (info.KilledPlayer is null)
                    {
                        _userDialogs.Alert("誰も処刑されませんでした", "確認");
                    }
                    else
                    {
                        _userDialogs.Alert(
                            $"{info.KilledPlayer.Index}番の{info.KilledPlayer.Name}さんが処刑されました",
                            "確認");
                    }
                })
                .AddTo(_disposables);

            _gameInfo.Where(info => info is not null && info.Phase == Phase.Noon)
                .Subscribe(info =>
                {
                    if (info.KilledPlayer is null)
                    {
                        _userDialogs.Alert("誰も殺されませんでした", "確認");
                    }
                    else
                    {
                        _userDialogs.Alert(
                            $"{info.KilledPlayer.Index}番の{info.KilledPlayer.Name}さんが殺されました",
                            "確認");
                    }
                })
                .AddTo(_disposables);

            _gameInfo.Where(info => info is not null && info.Phase == Phase.End)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(info =>
                {
                    NavigationService.NavigateAsync("/ResultPage", (ResultPageViewModel.GameInfoParameterKey, info));
                })
                .AddTo(_disposables);

            _gameInfo.Where(info => info is not null && info.Phase is Phase.Noon or Phase.Night)
                .WithLatestFrom(_gameUsecase.MyPlayer, (info, myPlayer) => (info, myPlayer))
                .Subscribe(t =>
                {
                    _title.Value = $"第{t.info.Day}の" + (t.info.Phase == Phase.Noon ? "昼" : "夜");

                    if (t.myPlayer!.IsDied)
                    {
                        _description.Value = "確認ボタンを押して下さい";
                        _actionName.Value = "確認";
                        _isActionEnabled.Value = true;
                        _actionType.Value = ActionType.Confirm;
                    }
                    else
                    {
                        if (t.info.Phase == Phase.Noon)
                        {
                            _description.Value = "処刑するプレイヤーを選んで下さい";
                            _actionName.Value = "棄権する";
                            _isActionEnabled.Value = true;
                            _actionType.Value = ActionType.Abstention;
                        }
                        else if (t.myPlayer.Role == Role.Werewolf && t.info.Day > 1)
                        {
                            _description.Value = "殺すプレイヤーを選んで下さい";
                            _actionName.Value = "";
                            _isActionEnabled.Value = false;
                            _actionType.Value = ActionType.None;
                        }
                        else
                        {
                            _description.Value = "確認ボタンを押して下さい";
                            _actionName.Value = "確認";
                            _isActionEnabled.Value = true;
                            _actionType.Value = ActionType.Confirm;
                        }
                    }
                })
                .AddTo(_disposables);

            SelectCommand = _busyNotifier.Inverse()
                .ObserveOn(SynchronizationContext.Current)
                .ToReactiveCommand<PlayerData>()
                .WithSubscribe(data =>
               {
                   if (GameInfo.Value.Phase == Phase.Noon)
                   {
                       _voteRequested.OnNext(Unit.Default);
                       _gameUsecase.Vote(_gameInfo.Value, data);
                   }
                   else if (GameInfo.Value.Phase == Phase.Night)
                   {
                       _killRequested.OnNext(Unit.Default);
                       _gameUsecase.Kill(_gameInfo.Value, data);
                   }
               })
                .AddTo(_disposables);

            ActionCommand = _busyNotifier.Inverse()
                .ObserveOn(SynchronizationContext.Current)
                .ToReactiveCommand()
                .WithSubscribe(() =>
                {
                    if (_actionType.Value == ActionType.Confirm)
                    {
                        _nextRequested.OnNext(_gameInfo.Value.Phase);
                        _gameUsecase.Next(_gameInfo.Value);
                    }
                    else if (_actionType.Value == ActionType.Abstention)
                    {
                        _voteRequested.OnNext(Unit.Default);
                        _gameUsecase.Vote(_gameInfo.Value, null);
                    }
                })
                .AddTo(_disposables);

            _voteRequested.SelectMany(_ => Observable.Using(() =>
            {
                return new CompositeDisposable()
                {
                    _busyNotifier.ProcessStart(),
                    _userDialogs.Loading(""),
                };
            }, _ => Observable.Amb(
                    _gameUsecase.VoteResponsed.Select(_ => true),
                    _gameUsecase.ErrorOccurred.Select(_ => false))
                    .Take(1)
                    .SelectMany(b => b ?
                    _gameInfo.Where(info => info is not null && info.Phase is Phase.Night or Phase.End)
                            .Select<GameInfo, (GameInfo? info, bool success)>(info => (info, b)) :
                        Observable.Return<(GameInfo? info, bool success)>((null, b)))
                    .Take(1)))
                .Subscribe(t =>
                {
                    if (!t.success)
                    {
                        _userDialogs.Alert("投票に失敗しました", "エラー");
                    }
                })
                .AddTo(_disposables);

            _killRequested.SelectMany(_ => Observable.Using(() =>
            {
                return new CompositeDisposable()
                {
                    _busyNotifier.ProcessStart(),
                    _userDialogs.Loading(""),
                };
            }, _ => Observable.Amb(
                    _gameUsecase.KillResponsed.Select(_ => true),
                    _gameUsecase.ErrorOccurred.Select(_ => false))
                    .Take(1)
                    .SelectMany(b => b ?
                        _gameInfo.Where(info => info is not null && info.Phase is Phase.Noon or Phase.End)
                            .Select<GameInfo, (GameInfo? info, bool success)>(info => (info, b)) :
                        Observable.Return<(GameInfo? info, bool success)>((null, b)))
                    .Take(1)))
                .Subscribe(t =>
                {
                    if (!t.success)
                    {
                        _userDialogs.Alert("投票に失敗しました", "エラー");
                    }
                })
                .AddTo(_disposables);

            _nextRequested.SelectMany(phase => Observable.Using(() =>
            {
                return new CompositeDisposable()
                {
                    _busyNotifier.ProcessStart(),
                    _userDialogs.Loading(""),
                };
            }, _ => Observable.Amb(
                    _gameUsecase.NextResponsed.Select(_ => (phase, success: true)),
                    _gameUsecase.ErrorOccurred.Select(_ => (phase, success: false)))
                    .Take(1)
                    .SelectMany(t => t.success ?
                        _gameInfo.Where(info => info is not null && info.Phase != t.phase)
                            .Select<GameInfo, (GameInfo? info, bool success)>(info => (info, t.success)) :
                        Observable.Return<(GameInfo? info, bool success)>((null, t.success)))
                    .Take(1)))
                .Subscribe(t =>
                {
                    if (!t.success)
                    {
                        _userDialogs.Alert("エラーが発生しました", "エラー");
                    }
                })
                .AddTo(_disposables);
        }

        public ReactivePropertySlim<IEnumerable<PlayerData>> Players => _players;
        public IReadOnlyReactiveProperty<GameInfo> GameInfo => _gameInfo;
        public ReactiveCommand<PlayerData> SelectCommand { get; }
        public ReactiveCommand ActionCommand { get; }
        public IReadOnlyReactiveProperty<string> Title => _title;
        public IReadOnlyReactiveProperty<string> Description => _description;
        public IReadOnlyReactiveProperty<string> ActionName => _actionName;
        public IReadOnlyReactiveProperty<bool> IsActionEnabled => _isActionEnabled;

        public override void Initialize(INavigationParameters parameters)
        {
            base.Initialize(parameters);

            if (parameters.TryGetValue<GameInfo>(GameInfoParameterKey, out var gameInfo))
            {
                _gameInfo.Value = gameInfo;
            }
        }

        public override void Destroy()
        {
            base.Destroy();

            _disposables.Dispose();
        }
    }
}
