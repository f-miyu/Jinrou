package repository

import (
	"github.com/f-miyu/jinrou/server/app/data/entity"
	"github.com/f-miyu/jinrou/server/app/domain"
	"github.com/f-miyu/jinrou/server/app/domain/repository"
	"gorm.io/gorm"
)

type refreshTokenRepository struct {
	gormRepository
}

func NewRefreshTokenRepository(db *gorm.DB) repository.RefreshTokenRepository {
	return &refreshTokenRepository{gormRepository: gormRepository{db: db}}
}

func (repository *refreshTokenRepository) Create(jti string, userID uint) (refreshToken domain.RefreshToken, err error) {
	entity := entity.RefreshTokenEntity{Jti: jti, UserID: userID}
	if err = repository.db.Create(&entity).Error; err != nil {
		return
	}
	refreshToken = repository.convertFrom(entity)
	return
}

func (repository *refreshTokenRepository) Delete(jti string) (err error) {
	return repository.db.Delete(&entity.RefreshTokenEntity{Jti: jti}).Error
}

func (repository *refreshTokenRepository) FindByJti(jti string) (refreshToken domain.RefreshToken, err error) {
	result := entity.RefreshTokenEntity{}
	if err = repository.db.Where(&entity.RefreshTokenEntity{Jti: jti}).First(&result).Error; err != nil {
		return
	}
	refreshToken = repository.convertFrom(result)
	return
}

func (repository *refreshTokenRepository) convertFrom(entity entity.RefreshTokenEntity) domain.RefreshToken {
	return domain.RefreshToken{
		Jti:    entity.Jti,
		UserID: entity.UserID,
	}
}
