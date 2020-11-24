package entity

import (
	"time"
)

type RefreshTokenEntity struct {
	Jti       string `gorm:"primaryKey"`
	UserID    uint
	CreatedAt time.Time
	UpdatedAt time.Time
}

func (RefreshTokenEntity) TableName() string {
	return "refresh_tokens"
}
