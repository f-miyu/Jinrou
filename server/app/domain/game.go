package domain

import (
	"crypto/rand"
	"errors"
	"fmt"
	"math/big"
	math_rand "math/rand"
	"sort"
	"sync"
	"time"

	"github.com/ahmetb/go-linq/v3"
)

type Game struct {
	ID            string
	Config        Config
	Phase         Phase
	Day           int
	players       map[uint]*Player
	stateChangeds sync.Map
	votings       map[uint]uint
	nextRequests  map[uint]bool
	mu            sync.RWMutex
}

func NewGame(config Config) (game *Game, err error) {
	if config.PlayerNum < 3 || config.WerewolfNum < 1 ||
		2*config.WerewolfNum >= config.PlayerNum {
		return nil, errors.New("invalid argument")
	}

	id, err := generateGameID()
	if err != nil {
		return
	}

	game = &Game{
		ID:           id,
		Config:       config,
		Phase:        Start,
		Day:          1,
		players:      make(map[uint]*Player),
		votings:      make(map[uint]uint),
		nextRequests: make(map[uint]bool),
	}

	return
}

func (game *Game) Join(playerID uint, playerName string) (state State, err error) {
	game.mu.Lock()
	defer game.mu.Unlock()

	if game.Phase != Start {
		err = errors.New("invalid phase")
		return
	}

	player := &Player{
		ID:         playerID,
		Name:       playerName,
		Role:       Unkown,
		Side:       Neutral,
		IsDied:     false,
		JoinedTime: time.Now(),
	}

	if _, ok := game.players[playerID]; ok {
		err = errors.New("already joined")
		return
	}

	game.players[playerID] = player

	game.update()

	state = game.snapshot()

	return
}

func (game *Game) Leave(playerID uint) (state State, err error) {
	game.mu.Lock()
	defer game.mu.Unlock()

	if game.Phase != Start {
		err = errors.New("invalid phase")
		return
	}

	if _, ok := game.players[playerID]; !ok {
		err = errors.New("not joined")
		return
	}

	state = game.snapshot()

	return
}

func (game *Game) setRoles() {
	players := make([]*Player, len(game.players))
	i := 0
	for _, p := range game.players {
		players[i] = p
		i++
	}

	sort.Slice(players, func(i, j int) bool {
		return players[i].JoinedTime.Before(players[j].JoinedTime)
	})

	for i := 0; i < len(players); i++ {
		players[i].Index = i + 1
	}

	for i := 0; i < game.Config.WerewolfNum; i++ {
		r := math_rand.Intn(len(players))
		players[r].Role = Werewolf
		players[r].Side = Werewolves
		players = append(players[:r], players[r+1:]...)
	}

	for _, player := range players {
		player.Role = Villager
		player.Side = Villagers
	}
}

func (game *Game) Vote(playerID uint, targetID uint) (state State, pahseResult PhaseResult, err error) {
	game.mu.Lock()
	defer game.mu.Unlock()

	player, err := game.getPlayer(playerID)
	if err != nil {
		return
	}

	if player.IsDied {
		err = errors.New("player is already died")
	}

	if targetID > 0 {
		var target *Player
		target, err = game.getPlayer(targetID)
		if err != nil {
			return
		}

		if target.IsDied {
			err = errors.New("target player is already died")
			return
		}
	}

	if game.Phase != Noon {
		err = errors.New("invalid phase")
		return
	}

	if playerID == targetID {
		err = errors.New("cannot vote myself")
		return
	}

	if _, ok := game.votings[playerID]; ok {
		err = errors.New("already voted")
		return
	}

	game.votings[playerID] = targetID
	game.nextRequests[playerID] = true

	pahseResult = game.update()

	state = game.snapshot()

	return
}

func (game *Game) Kill(playerID uint, targetID uint) (state State, pahseResult PhaseResult, err error) {
	game.mu.Lock()
	defer game.mu.Unlock()

	player, err := game.getPlayer(playerID)
	if err != nil {
		return
	}

	if player.Role != Werewolf {
		err = errors.New("player is not werewolf")
	}

	target, err := game.getPlayer(targetID)
	if err != nil {
		return
	}

	if target.IsDied {
		err = errors.New("target player is already died")
		return
	}

	if game.Phase != Night || game.Day == 1 {
		err = errors.New("invalid phase")
		return
	}

	if playerID == targetID {
		err = errors.New("cannot kill myself")
		return
	}

	if _, ok := game.votings[playerID]; ok {
		err = errors.New("already voted")
		return
	}

	game.votings[playerID] = targetID
	game.nextRequests[playerID] = true

	pahseResult = game.update()

	state = game.snapshot()

	return
}

func (game *Game) Next(playerID uint) (state State, pahseResult PhaseResult, err error) {
	game.mu.Lock()
	defer game.mu.Unlock()

	_, err = game.getPlayer(playerID)
	if err != nil {
		return
	}

	if game.Phase != Night && game.Phase != Noon {
		err = errors.New("invalid phase")
		return
	}

	if _, ok := game.nextRequests[playerID]; ok {
		err = errors.New("already requested")
		return
	}

	game.nextRequests[playerID] = true

	pahseResult = game.update()

	state = game.snapshot()

	return
}

func (game *Game) GetRoles(playerID uint) (roles map[uint]Role, err error) {
	game.mu.RLock()
	defer game.mu.RUnlock()

	player, err := game.getPlayer(playerID)
	if err != nil {
		return
	}

	if game.Phase == Start {
		err = errors.New("invalid phase")
		return
	}

	roles = map[uint]Role{player.ID: player.Role}

	if player.Role == Werewolf {
		for k, p := range game.players {
			if p.Role == Werewolf {
				roles[k] = p.Role
			}
		}
	}

	return
}

