package domain

type ChangeType int

const (
	PlayerJoined ChangeType = iota
	PlayerLeft
	PhaseChanged
	PhaseChangedWithoutKilling
	GameOver
)
