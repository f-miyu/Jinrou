package repository

import (
	"github.com/f-miyu/jinrou/server/app/domain/repository"
	"gorm.io/gorm"
)

type gormRepository struct {
	db *gorm.DB
}

func (repository *gormRepository) RunTransaction(fc func(transaction repository.Transaction) error) (err error) {
	transaction := repository.db.Begin()
	defer func() {
		if err != nil {
			transaction.Rollback()
		} else {
			transaction.Commit()
		}
	}()

	gormTransaction := &gormTransaction{
		refreshTokenRepository: NewRefreshTokenRepository(transaction),
		userRepository:         NewUserRepository(transaction),
	}
	err = fc(gormTransaction)
	return
}
