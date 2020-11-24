package usecase

import (
	"time"

	"github.com/f-miyu/jinrou/server/app/domain"
	"github.com/f-miyu/jinrou/server/app/domain/repository"
	"github.com/f-miyu/jinrou/server/app/domain/service"
)

const tokenExpiredDuration = time.Hour

type AuthUsecase interface {
	Register(userName string) (user domain.User, token domain.Token, refreshToken domain.Token, err error)
	VerifyToken(tokenString string) (token domain.Token, err error)
	RefreshTokens(refreshTokenString string) (token domain.Token, refreshToken domain.Token, err error)
}

type authUsecase struct {
	tokenService           service.TokenService
	refreshTokenRepository repository.RefreshTokenRepository
	userRepository         repository.UserRepository
}

func NewAuthUsecase(tokenService service.TokenService,
	refreshTokenRepository repository.RefreshTokenRepository,
	userRepository repository.UserRepository) AuthUsecase {
	return &authUsecase{
		tokenService:           tokenService,
		refreshTokenRepository: refreshTokenRepository,
		userRepository:         userRepository,
	}
}

func (usecase *authUsecase) Register(userName string) (user domain.User, token domain.Token, refreshToken domain.Token, err error) {
	err = usecase.userRepository.RunTransaction(func(tx repository.Transaction) (err error) {
		user, err = tx.GetUserRepository().Create(userName)
		if err != nil {
			return
		}

		token, refreshToken, err = usecase.tokenService.IssueTokens(user.ID, tokenExpiredDuration)
		if err != nil {
			return
		}

		_, err = tx.GetRefreshTokenRepository().Create(refreshToken.Jti, user.ID)

		return
	})

	return
}

func (usecase *authUsecase) VerifyToken(tokenString string) (token domain.Token, err error) {
	return usecase.tokenService.VerifyToken(tokenString)
}

func (usecase *authUsecase) RefreshTokens(refreshTokenString string) (token domain.Token, refreshToken domain.Token, err error) {
	tokenData, err := usecase.VerifyToken(refreshTokenString)
	if err != nil {
		return
	}

	err = usecase.refreshTokenRepository.RunTransaction(func(tx repository.Transaction) (err error) {
		refreshTokenRepository := tx.GetRefreshTokenRepository()

		oldRefreshToken, err := refreshTokenRepository.FindByJti(tokenData.Jti)
		if err != nil {
			return
		}

		isuuedToken, issuedRefreshToken, err := usecase.tokenService.IssueTokens(oldRefreshToken.UserID, tokenExpiredDuration)
		if err != nil {
			return
		}

		err = refreshTokenRepository.Delete(oldRefreshToken.Jti)
		if err != nil {
			return
		}

		_, err = refreshTokenRepository.Create(issuedRefreshToken.Jti, oldRefreshToken.UserID)
		if err != nil {
			return
		}

		token = isuuedToken
		refreshToken = issuedRefreshToken

		return
	})

	return
}