func (game *Game) update() (phaseResult PhaseResult) {
	phase := game.Phase
	day := game.Day

	switch {
	case phase == Start:
		if len(game.players) == game.Config.PlayerNum {
			game.Phase = Night
			game.Day = 1

			game.setRoles()
		}
	case phase == Night && day == 1:
		if len(game.nextRequests) == game.Config.PlayerNum {
			game.Phase = Noon
			game.nextRequests = make(map[uint]bool)
		}
	case phase == Night:
		if len(game.votings) == game.Config.WerewolfNum &&
			len(game.nextRequests) == game.Config.PlayerNum {
			result := game.getVotingResult()

			var targetID uint
			if len(result.targetIDs) > 1 {
				r := math_rand.Intn(len(result.targetIDs))
				targetID = result.targetIDs[r]
			} else {
				targetID = result.targetIDs[0]
			}

			if killedPlayer, err := game.getPlayer(targetID); err == nil {
				killedPlayer.IsDied = true
			}

			winner := game.judge()

			if winner != Neutral {
				game.Phase = End
				phaseResult = PhaseResult{Winner: winner}
			} else {
				game.Phase = Noon
				phaseResult = PhaseResult{KilledPlayerID: targetID}
			}

			game.votings = make(map[uint]uint)
			game.nextRequests = make(map[uint]bool)
		}
	case phase == Noon:
		if len(game.votings) == game.getAlivePlayerNum() &&
			len(game.nextRequests) == game.Config.PlayerNum {
			result := game.getVotingResult()

			if len(result.targetIDs) == 1 && result.targetIDs[0] > 0 {
				if killedPlayer, err := game.getPlayer(result.targetIDs[0]); err == nil {
					killedPlayer.IsDied = true
				}

				winner := game.judge()

				if winner != Neutral {
					game.Phase = End
					phaseResult = PhaseResult{Winner: winner}
				} else {
					game.Phase = Night
					game.Day++
					phaseResult = PhaseResult{KilledPlayerID: result.targetIDs[0]}
				}
			} else {
				game.Phase = Night
				game.Day++
			}

			game.votings = make(map[uint]uint)
			game.nextRequests = make(map[uint]bool)
		}
	}

	return
}

func (game *Game) getVotingResult() VotingResult {
	return linq.From(game.votings).Select(func(i interface{}) interface{} {
		return i.(linq.KeyValue).Value
	}).GroupBy(func(i interface{}) interface{} {
		return i
	}, func(i interface{}) interface{} {
		return i
	}).AggregateWithSeed(VotingResult{targetIDs: []uint{}}, func(r, c interface{}) interface{} {
		result := r.(VotingResult)
		group := c.(linq.Group)
		number := len(group.Group)

		if number > result.number {
			result.targetIDs = []uint{group.Key.(uint)}
			result.number = number
		} else if number == result.number {
			result.targetIDs = append(result.targetIDs, group.Key.(uint))
		}
		return result
	}).(VotingResult)
}

func (game *Game) judge() (winner Side) {
	villagerNum := linq.From(game.players).CountWith(func(i interface{}) bool {
		player := i.(linq.KeyValue).Value.(*Player)
		return player.Side == Villagers && !player.IsDied
	})

	werewolNum := linq.From(game.players).CountWith(func(i interface{}) bool {
		player := i.(linq.KeyValue).Value.(*Player)
		return player.Side == Werewolves && !player.IsDied
	})

	winner = Neutral
	if werewolNum == 0 {
		winner = Villagers
	} else if villagerNum <= werewolNum {
		winner = Werewolves
	}
	return
}

func (game *Game) GetPlayer(playerID uint) (player Player, err error) {
	game.mu.RLock()
	defer game.mu.RUnlock()
	pplayer, err := game.getPlayer(playerID)
	return *pplayer, err
}

func (game *Game) getPlayer(playerID uint) (player *Player, err error) {
	player, ok := game.players[playerID]
	if !ok {
		err = errors.New("player not found")
		return
	}
	return
}

func (game *Game) ObserveState(playerID uint) <-chan StateChange {
	actual, _ := game.stateChangeds.LoadOrStore(playerID, make(chan StateChange))
	return actual.(chan StateChange)
}

func (game *Game) UnobserveState(playerID uint) {
	value, loaded := game.stateChangeds.LoadAndDelete(playerID)
	if loaded {
		close(value.(chan StateChange))
	}
}

func (game *Game) NotifyStateChanged(stateChange StateChange) {
	game.stateChangeds.Range(func(key, value interface{}) bool {
		stateChanged := value.(chan StateChange)
		stateChanged <- stateChange
		return true
	})
}

func (game *Game) Snapshot() State {
	game.mu.RLock()
	defer game.mu.RUnlock()
	return game.snapshot()
}

func (game *Game) snapshot() State {
	players := make(map[uint]Player)
	for k, p := range game.players {
		players[k] = *p
	}
	return State{
		ID:      game.ID,
		Config:  game.Config,
		Phase:   game.Phase,
		Day:     game.Day,
		Players: players,
	}
}

func (game *Game) Dispose() {
	game.stateChangeds.Range(func(key, value interface{}) bool {
		close(value.(chan StateChange))
		return true
	})
}

func (game *Game) getAlivePlayerNum() (num int) {
	for _, player := range game.players {
		if !player.IsDied {
			num++
		}
	}
	return
}

func generateGameID() (id string, err error) {
	n, err := rand.Int(rand.Reader, big.NewInt(999999))
	if err != nil {
		return
	}
	id = fmt.Sprintf("%06d", n)
	return
}
