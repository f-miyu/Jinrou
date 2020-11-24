package usecase

import (
	"github.com/f-miyu/jinrou/server/app/domain"
	"github.com/f-miyu/jinrou/server/app/domain/repository"
)

type GameUsecase interface {
	CreateGame(playerID uint, config domain.Config) (state domain.State, err error)
	Join(gameID string, playerID uint) (state domain.State, err error)
	Leave(gameID string, playerID uint) (state domain.State, err error)
	Vote(gameID string, playerID uint, targetID uint) (err error)
	Kill(gameID string, playerID uint, targetID uint) (err error)
	Next(gameID string, playerID uint) (err error)
	GetRoles(gameID string, playerID uint) (roles map[uint]domain.Role, err error)
	ObserveState(gameID string, playerID uint) (ch <-chan domain.StateChange, err error)
	UnobserveState(gameID string, playerID uint) (err error)
}

type gameUsecase struct {
	gameRepository repository.GameRepository
	userRepository repository.UserRepository
}

func NewGameUsecase(gameRepository repository.GameRepository, userRepository repository.UserRepository) GameUsecase {
	return &gameUsecase{
		gameRepository: gameRepository,
		userRepository: userRepository,
	}
}

func (usecase *gameUsecase) CreateGame(playerID uint, config domain.Config) (state domain.State, err error) {
	user, err := usecase.userRepository.FindByID(playerID)
	if err != nil {
		return
	}

	game, err := domain.NewGame(config)
	if err != nil {
		return
	}

	state, err = game.Join(playerID, user.Name)
	if err != nil {
		return
	}

	usecase.gameRepository.Store(game)

	return
}

func (usecase *gameUsecase) Join(gameID string, playerID uint) (state domain.State, err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	user, err := usecase.userRepository.FindByID(playerID)
	if err != nil {
		return
	}

	oldPhase := game.Phase

	state, err = game.Join(playerID, user.Name)
	if err != nil {
		return
	}

	playerJoined := domain.StateChange{
		State:         state,
		ChangeType:    domain.PlayerJoined,
		OldPhase:      oldPhase,
		AddedPlayerID: playerID,
	}

	game.NotifyStateChanged(playerJoined)

	if state.Phase != oldPhase {
		phaseChanged := domain.StateChange{
			State:      state,
			ChangeType: domain.PhaseChanged,
			OldPhase:   oldPhase,
		}

		game.NotifyStateChanged(phaseChanged)
	}

	return
}

func (usecase *gameUsecase) Leave(gameID string, playerID uint) (state domain.State, err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	oldPhase := game.Phase

	state, err = game.Leave(playerID)
	if err != nil {
		return
	}

	stateChanged := domain.StateChange{
		State:        state,
		ChangeType:   domain.PlayerLeft,
		OldPhase:     oldPhase,
		LeftPlayerID: playerID,
	}

	game.NotifyStateChanged(stateChanged)

	return
}

func (usecase *gameUsecase) Vote(gameID string, playerID uint, targetID uint) (err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	oldPhase := game.Phase

	state, phaseResult, err := game.Vote(playerID, targetID)

	if err != nil {
		return
	}

	usecase.notifyStateChangedIfNeeded(game, state, phaseResult, oldPhase)
	usecase.deleteIfNeeded(game, state)

	return
}

func (usecase *gameUsecase) Kill(gameID string, playerID uint, targetID uint) (err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	oldPhase := game.Phase

	state, phaseResult, err := game.Kill(playerID, targetID)

	if err != nil {
		return
	}

	usecase.notifyStateChangedIfNeeded(game, state, phaseResult, oldPhase)
	usecase.deleteIfNeeded(game, state)

	return
}

func (usecase *gameUsecase) Next(gameID string, playerID uint) (err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	oldPhase := game.Phase

	state, phaseResult, err := game.Next(playerID)

	if err != nil {
		return
	}

	usecase.notifyStateChangedIfNeeded(game, state, phaseResult, oldPhase)
	usecase.deleteIfNeeded(game, state)

	return
}

func (usecase *gameUsecase) GetRoles(gameID string, playerID uint) (roles map[uint]domain.Role, err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	roles, err = game.GetRoles(playerID)
	if err != nil {
		return
	}

	return
}

func (usecase *gameUsecase) ObserveState(gameID string, playerID uint) (ch <-chan domain.StateChange, err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	ch = game.ObserveState(playerID)

	return
}

func (usecase *gameUsecase) UnobserveState(gameID string, playerID uint) (err error) {
	game, err := usecase.gameRepository.Load(gameID)
	if err != nil {
		return
	}

	game.UnobserveState(playerID)

	return
}

func (usecase *gameUsecase) notifyStateChangedIfNeeded(game *domain.Game, state domain.State, phaseResult domain.PhaseResult, oldPhase domain.Phase) {
	if state.Phase != oldPhase {
		stateChange := domain.StateChange{
			State:    state,
			OldPhase: oldPhase,
		}

		if phaseResult.Winner != domain.Neutral {
			stateChange.ChangeType = domain.GameOver
			stateChange.Winner = phaseResult.Winner
		} else if phaseResult.KilledPlayerID > 0 {
			stateChange.ChangeType = domain.PhaseChanged
			stateChange.KilledPlayerID = phaseResult.KilledPlayerID
		} else {
			stateChange.ChangeType = domain.PhaseChangedWithoutKilling
		}

		game.NotifyStateChanged(stateChange)
	}
}

func (usecase *gameUsecase) deleteIfNeeded(game *domain.Game, state domain.State) {
	if state.Phase == domain.End {
		game.Dispose()
		usecase.gameRepository.Delete(game.ID)
	}
}
