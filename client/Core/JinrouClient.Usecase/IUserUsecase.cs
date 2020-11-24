using System;
using JinrouClient.Domain;
using Reactive.Bindings;

namespace JinrouClient.Usecase
{
    public interface IUserUsecase
    {
        IReadOnlyReactiveProperty<bool?> UserExists { get; }
        IObservable<User> UserRegistered { get; }
        IObservable<Exception> ErrorOccurred { get; }
        void Register(string name);
    }
}
