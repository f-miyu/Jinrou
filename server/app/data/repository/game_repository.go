package repository

import (
	"errors"
	"sync"

	"github.com/f-miyu/jinrou/server/app/domain"
	"github.com/f-miyu/jinrou/server/app/domain/repository"
)

type gameRepository struct {
	games sync.Map
}

func NewGameRepository() repository.GameRepository {
	return &gameRepository{}
}

func (reposiotry *gameRepository) Store(game *domain.Game) {
	reposiotry.games.Store(game.ID, game)
}

func (reposiotry *gameRepository) Delete(id string) {
	reposiotry.games.Delete(id)
}

func (reposiotry *gameRepository) Load(id string) (game *domain.Game, err error) {
	val, ok := reposiotry.games.Load(id)
	if !ok {
		err = errors.New("not found")
		return
	}

	game = val.(*domain.Game)

	return
}
