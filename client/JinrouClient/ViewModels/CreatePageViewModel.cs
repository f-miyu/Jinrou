using System;
using System.Reactive.Linq;
using JinrouClient.Domain;
using Prism.Navigation;
using Reactive.Bindings;

namespace JinrouClient.ViewModels
{
    public class CreatePageViewModel : ViewModelBase
    {
        public const string GameConfigParameterKey = "gameConfig";

        public CreatePageViewModel(INavigationService navigationService) : base(navigationService)
        {
            EnterCommand = Observable.CombineLatest(
                PlayerNum,
                WerewolfNum,
                (playerNum, werewolfNum) => (playerNum, werewolfNum))
                .Select(t => t.playerNum >= 3 && t.werewolfNum >= 1 && t.playerNum > 2 * t.werewolfNum)
                .ToAsyncReactiveCommand()
                .WithSubscribe(() =>
                {
                    var config = new GameConfig
                    {
                        PlayerNum = PlayerNum.Value,
                        WerewolfNum = WerewolfNum.Value
                    };
                    return NavigationService.GoBackAsync((GameConfigParameterKey, config));
                });
        }

        public ReactivePropertySlim<int> PlayerNum { get; } = new ReactivePropertySlim<int>(5);
        public ReactivePropertySlim<int> WerewolfNum { get; } = new ReactivePropertySlim<int>(1);
        public AsyncReactiveCommand EnterCommand { get; }
    }
}
