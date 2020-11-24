package repository

import (
	"github.com/f-miyu/jinrou/server/app/data/entity"
	"github.com/f-miyu/jinrou/server/app/domain"
	"github.com/f-miyu/jinrou/server/app/domain/repository"
	"gorm.io/gorm"
)

type userRepository struct {
	gormRepository
}

func NewUserRepository(db *gorm.DB) repository.UserRepository {
	return &userRepository{gormRepository: gormRepository{db: db}}
}

func (repository *userRepository) Create(name string) (user domain.User, err error) {
	entity := entity.UserEntity{Name: name}
	if err = repository.db.Create(&entity).Error; err != nil {
		return
	}
	user = repository.convertFrom(entity)
	return
}

func (repository *userRepository) FindByID(id uint) (user domain.User, err error) {
	entity := entity.UserEntity{}
	if err = repository.db.Find(&entity, id).Error; err != nil {
		return
	}
	user = repository.convertFrom(entity)
	return
}

func (repository *userRepository) convertFrom(entity entity.UserEntity) domain.User {
	return domain.User{
		ID:   entity.ID,
		Name: entity.Name,
	}
}
