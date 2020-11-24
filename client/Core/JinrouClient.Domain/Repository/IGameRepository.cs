using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JinrouClient.Domain.Repository
{
    public interface IGameRepository
    {
        Task<Game> CreateGameAsync(GameConfig config, string token);
        Task<Game> JoinAsync(string gameId, string token);
        Task<Game> LeaveAsync(string gameId, string token);
        Task VoteAsync(string gameId, ulong playerId, string token);
        Task KillAsync(string gameId, ulong playerId, string token);
        Task NextAsync(string gameId, string token);
        Task<IReadOnlyDictionary<ulong, Role>> GetRolesAsync(string gameId, string token);
        IObservable<StateChange> ObserveState(string gameId, string token);
    }
}
