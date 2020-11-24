package repository

import "github.com/f-miyu/jinrou/server/app/domain"

type UserRepository interface {
	TransactionRunnable
	Create(name string) (user domain.User, err error)
	FindByID(id uint) (user domain.User, err error)
}
