using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using JinrouClient.Domain;
using JinrouClient.Domain.Repository;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;

namespace JinrouClient.Usecase
{
    public static class ObservableExtensions
    {
        public static IObservable<TSource> OnErrorRefreshRetry<TSource>(this IObservable<TSource> source,
            IAuthRepository authRepository, IUserRepository userRepository)
        {
            return source.RetryWhen(errors =>
                errors.WithLatestFrom(userRepository.CurrentUser, (error, user) => (error, user))
                    .SelectMany(async t =>
                    {
                        if (t.error is JinrouException jinrouException)
                        {
                            if (jinrouException.ErrorCode != ErrorCode.TokenExpired) throw t.error;
                            if (t.user is null) throw t.error;

                            (var token, var refreshToken) = await authRepository.RefreshAsync(t.user.RefreshToken);
                            var newUser = t.user with { Token = token, RefreshToken = refreshToken };
                            await userRepository.SetUserAsync(newUser);
                            return newUser;
                        }
                        throw t.error;
                    }));
        }
    }
}
