package entity

import "gorm.io/gorm"

type UserEntity struct {
	gorm.Model
	Name string
}

func (UserEntity) TableName() string {
	return "users"
}
