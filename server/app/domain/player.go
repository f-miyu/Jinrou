package domain

import "time"

type Player struct {
	ID         uint
	Name       string
	Role       Role
	Side       Side
	IsDied     bool
	Index      int
	JoinedTime time.Time
}
