package main

import (
	"fmt"
	"math/rand"
	"net"
	"os"
	"time"

	"github.com/f-miyu/jinrou/server/app/data/entity"
	"github.com/f-miyu/jinrou/server/app/domain/service"
	"github.com/f-miyu/jinrou/server/app/pb"
	grpc_auth "github.com/grpc-ecosystem/go-grpc-middleware/auth"
	"google.golang.org/grpc"
	"gorm.io/driver/mysql"
	"gorm.io/gorm"
)

func main() {
	rand.Seed(time.Now().UnixNano())

	db, err := connectDB()
	if err != nil {
		fmt.Println(err)
		return
	}

	db.AutoMigrate(&entity.UserEntity{}, &entity.RefreshTokenEntity{})

	serve(db)
}

func connectDB() (db *gorm.DB, err error) {
	host := getenv("MYSQL_DB_HOST", "localhost")
	database := getenv("MYSQL_DATABASE", "jinrou")
	dbPort := getenv("MYSQL_PORT", "3306")
	user := getenv("MYSQL_USER", "user")
	password := getenv("MYSQL_PASSWORD", "password")

	dsn := fmt.Sprintf("%s:%s@tcp(%s:%s)/%s?charset=utf8mb4&parseTime=True&loc=Local",
		user, password, host, dbPort, database)

	db, err = gorm.Open(mysql.Open(dsn), &gorm.Config{})

	return
}

func serve(db *gorm.DB) (err error) {
	signingKey := getenv("SIGNING_KEY", "SECRET")
	port := getenv("GRPC_PORT", "50051")

	jinrouServer := initializeJinrouServer(db, service.NewTokenService(signingKey))

	lis, err := net.Listen("tcp", ":"+port)
	if err != nil {
		return
	}

	server := grpc.NewServer(
		grpc.StreamInterceptor(grpc_auth.StreamServerInterceptor(jinrouServer.Authenticate)),
		grpc.UnaryInterceptor(grpc_auth.UnaryServerInterceptor(jinrouServer.Authenticate)),
	)

	pb.RegisterJinrouServer(server, jinrouServer)
	err = server.Serve(lis)

	return
}

func getenv(key string, defaultValue string) string {
	env := os.Getenv(key)
	if env != "" {
		return env
	}
	return defaultValue
}
