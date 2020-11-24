package repository

import "github.com/f-miyu/jinrou/server/app/domain"

type GameRepository interface {
	Store(game *domain.Game)
	Delete(id string)
	Load(id string) (game *domain.Game, err error)
}
