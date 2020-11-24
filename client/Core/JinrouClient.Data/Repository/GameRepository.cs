using System;
using Grpc.Core;
using Jinrou;
using System.Linq;
using JinrouClient.Domain.Repository;
using System.Threading.Tasks;
using JinrouClient.Domain;
using System.Collections.Generic;

namespace JinrouClient.Data.Repository
{
    public class GameRepository : IGameRepository
    {
        private readonly Jinrou.Jinrou.JinrouClient _client;

        public GameRepository(IJinrouClientProvider provider)
        {
            _client = provider.Client;
        }

        public async Task<Game> CreateGameAsync(GameConfig config, string token)
        {
            try
            {
                var request = new CreateGameRequest
                {
                    Config = new Jinrou.Config
                    {
                        PlayerNum = config.PlayerNum,
                        WerewolfNum = config.WerewolfNum
                    }
                };

                var response = await _client.CreateGameAsync(request, CreateHeader(token));

                return ConvertState(response.State);
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Game> JoinAsync(string gameId, string token)
        {
            try
            {
                var request = new JoinRequest
                {
                    GameId = gameId
                };

                var response = await _client.JoinAsync(request, CreateHeader(token));

                return ConvertState(response.State);
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Game> LeaveAsync(string gameId, string token)
        {
            try
            {
                var request = new LeaveRequest
                {
                    GameId = gameId
                };

                var response = await _client.LeaveAsync(request, CreateHeader(token));

                return ConvertState(response.State);
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task VoteAsync(string gameId, ulong playerId, string token)
        {
            try
            {
                var request = new VoteRequest
                {
                    GameId = gameId,
                    PlayerId = playerId
                };

                await _client.VoteAsync(request, CreateHeader(token));
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task KillAsync(string gameId, ulong playerId, string token)
        {
            try
            {
                var request = new KillRequest
                {
                    GameId = gameId,
                    PlayerId = playerId
                };

                await _client.KillAsync(request, CreateHeader(token));
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task NextAsync(string gameId, string token)
        {
            try
            {
                var request = new NextRequest
                {
                    GameId = gameId,
                };

                await _client.NextAsync(request, CreateHeader(token));
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task<IReadOnlyDictionary<ulong, Domain.Role>> GetRolesAsync(string gameId, string token)
        {
            try
            {
                var request = new GetRolesRequest
                {
                    GameId = gameId
                };

                var response = await _client.GetRolesAsync(request, CreateHeader(token));

                return response.Roles.ToDictionary(x => x.Key, x => (Domain.Role)x.Value);
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        public IObservable<StateChange> ObserveState(string gameId, string token)
        {
            try
            {
                var request = new ObserveStateRequest
                {
                    GameId = gameId
                };

                return _client.ObserveState(request, CreateHeader(token)).ResponseStream
                    .ReadAllAsync()
                    .Select(response =>
                    {
                        var game = ConvertState(response.State);
                        var oldPhase = (Domain.Phase)response.OldPhase;

                        return response.ChangeType switch
                        {
                            ChangeType.PlayerJoined => new PlayerJoinedStateChange
                            {
                                Game = game,
                                OldPhase = oldPhase,
                                PlayerId = response.AddedPlayerId
                            },
                            ChangeType.PlayerLeft => new PlayerLeftStateChange
                            {
                                Game = game,
                                OldPhase = oldPhase,
                                PlayerId = response.LeftPlayerId
                            },
                            ChangeType.PhaseChanged => new PlayerKilledStateChange
                            {
                                Game = game,
                                OldPhase = oldPhase,
                                PlayerId = response.KilledPlayerId
                            },
                            ChangeType.GameOver => new GameOverStateChange
                            {
                                Game = game,
                                OldPhase = oldPhase,
                                Winner = (Domain.Side)response.Winner
                            },
                            _ => new StateChange
                            {
                                Game = game,
                                OldPhase = oldPhase
                            }
                        };
                    })
                    .ToObservable();
            }
            catch (RpcException ex)
            {
                throw JinrouExceptionMapper.Transform(ex);
            }
            catch
            {
                throw;
            }
        }

        private Metadata CreateHeader(string token)
        {
            return new Metadata
            {
                {"Authorization", $"Bearer {token}" }
            };
        }

        private Game ConvertState(State state)
        {
            return new Game
            {
                GameId = state.GameId,
                Config = new GameConfig
                {
                    PlayerNum = state.Config.PlayerNum,
                    WerewolfNum = state.Config.WerewolfNum
                },
                Phase = (Domain.Phase)state.Phase,
                Day = state.Day,
                Players = state.Players
                    .Select(x => (key: x.Key, value: new Domain.Player
                    {
                        Id = x.Value.PlayerId,
                        Name = x.Value.PlayerName,
                        IsDied = x.Value.IsDied,
                        Index = x.Value.Index,
                    })).ToDictionary(x => x.key, x => x.value),
            };
        }
    }
}
