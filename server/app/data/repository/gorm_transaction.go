package repository

import (
	"github.com/f-miyu/jinrou/server/app/domain/repository"
)

type gormTransaction struct {
	refreshTokenRepository repository.RefreshTokenRepository
	userRepository         repository.UserRepository
}

func (transaction *gormTransaction) GetRefreshTokenRepository() repository.RefreshTokenRepository {
	return transaction.refreshTokenRepository
}

func (transaction *gormTransaction) GetUserRepository() repository.UserRepository {
	return transaction.userRepository
}
