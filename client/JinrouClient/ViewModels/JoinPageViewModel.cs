using System;
using System.ComponentModel.DataAnnotations;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace JinrouClient.ViewModels
{
    public class JoinPageViewModel : ViewModelBase
    {
        public const string GameIdParameterKey = "gameId";

        public JoinPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            GameId.SetValidateAttribute(() => GameId);

            EnterCommand = GameId.ObserveHasErrors.Inverse()
                .ToAsyncReactiveCommand()
                .WithSubscribe(() => NavigationService.GoBackAsync((GameIdParameterKey, GameId.Value)));
        }

        [Required]
        [RegularExpression(@"[0-9]{6}")]
        public ReactiveProperty<string> GameId { get; } = new ReactiveProperty<string>();

        public AsyncReactiveCommand EnterCommand { get; }
    }
}
