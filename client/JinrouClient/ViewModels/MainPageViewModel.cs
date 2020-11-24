using Acr.UserDialogs;
using JinrouClient.Domain;
using JinrouClient.Domain.Repository;
using JinrouClient.Usecase;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;

namespace JinrouClient.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly IGameUsecase _gameUsecase;
        private readonly IUserDialogs _userDialogs;

        private readonly Subject<Unit> _createRequested = new Subject<Unit>();
        private readonly Subject<Unit> _joinRequested = new Subject<Unit>();
        private readonly Subject<GameInfo> _waitRequested = new Subject<GameInfo>();
        private readonly ReactivePropertySlim<GameInfo> _gameInfo = new ReactivePropertySlim<GameInfo>();

        private readonly BusyNotifier _busyNotifier = new BusyNotifier();

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public MainPageViewModel(INavigationService navigationService, IGameUsecase gameUsecase, IUserDialogs userDialogs) : base(navigationService)
        {
            _gameUsecase = gameUsecase;
            _userDialogs = userDialogs;

            CreateCommand = _busyNotifier
                .Inverse()
                .ObserveOn(SynchronizationContext.Current)
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => NavigationService.NavigateAsync("CreatePage"))
                .AddTo(_disposables);

            JoinCommand = _busyNotifier
                .Inverse()
                .ObserveOn(SynchronizationContext.Current)
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => NavigationService.NavigateAsync("JoinPage"))
                .AddTo(_disposables);

            _gameUsecase.PhaseChanged
                .Subscribe(info => _gameInfo.Value = info)
                .AddTo(_disposables);

            _createRequested.SelectMany(_ => Observable.Using(() =>
            {
                return new CompositeDisposable()
                {
                    _busyNotifier.ProcessStart(),
                    _userDialogs.Loading(""),
                };
            }, _ => Observable.Amb(
                    _gameUsecase.StartNewGameResponsed.Select(_ => true),
                    _gameUsecase.ErrorOccurred.Select(_ => false))
                    .Take(1)
                    .SelectMany(b => b ?
                        _gameInfo.Where(info => info is not null)
                                .Select<GameInfo, (GameInfo? info, bool success)>(info => (info, b)) :
                        Observable.Return<(GameInfo? info, bool success)>((null, b)))
                    .Take(1)))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(async t =>
                {
                    if (!t.success)
                    {
                        _userDialogs.Alert("ゲーム開始に失敗しました", "エラー");
                    }
                    else
                    {
                        await _userDialogs.AlertAsync($"ゲームID: {t.info!.GameId}", "確認");
                        _waitRequested.OnNext(t.info);
                    }
                })
                .AddTo(_disposables);

            _joinRequested.SelectMany(_ => Observable.Using(() =>
            {
                return new CompositeDisposable()
                {
                    _busyNotifier.ProcessStart(),
                    _userDialogs.Loading(""),
                };
            }, _ => Observable.Amb(
                    _gameUsecase.JoinResponsed.Select(_ => true),
                    _gameUsecase.ErrorOccurred.Select(_ => false))
                    .Take(1)
                    .SelectMany(b => b ?
                        _gameInfo.Where(info => info is not null && info.Phase == Phase.Night && info.Day == 1)
                                .Select<GameInfo, (GameInfo? info, bool success)>(info => (info, b)) :
                        Observable.Return<(GameInfo? info, bool success)>((null, b)))
                    .Take(1)))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(t =>
                {
                    if (!t.success)
                    {
                        _userDialogs.Alert("ゲーム参加に失敗しました", "エラー");
                    }
                    else
                    {
                        NavigationService.NavigateAsync("/GamePage",
                            (GamePageViewModel.GameInfoParameterKey, t.info));
                    }
                })
                .AddTo(_disposables);

            _waitRequested.SelectMany(info => Observable.Using(() =>
            {
                return new CompositeDisposable()
                {
                    _busyNotifier.ProcessStart(),
                    _userDialogs.Loading($"ゲームID: {info.GameId}"),
                };
            }, _ => _gameInfo.Where(info => info is not null && info.Phase == Phase.Night && info.Day == 1)
                    .Take(1)))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(info =>
                {
                    NavigationService.NavigateAsync("/GamePage",
                        (GamePageViewModel.GameInfoParameterKey, info));
                })
                .AddTo(_disposables);
        }

        public AsyncReactiveCommand CreateCommand { get; }
        public AsyncReactiveCommand JoinCommand { get; }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            if (parameters.GetNavigationMode() == NavigationMode.Back)
            {
                if (parameters.TryGetValue<GameConfig>(CreatePageViewModel.GameConfigParameterKey, out var config))
                {
                    _createRequested.OnNext(Unit.Default);
                    _gameUsecase.StartNewGame(config);
                }
                else if (parameters.TryGetValue<string>(JoinPageViewModel.GameIdParameterKey, out var gameId))
                {
                    _joinRequested.OnNext(Unit.Default);
                    _gameUsecase.Join(gameId);
                }
            }
        }

        public override void Destroy()
        {
            base.Destroy();

            _disposables.Dispose();
        }
    }
}
