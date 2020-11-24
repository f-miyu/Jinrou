using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using JinrouClient.Usecase;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace JinrouClient.ViewModels
{
    public class InitialPageViewModel : ViewModelBase
    {
        private readonly IUserUsecase _userUsecase;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();


        public InitialPageViewModel(INavigationService navigationService, IUserUsecase userUsecase) : base(navigationService)
        {
            _userUsecase = userUsecase;

            _userUsecase.UserExists.Where(x => x.HasValue)
                .Select(exists => exists!.Value)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(exists =>
                {
                    if (exists)
                    {
                        navigationService.NavigateAsync("/MainPage");
                    }
                    else
                    {
                        navigationService.NavigateAsync("/RegisterPage");
                    }
                }).AddTo(_disposables);
        }

        public override void Destroy()
        {
            base.Destroy();

            _disposables.Dispose();
        }
    }
}
