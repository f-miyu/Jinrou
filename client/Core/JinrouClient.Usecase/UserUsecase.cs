using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JinrouClient.Domain;
using JinrouClient.Domain.Repository;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace JinrouClient.Usecase
{
    public class UserUsecase : IUserUsecase
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthRepository _authRepository;

        private readonly Subject<string> _registerRequested = new Subject<string>();
        private readonly Subject<User> _userRegistered = new Subject<User>();
        private readonly Subject<Exception> _errorOccurred = new Subject<Exception>();
        private readonly ReactivePropertySlim<bool?> _userExists = new ReactivePropertySlim<bool?>();

        public UserUsecase(IUserRepository userRepository, IAuthRepository authRepository)
        {
            _userRepository = userRepository;
            _authRepository = authRepository;

            _registerRequested.SelectMany(async name =>
                {
                    var user = await _authRepository.RegisterAsync(name);
                    await _userRepository.SetUserAsync(user);
                    return user;
                })
                .OnErrorRetry((Exception error) => _errorOccurred.OnNext(error))
                .Subscribe(user =>
                {
                    _userRegistered.OnNext(user);
                    _userExists.Value = true;
                });

            Observable.FromAsync(_ => _userRepository.GetUserAsync())
                .Subscribe(user => _userExists.Value = user is not null);
        }

        public IReadOnlyReactiveProperty<bool?> UserExists => _userExists;
        public IObservable<User> UserRegistered => _userRegistered;
        public IObservable<Exception> ErrorOccurred => _errorOccurred;

        public void Register(string name)
        {
            _registerRequested.OnNext(name);
        }
    }
}
