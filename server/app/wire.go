//+build wireinject

package main

import (
	"github.com/f-miyu/jinrou/server/app/data/repository"
	"github.com/f-miyu/jinrou/server/app/domain/service"
	"github.com/f-miyu/jinrou/server/app/infrastracture"
	"github.com/f-miyu/jinrou/server/app/usecase"
	"github.com/google/wire"
	"gorm.io/gorm"
)

func initializeJinrouServer(db *gorm.DB, tokenService service.TokenService) *infrastracture.JinrouServer {
	wire.Build(
		infrastracture.NewJinrouServer,
		usecase.NewGameUsecase,
		usecase.NewAuthUsecase,
		repository.NewRefreshTokenRepository,
		repository.NewUserRepository,
		repository.NewGameRepository,
	)
	return nil
}
