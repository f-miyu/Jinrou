using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using JinrouClient.Domain;
using JinrouClient.Usecase;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace JinrouClient.ViewModels
{
    public class ResultPageViewModel : ViewModelBase
    {
        public const string GameInfoParameterKey = "gameInfo";

        private readonly IGameUsecase _gameUsecase;

        private readonly ReactivePropertySlim<GameInfo> _gameInfo = new ReactivePropertySlim<GameInfo>();

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public ResultPageViewModel(INavigationService navigationService, IGameUsecase gameUsecase) : base(navigationService)
        {
            _gameUsecase = gameUsecase;

            IsWinner = Observable.CombineLatest(
                _gameUsecase.MyPlayer.Where(x => x is not null),
                _gameInfo.Where(x => x is not null),
                (player, info) => info.Winner == player!.Side)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_disposables);

            Side = _gameUsecase.MyPlayer.Where(x => x is not null)
                .Select(x => x!.Side).ToReadOnlyReactivePropertySlim()
                .AddTo(_disposables);
        }

        public IReadOnlyReactiveProperty<bool> IsWinner { get; }
        public IReadOnlyReactiveProperty<Side> Side { get; }

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
