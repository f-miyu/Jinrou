package repository

type Transaction interface {
	GetRefreshTokenRepository() RefreshTokenRepository
	GetUserRepository() UserRepository
}
