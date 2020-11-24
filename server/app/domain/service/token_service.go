package service

import (
	"strconv"
	"time"

	"github.com/dgrijalva/jwt-go"
	"github.com/f-miyu/jinrou/server/app/domain"
	"github.com/google/uuid"
)

type TokenService interface {
	IssueTokens(userID uint, tokenExpiredDuration time.Duration) (token domain.Token, refreshToken domain.Token, err error)
	VerifyToken(tokenString string) (token domain.Token, err error)
}

type tokenService struct {
	signingKey string
}

func NewTokenService(signingKey string) TokenService {
	return &tokenService{signingKey: signingKey}
}

func (service *tokenService) IssueTokens(id uint, tokenExpiredDuration time.Duration) (token domain.Token, refreshToken domain.Token, err error) {
	token, err = service.issueToken(id, tokenExpiredDuration)
	if err != nil {
		return
	}

	refreshToken, err = service.issueToken(id, 0)

	return
}

func (service *tokenService) VerifyToken(tokenString string) (token domain.Token, err error) {
	parsedToken, err := jwt.Parse(tokenString, func(t *jwt.Token) (interface{}, error) {
		return []byte(service.signingKey), nil
	})

	if err != nil {
		return
	}

	err = parsedToken.Claims.Valid()

	if err != nil {
		return
	}

	claims := parsedToken.Claims.(jwt.MapClaims)
	userID, err := strconv.ParseUint(claims["sub"].(string), 10, 64)
	if err != nil {
		return
	}

	jti := claims["jti"].(string)

	token = domain.Token{Jti: jti, String: tokenString, UserID: uint(userID)}

	return
}

func (service *tokenService) issueToken(userID uint, expiredDuration time.Duration) (token domain.Token, err error) {
	jwtToken := jwt.New(jwt.SigningMethodHS256)
	uuid := uuid.New()
	jti := uuid.String()

	claims := jwt.MapClaims{
		"jti": jti,
		"sub": strconv.FormatUint(uint64(userID), 10),
		"iat": time.Now().Unix(),
	}

	if expiredDuration > 0 {
		claims["exp"] = time.Now().Add(expiredDuration).Unix()
	}

	jwtToken.Claims = claims

	signedString, err := jwtToken.SignedString([]byte(service.signingKey))

	token = domain.Token{Jti: jti, String: signedString, UserID: userID}

	return
}
