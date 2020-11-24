package repository

type TransactionRunnable interface {
	RunTransaction(fc func(Transaction) error) error
}
