package repository

import "github.com/f-miyu/jinrou/server/app/domain"

type RefreshTokenRepository interface {
	TransactionRunnable
	Create(jti string, userID uint) (domain.RefreshToken, error)
	Delete(jti string) error
	FindByJti(jti string) (domain.RefreshToken, error)
}
