package domain

type StateChange struct {
	State          State
	ChangeType     ChangeType
	OldPhase       Phase
	AddedPlayerID  uint
	LeftPlayerID   uint
	KilledPlayerID uint
	Winner         Side
}
