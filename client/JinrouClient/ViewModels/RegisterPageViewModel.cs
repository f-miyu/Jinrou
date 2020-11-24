using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Acr.UserDialogs;
using JinrouClient.Domain;
using JinrouClient.Usecase;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace JinrouClient.ViewModels
{
    public class RegisterPageViewModel : ViewModelBase
    {
        private readonly IUserUsecase _userUsecase;
        private readonly IUserDialogs _userDialogs;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly BusyNotifier _busyNotifier = new BusyNotifier();

        private readonly Subject<Unit> _registerRequested = new Subject<Unit>();

        public RegisterPageViewModel(INavigationService navigationService, IUserUsecase userUsecase, IUserDialogs userDialogs) : base(navigationService)
        {
            _userUsecase = userUsecase;
            _userDialogs = userDialogs;

            Name.SetValidateAttribute(() => Name);

            RegisterCommand = new[]
            {
                Name.ObserveHasErrors,
                _busyNotifier,
            }
            .CombineLatestValuesAreAllFalse()
            .ObserveOn(SynchronizationContext.Current)
            .ToReactiveCommand()
            .WithSubscribe(() =>
            {
                _registerRequested.OnNext(Unit.Default);
                _userUsecase.Register(Name.Value);
            })
            .AddTo(_disposables);

            _registerRequested.SelectMany(_ => Observable.Using(() =>
            {
                return new CompositeDisposable()
                {
                    _busyNotifier.ProcessStart(),
                    _userDialogs.Loading(""),
                };
            }, _ => Observable.Amb(
                _userUsecase.UserRegistered.Select<User, (User? user, Exception? error)>(user => (user, null)),
                _userUsecase.ErrorOccurred.Select<Exception, (User? user, Exception? error)>(ex => (null, ex)))
                .Take(1)))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(async t =>
            {
                if (t.user is not null)
                {
                    await _userDialogs.AlertAsync("登録が完了しました", "確認");
                    await navigationService.NavigateAsync("/MainPage");
                }
                else
                {
                    await _userDialogs.AlertAsync("登録に失敗しました", "エラー");
                }
            })
            .AddTo(_disposables);
        }

        [Required]
        public ReactiveProperty<string> Name { get; } = new ReactiveProperty<string>();

        public ReactiveCommand RegisterCommand { get; }

        public override void Destroy()
        {
            base.Destroy();

            _disposables.Dispose();
        }
    }
}
