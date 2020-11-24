package domain

type State struct {
	ID      string
	Config  Config
	Phase   Phase
	Day     int
	Players map[uint]Player
}
